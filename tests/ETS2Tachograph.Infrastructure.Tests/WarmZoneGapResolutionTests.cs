using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Application.Services;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Rules;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.Infrastructure.Persistence;
using ETS2Tachograph.RuleEngine;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ETS2Tachograph.Infrastructure.Tests;

/// <summary>
/// Checkpoint M5.2-P changed where a gap context takes its canonical records from:
/// it stopped projecting the raw sessions and started reusing the hot/warm history
/// projection. That stream is handed straight to the RuleEngine in ManualEntryService,
/// and the warm projection merges adjacent blocks and truncates session branches at a
/// different floor. The field measurement behind the checkpoint resolved zero gaps, so
/// it never exercised this path. These tests pin the equivalence the checkpoint claims.
/// </summary>
public sealed class WarmZoneGapResolutionTests
{
    private static readonly DateTimeOffset Epoch =
        new(2026, 7, 27, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Warm_gap_context_covers_the_same_minutes_as_the_raw_projection()
    {
        const string card = "CARD-WARM-CONTEXT";
        var (connection, context) = await CreateDatabaseAsync(card);
        await using (connection)
        await using (context)
        {
            var repository = new ActivityRepository(context);
            var gap = await ArchivedRestAroundGapAsync(repository, card);

            var gapContext = await repository.LoadGapContextAsync(gap.Id);

            Assert.NotNull(gapContext);
            Assert.True(gapContext.IsCanonical);
            Assert.Equal(
                MinuteMap(await repository.LoadRawDriverHistoryAsync(card)),
                MinuteMap(gapContext.CanonicalRecords));
        }
    }

    [Fact]
    public async Task Gap_resolved_in_the_warm_zone_evaluates_like_the_raw_projection()
    {
        const string card = "CARD-WARM-RESOLVE";
        var (connection, context) = await CreateDatabaseAsync(card);
        await using (connection)
        await using (context)
        {
            var repository = new ActivityRepository(context);
            var gap = await ArchivedRestAroundGapAsync(repository, card);
            var service = new ManualEntryService(repository);
            var now = new GameTime(1260);

            var resolved = await service.ResolveGapAsync(
                gap.Id,
                [new ManualEntrySegment(600, 660, DriverActivity.BreakOrRest)],
                now);

            Assert.Equal(ResolveGapStatus.Resolved, resolved.Status);
            var reference = new RegulationEngine().Evaluate(new RuleContext(
                now,
                await repository.LoadRawDriverHistoryAsync(card)));
            Assert.Equal(reference.State, resolved.Evaluation.State);
            Assert.Equal(1260, resolved.Evaluation.State.CurrentContinuousBreakMinutes);
        }
    }

    [Fact]
    public async Task Second_consecutive_warm_gap_resolution_keeps_rest_continuity()
    {
        const string card = "CARD-WARM-SECOND-GAP";
        var (connection, context) = await CreateDatabaseAsync(card);
        await using (connection)
        await using (context)
        {
            var repository = new ActivityRepository(context);
            var (first, second) = await ArchivedRestAroundTwoGapsAsync(repository, card);
            var service = new ManualEntryService(repository);
            var now = new GameTime(1300);

            await service.ResolveGapAsync(
                first.Id,
                [new ManualEntrySegment(600, 660, DriverActivity.BreakOrRest)],
                now);
            var resolved = await service.ResolveGapAsync(
                second.Id,
                [new ManualEntrySegment(700, 760, DriverActivity.BreakOrRest)],
                now);

            Assert.Equal(ResolveGapStatus.Resolved, resolved.Status);
            var reference = new RegulationEngine().Evaluate(new RuleContext(
                now,
                await repository.LoadRawDriverHistoryAsync(card)));
            Assert.Equal(reference.State, resolved.Evaluation.State);

            // The whole span is one uninterrupted rest once both gaps are settled.
            // The beta.10 finding broke exactly here, on the second settled gap.
            Assert.Equal(1300, resolved.Evaluation.State.CurrentContinuousBreakMinutes);
            var logical = await repository.LoadDriverHistoryAsync(card);
            Assert.Equal(
                Enumerable.Range(0, 1300)
                    .Select(minute => (minute, DriverActivity.BreakOrRest))
                    .ToList(),
                MinuteMap(logical));
        }
    }

    private static async Task<ActivityGap> ArchivedRestAroundGapAsync(
        ActivityRepository repository,
        string card)
    {
        var gap = Gap(card, 600, 660);
        await repository.ApplySessionWritesAsync(
        [
            new ActivitySessionWrite(
                card,
                0,
                new GameTime(0),
                [
                    Span(card, 0, 600, DriverActivity.BreakOrRest),
                    Span(card, 660, 1260, DriverActivity.BreakOrRest)
                ],
                [gap])
        ]);
        await ArchiveEverythingAsync(repository, card, 1260);
        return gap;
    }

    private static async Task<(ActivityGap First, ActivityGap Second)>
        ArchivedRestAroundTwoGapsAsync(ActivityRepository repository, string card)
    {
        var first = Gap(card, 600, 660);
        var second = Gap(card, 700, 760);
        await repository.ApplySessionWritesAsync(
        [
            new ActivitySessionWrite(
                card,
                0,
                new GameTime(0),
                [
                    Span(card, 0, 600, DriverActivity.BreakOrRest),
                    Span(card, 660, 700, DriverActivity.BreakOrRest),
                    Span(card, 760, 1300, DriverActivity.BreakOrRest)
                ],
                [first, second])
        ]);
        await ArchiveEverythingAsync(repository, card, 1300);
        return (first, second);
    }

    /// <summary>
    /// Pushes the retention anchor a full hot window past the recorded history, so every
    /// stored record falls below the warm threshold and the gap ends up in the warm zone.
    /// </summary>
    private static async Task ArchiveEverythingAsync(
        ActivityRepository repository,
        string card,
        long lastMinuteExclusive)
    {
        await repository.ObserveGameTimeAsync(
            card,
            new GameTime(lastMinuteExclusive + ActivityRetentionPolicy.HotWindowMinutes));
        await repository.ArchiveWarmAsync(card);
        Assert.True(await repository.LoadDriverHistoryAsync(card) is { Count: > 0 });
    }

    private static List<(int Minute, DriverActivity Activity)> MinuteMap(
        IReadOnlyList<ActivityRecord> records) => records
        .SelectMany(record => Enumerable
            .Range((int)record.Start.TotalMinutes, (int)record.DurationMinutes)
            .Select(minute => (minute, record.Activity)))
        .OrderBy(entry => entry.minute)
        .ToList();

    private static ActivityGap Gap(string card, long start, long endExclusive) => new()
    {
        Id = Guid.NewGuid(),
        DriverCardId = card,
        Slot = 1,
        SessionIndex = 0,
        Start = new GameTime(start),
        EndExclusive = new GameTime(endExclusive),
        Reason = ActivityGapReason.ForwardTimeJump,
        State = ActivityGapState.Unresolved
    };

    private static ActivityRecord Span(
        string card,
        long start,
        long endExclusive,
        DriverActivity activity) => new()
    {
        Id = Guid.NewGuid(),
        DriverCardId = card,
        Activity = activity,
        Start = new GameTime(start),
        EndExclusive = new GameTime(endExclusive),
        RecordedAtUtc = Epoch.AddMinutes(start),
        Source = ActivitySource.Telemetry,
        Condition = SpecialCondition.None
    };

    private static async Task<(SqliteConnection Connection, TachographDbContext Context)>
        CreateDatabaseAsync(string card)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TachographDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new TachographDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.DriverProfiles.Add(new DriverProfileEntity
        {
            Id = Guid.NewGuid(),
            DisplayName = card,
            IsActive = true,
            CreatedAtUtc = Epoch,
            Cards = [new DriverCardEntity
            {
                Id = card,
                CountryCode = "PL",
                ValidFrom = new DateOnly(2026, 1, 1),
                ValidUntil = new DateOnly(2031, 1, 1)
            }]
        });
        await context.SaveChangesAsync();
        return (connection, context);
    }
}

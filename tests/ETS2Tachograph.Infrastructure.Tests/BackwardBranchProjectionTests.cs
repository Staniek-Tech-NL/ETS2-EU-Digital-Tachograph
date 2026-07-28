// Test regresyjny P1 opisanego w KNOWN_ISSUES.md i w bramce zgodności
// projekcji checkpointu M5.2-P.
//
// Scenariusz: karta ma historię zarchiwizowaną do warm, po czym pojawia się NOWA sesja
// zakotwiczona poniżej progu warm (skok czasu w tył o więcej niż 14 dni gry, czyli
// wczytanie starszego zapisu gry). Nowa gałąź musi unieważnić pochodną projekcję
// warm, a odczyt ma zachować strażnik i bezpieczny fallback do projekcji raw.
//
// Zmierzony wynik (2026-07-27, przed poprawką):
//   raw     = [0-600), [660-700), [700-800)     <- poprawne, gałąź przycięta na 700
//   logical = [0-600), [660-1300), [700-800)    <- nachodzenie 700..800
//   RegulationState rozjeżdża się na czterech polach:
//     ReducedDailyRestsSinceWeeklyRest       1 -> 2
//     MinutesUntilDailyRestDeadline        740 -> 1440
//     DailyRestCompletionDeadlineGameMinute 2040 -> 2740
//     LastDailyRestResetAt                 600 -> 1300

using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Rules;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.Infrastructure.Persistence;
using ETS2Tachograph.RuleEngine;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ETS2Tachograph.Infrastructure.Tests;

public sealed class BackwardBranchProjectionTests
{
    private const string Card = "CARD-BACKWARD-BRANCH";

    private static readonly DateTimeOffset Epoch =
        new(2026, 7, 27, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Session_branching_below_the_warm_floor_does_not_overlap_warm_blocks()
    {
        var (connection, context) = await CreateDatabaseAsync();
        await using (connection)
        await using (context)
        {
            var repository = await ArchivedHistoryWithBackwardBranchAsync(context);

            var logical = await repository.LoadDriverHistoryAsync(Card);

            var minutes = logical
                .SelectMany(record => Enumerable.Range(
                    (int)record.Start.TotalMinutes,
                    (int)record.DurationMinutes))
                .ToList();
            Assert.Equal(minutes.Count, minutes.Distinct().Count());
            Assert.Empty(await context.WarmActivityBlocks.AsNoTracking().ToListAsync());
            Assert.Equal(
                0,
                await context.ActivityRecords.CountAsync(record =>
                    record.IsArchivedToWarm));
        }
    }

    [Fact]
    public async Task Gap_context_below_the_warm_floor_matches_the_raw_projection()
    {
        var (connection, context) = await CreateDatabaseAsync();
        await using (connection)
        await using (context)
        {
            var repository = await ArchivedHistoryWithBackwardBranchAsync(context);
            var gapId = await context.ActivityGaps.AsNoTracking()
                .Select(gap => gap.Id)
                .SingleAsync();
            var now = new GameTime(1300);

            var gapContext = await repository.LoadGapContextAsync(gapId);

            var raw = new RegulationEngine().Evaluate(new RuleContext(
                now,
                await repository.LoadRawDriverHistoryAsync(Card)));
            var fromContext = new RegulationEngine().Evaluate(new RuleContext(
                now,
                gapContext!.CanonicalRecords));
            Assert.Equal(raw.State, fromContext.State);
        }
    }

    [Fact]
    public async Task Rebuilt_warm_projection_after_backward_branch_is_idempotent_and_lossless()
    {
        var (connection, context) = await CreateDatabaseAsync();
        await using (connection)
        await using (context)
        {
            var repository = await ArchivedHistoryWithBackwardBranchAsync(context);
            var before = MinuteMap(await repository.LoadRawDriverHistoryAsync(Card));

            var first = await repository.ArchiveWarmAsync(Card);
            context.ChangeTracker.Clear();
            var afterFirst = MinuteMap(await repository.LoadDriverHistoryAsync(Card));
            var second = await repository.ArchiveWarmAsync(Card);
            context.ChangeTracker.Clear();
            var afterSecond = MinuteMap(await repository.LoadDriverHistoryAsync(Card));

            Assert.Equal(first, second);
            Assert.Equal(before, afterFirst);
            Assert.Equal(before, afterSecond);
            Assert.NotEmpty(await context.WarmActivityBlocks.AsNoTracking().ToListAsync());
            Assert.True(await context.ActivityRecords.AnyAsync(record =>
                record.IsArchivedToWarm));
        }
    }

    [Fact]
    public async Task Existing_overlapping_warm_projection_falls_back_to_raw_history()
    {
        var (connection, context) = await CreateDatabaseAsync();
        await using (connection)
        await using (context)
        {
            var diagnostics = new RecordingDiagnostics();
            var repository = await ArchivedHistoryWithLegacyBackwardBranchAsync(
                context,
                diagnostics);

            var logical = MinuteMap(await repository.LoadDriverHistoryAsync(Card));
            var raw = MinuteMap(await repository.LoadRawDriverHistoryAsync(Card));

            Assert.Equal(raw, logical);
            Assert.Single(diagnostics.Fallbacks);
            Assert.NotEmpty(await context.WarmActivityBlocks.AsNoTracking().ToListAsync());
            Assert.True(await context.ActivityRecords.AnyAsync(record =>
                record.IsArchivedToWarm));
        }
    }

    private static async Task<ActivityRepository> ArchivedHistoryWithBackwardBranchAsync(
        TachographDbContext context)
    {
        var repository = new ActivityRepository(context);
        await repository.ApplySessionWritesAsync(
        [
            new ActivitySessionWrite(
                Card,
                0,
                new GameTime(0),
                [Span(0, 600), Span(660, 1300)],
                [
                    new ActivityGap
                    {
                        Id = Guid.NewGuid(),
                        DriverCardId = Card,
                        Slot = 1,
                        SessionIndex = 0,
                        Start = new GameTime(600),
                        EndExclusive = new GameTime(660),
                        Reason = ActivityGapReason.ForwardTimeJump,
                        State = ActivityGapState.Unresolved
                    }
                ])
        ]);
        await repository.ObserveGameTimeAsync(
            Card,
            new GameTime(1300 + ActivityRetentionPolicy.HotWindowMinutes));
        await repository.ArchiveWarmAsync(Card);

        // Backward jump: the new session anchors more than a hot window below the mark.
        await repository.EnsureSessionAsync(Card, 1, new GameTime(700));
        await repository.AppendAsync(Card, 1, [Span(700, 800)]);
        return repository;
    }

    private static async Task<ActivityRepository> ArchivedHistoryWithLegacyBackwardBranchAsync(
        TachographDbContext context,
        RecordingDiagnostics diagnostics)
    {
        var repository = new ActivityRepository(context);
        await repository.ApplySessionWritesAsync(
        [
            new ActivitySessionWrite(
                Card,
                0,
                new GameTime(0),
                [Span(0, 600), Span(660, 1300)])
        ]);
        await repository.ObserveGameTimeAsync(
            Card,
            new GameTime(1300 + ActivityRetentionPolicy.HotWindowMinutes));
        await repository.ArchiveWarmAsync(Card);
        context.ChangeTracker.Clear();

        // Simulates a database created before the hotfix: the backward session already
        // exists, so the new-session invalidation hook cannot run during this process.
        context.ActivitySessions.Add(new ActivitySessionEntity
        {
            Id = Guid.NewGuid(),
            DriverCardId = Card,
            SessionIndex = 1,
            StartedAtGameMinute = 700,
            CreatedAtUtc = Epoch.AddDays(1),
            Records =
            [
                new ActivityRecordEntity
                {
                    Id = Guid.NewGuid(),
                    Activity = DriverActivity.BreakOrRest,
                    StartGameMinute = 700,
                    EndGameMinuteExclusive = 800,
                    RecordedAtUtc = Epoch.AddMinutes(700),
                    Source = ActivitySource.Telemetry,
                    Condition = SpecialCondition.None
                }
            ]
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return new ActivityRepository(context, diagnostics);
    }

    private static ActivityRecord Span(long start, long endExclusive) => new()
    {
        Id = Guid.NewGuid(),
        DriverCardId = Card,
        Activity = DriverActivity.BreakOrRest,
        Start = new GameTime(start),
        EndExclusive = new GameTime(endExclusive),
        RecordedAtUtc = Epoch.AddMinutes(start),
        Source = ActivitySource.Telemetry,
        Condition = SpecialCondition.None
    };

    private static IReadOnlyList<(long Minute, DriverActivity Activity)> MinuteMap(
        IEnumerable<ActivityRecord> records) => records
        .SelectMany(record => Enumerable.Range(
                (int)record.Start.TotalMinutes,
                (int)record.DurationMinutes)
            .Select(minute => ((long)minute, record.Activity)))
        .ToList();

    private static async Task<(SqliteConnection Connection, TachographDbContext Context)>
        CreateDatabaseAsync()
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
            DisplayName = Card,
            IsActive = true,
            CreatedAtUtc = Epoch,
            Cards = [new DriverCardEntity
            {
                Id = Card,
                CountryCode = "PL",
                ValidFrom = new DateOnly(2026, 1, 1),
                ValidUntil = new DateOnly(2031, 1, 1)
            }]
        });
        await context.SaveChangesAsync();
        return (connection, context);
    }

    private sealed class RecordingDiagnostics : IActivityPersistenceDiagnostics
    {
        public List<(string CardId, ActivityRecord Previous, ActivityRecord Current)>
            Fallbacks { get; } = [];

        public void RecordConflict(
            string driverCardId,
            int sessionIndex,
            ActivityRecord existing,
            ActivityRecord incoming)
        {
        }

        public void RecordCanonicalProjectionFallback(
            string driverCardId,
            ActivityRecord previous,
            ActivityRecord current) =>
            Fallbacks.Add((driverCardId, previous, current));
    }
}

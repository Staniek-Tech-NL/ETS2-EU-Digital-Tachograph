using System.Text;
using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Application.Services;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Rules;
using ETS2Tachograph.Core.Telemetry;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.Engine;
using ETS2Tachograph.Infrastructure.Persistence;
using ETS2Tachograph.RuleEngine;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ETS2Tachograph.Infrastructure.Tests;

public sealed class ActivityRetentionTests
{
    private static readonly DateTimeOffset Epoch =
        new(2026, 7, 15, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Archive_then_restore_preserves_3h53_plus_1h34_as_5h27_without_overlap()
    {
        var (connection, context) = await CreateDatabaseAsync("CARD-327-HOT");
        await using (connection)
        await using (context)
        {
            var repository = new ActivityRepository(context);
            await AddOverlappingDrivingSessionsAsync(repository, "CARD-327-HOT");
            await repository.ObserveGameTimeAsync("CARD-327-HOT", new GameTime(327));

            await repository.ArchiveWarmAsync("CARD-327-HOT");
            var sessions = await repository.LoadSessionsAsync("CARD-327-HOT");
            var engine = new TachographEngine("CARD-327-HOT");
            engine.RestoreSessions(sessions.Select(x => new RestoredActivitySession(
                x.SessionIndex,
                x.StartedAt,
                x.Records)));

            Assert.Equal(327, engine.Current.Regulation!.State.DailyDrivingMinutes);
            Assert.Equal(327, engine.History.RegulationRecords().Sum(x => x.DurationMinutes));
            Assert.Equal(327, engine.History.RegulationRecords().Count);
        }
    }

    [Fact]
    public async Task Warm_archive_is_idempotent_lossless_and_preserves_truncate_append()
    {
        var (connection, context) = await CreateDatabaseAsync("CARD-327-WARM");
        await using (connection)
        await using (context)
        {
            var repository = new ActivityRepository(context);
            await AddOverlappingDrivingSessionsAsync(repository, "CARD-327-WARM");
            await repository.ObserveGameTimeAsync(
                "CARD-327-WARM",
                new GameTime(327 + ActivityRetentionPolicy.HotWindowMinutes));

            var first = await repository.ArchiveWarmAsync("CARD-327-WARM");
            context.ChangeTracker.Clear();
            var firstIds = await context.WarmActivityBlocks.AsNoTracking()
                .Select(x => x.Id)
                .ToListAsync();
            var second = await repository.ArchiveWarmAsync("CARD-327-WARM");
            context.ChangeTracker.Clear();
            var secondIds = await context.WarmActivityBlocks.AsNoTracking()
                .Select(x => x.Id)
                .ToListAsync();
            var logical = await repository.LoadDriverHistoryAsync("CARD-327-WARM");
            var raw = await repository.LoadRawDriverHistoryAsync("CARD-327-WARM");
            var physicalCount = await context.ActivityRecords.CountAsync();
            var archivedCount = await context.ActivityRecords.CountAsync(x => x.IsArchivedToWarm);
            var regulation = new RegulationEngine().Evaluate(
                new RuleContext(new GameTime(327), logical));

            Assert.Equal(first, second);
            Assert.Equal(firstIds, secondIds);
            Assert.Equal(328, physicalCount);
            Assert.Equal(328, archivedCount);
            Assert.Equal(327, raw.Count);
            var warmBlock = Assert.Single(logical);
            Assert.Equal(327, warmBlock.DurationMinutes);
            Assert.Equal(327, regulation.State.DailyDrivingMinutes);
            Assert.All(await repository.LoadSessionsAsync("CARD-327-WARM"),
                session => Assert.Empty(session.Records));
        }
    }

    [Fact]
    public async Task Empty_archived_session_does_not_remove_history_restored_by_later_session()
    {
        const string cardId = "CARD-EMPTY-ARCHIVED-BRANCH";
        var (connection, context) = await CreateDatabaseAsync(cardId);
        await using (connection)
        await using (context)
        {
            var repository = new ActivityRepository(context);
            await repository.EnsureSessionAsync(cardId, 0, new GameTime(0));
            await repository.AppendAsync(cardId, 0, Enumerable.Range(0, 10)
                .Select(minute => Record(cardId, minute, DriverActivity.Driving))
                .ToList());
            await repository.EnsureSessionAsync(cardId, 1, new GameTime(3));
            await repository.EnsureSessionAsync(cardId, 2, new GameTime(5));
            await repository.AppendAsync(cardId, 2, Enumerable.Range(5, 5)
                .Select(minute => Record(cardId, minute, DriverActivity.BreakOrRest))
                .ToList());
            await repository.ObserveGameTimeAsync(
                cardId,
                new GameTime(10 + ActivityRetentionPolicy.HotWindowMinutes));

            await repository.ArchiveWarmAsync(cardId);
            context.ChangeTracker.Clear();

            var logical = await repository.LoadDriverHistoryAsync(cardId);
            var raw = await repository.LoadRawDriverHistoryAsync(cardId);
            var logicalMinutes = logical
                .SelectMany(x => Enumerable
                    .Range((int)x.Start.TotalMinutes, (int)x.DurationMinutes)
                    .Select(minute => (minute, x.Activity)))
                .ToList();
            var rawMinutes = raw
                .Select(x => ((int)x.Start.TotalMinutes, x.Activity))
                .ToList();

            Assert.Equal(rawMinutes, logicalMinutes);
            Assert.Equal(
                [
                    (new GameTime(0), new GameTime(3), DriverActivity.Driving),
                    (new GameTime(5), new GameTime(10), DriverActivity.BreakOrRest)
                ],
                logical.Select(x => (x.Start, x.EndExclusive, x.Activity)).ToList());
        }
    }

    [Fact]
    public async Task Warm_blocks_split_only_on_activity_and_mark_changed_source_as_mixed()
    {
        var (connection, context) = await CreateDatabaseAsync("CARD-MIXED");
        await using (connection)
        await using (context)
        {
            var repository = new ActivityRepository(context);
            await repository.EnsureSessionAsync("CARD-MIXED", 0, new GameTime(0));
            await repository.AppendAsync("CARD-MIXED", 0,
            [
                Record("CARD-MIXED", 0, DriverActivity.Driving, ActivitySource.Telemetry),
                Record("CARD-MIXED", 1, DriverActivity.Driving, ActivitySource.Reconstructed),
                Record("CARD-MIXED", 2, DriverActivity.Driving, ActivitySource.Telemetry),
                Record("CARD-MIXED", 3, DriverActivity.OtherWork, ActivitySource.Manual)
            ]);
            await repository.ObserveGameTimeAsync(
                "CARD-MIXED",
                new GameTime(4 + ActivityRetentionPolicy.HotWindowMinutes));

            var result = await repository.ArchiveWarmAsync("CARD-MIXED");
            var blocks = await context.WarmActivityBlocks.AsNoTracking()
                .OrderBy(x => x.StartGameMinute)
                .ToListAsync();

            Assert.Equal(2, result.WarmBlockCount);
            Assert.Equal(2, blocks.Count);
            Assert.Equal(3, blocks[0].DurationMinutes);
            Assert.Equal(ActivitySource.Mixed, blocks[0].Source);
            Assert.Equal(DriverActivity.OtherWork, blocks[1].Activity);
        }
    }

    [Fact]
    public async Task Warm_archive_does_not_merge_stationary_rest_with_crew_break_in_motion()
    {
        var (connection, context) = await CreateDatabaseAsync("CARD-CREW-BREAK");
        await using (connection)
        await using (context)
        {
            var repository = new ActivityRepository(context);
            await repository.EnsureSessionAsync("CARD-CREW-BREAK", 0, new GameTime(0));
            await repository.AppendAsync("CARD-CREW-BREAK", 0,
            [
                Record(
                    "CARD-CREW-BREAK",
                    0,
                    DriverActivity.BreakOrRest,
                    condition: SpecialCondition.None),
                Record(
                    "CARD-CREW-BREAK",
                    1,
                    DriverActivity.BreakOrRest,
                    condition: SpecialCondition.CrewBreakInMotion)
            ]);
            await repository.ObserveGameTimeAsync(
                "CARD-CREW-BREAK",
                new GameTime(2 + ActivityRetentionPolicy.HotWindowMinutes));

            var result = await repository.ArchiveWarmAsync("CARD-CREW-BREAK");
            var blocks = await context.WarmActivityBlocks.AsNoTracking()
                .OrderBy(x => x.StartGameMinute)
                .ToListAsync();

            Assert.Equal(2, result.WarmBlockCount);
            Assert.Collection(
                blocks,
                block => Assert.Equal(SpecialCondition.None, block.Condition),
                block => Assert.Equal(
                    SpecialCondition.CrewBreakInMotion,
                    block.Condition));
        }
    }

    [Fact]
    public async Task Warm_archive_preserves_manual_entry_source_gap_link()
    {
        var (connection, context) = await CreateDatabaseAsync("CARD-MANUAL-WARM");
        await using (connection)
        await using (context)
        {
            var repository = new ActivityRepository(context);
            var gap = new ActivityGap
            {
                Id = Guid.NewGuid(),
                DriverCardId = "CARD-MANUAL-WARM",
                Slot = 1,
                SessionIndex = 0,
                Start = new GameTime(0),
                EndExclusive = new GameTime(3),
                Reason = ActivityGapReason.CardRemoved,
                State = ActivityGapState.Resolved,
                ResolvedAt = new GameTime(10)
            };
            await repository.ApplySessionWritesAsync(
            [
                new ActivitySessionWrite(
                    "CARD-MANUAL-WARM",
                    0,
                    new GameTime(0),
                    [
                        Record("CARD-MANUAL-WARM", 0, DriverActivity.BreakOrRest,
                            ActivitySource.ManualEntry) with { SourceGapId = gap.Id },
                        Record("CARD-MANUAL-WARM", 1, DriverActivity.BreakOrRest,
                            ActivitySource.ManualEntry) with { SourceGapId = gap.Id },
                        Record("CARD-MANUAL-WARM", 2, DriverActivity.BreakOrRest,
                            ActivitySource.ManualEntry) with { SourceGapId = gap.Id }
                    ],
                    [gap])
            ]);
            await repository.ObserveGameTimeAsync(
                "CARD-MANUAL-WARM",
                new GameTime(3 + ActivityRetentionPolicy.HotWindowMinutes));

            await repository.ArchiveWarmAsync("CARD-MANUAL-WARM");

            context.ChangeTracker.Clear();
            var block = Assert.Single(await repository.LoadDriverHistoryAsync("CARD-MANUAL-WARM"));
            Assert.Equal(ActivitySource.ManualEntry, block.Source);
            Assert.Equal(gap.Id, block.SourceGapId);
            Assert.Equal(3, block.DurationMinutes);
        }
    }

    [Fact]
    public async Task Session_crossing_hot_boundary_truncates_only_previous_hot_tail()
    {
        var (connection, context) = await CreateDatabaseAsync("CARD-BOUNDARY");
        await using (connection)
        await using (context)
        {
            var repository = new ActivityRepository(context);
            await repository.EnsureSessionAsync("CARD-BOUNDARY", 0, new GameTime(0));
            await repository.AppendAsync("CARD-BOUNDARY", 0, Enumerable.Range(0, 20)
                .Select(minute => Record("CARD-BOUNDARY", minute, DriverActivity.Driving))
                .ToList());
            await repository.EnsureSessionAsync("CARD-BOUNDARY", 1, new GameTime(5));
            await repository.AppendAsync("CARD-BOUNDARY", 1, Enumerable.Range(5, 10)
                .Select(minute => Record("CARD-BOUNDARY", minute, DriverActivity.Driving))
                .ToList());
            await repository.ObserveGameTimeAsync(
                "CARD-BOUNDARY",
                new GameTime(10 + ActivityRetentionPolicy.HotWindowMinutes));

            await repository.ArchiveWarmAsync("CARD-BOUNDARY");
            var logical = await repository.LoadDriverHistoryAsync("CARD-BOUNDARY");
            var clockSpan = logical.Max(x => x.EndExclusive.TotalMinutes) -
                logical.Min(x => x.Start.TotalMinutes);

            Assert.Equal(clockSpan, logical.Sum(x => x.DurationMinutes));
            Assert.Equal(15, clockSpan);
            Assert.Equal(15, logical.Max(x => x.EndExclusive.TotalMinutes));
            Assert.Equal(logical.Sum(x => x.DurationMinutes),
                logical.SelectMany(x => Enumerable.Range(
                    (int)x.Start.TotalMinutes,
                    (int)x.DurationMinutes)).Distinct().LongCount());
        }
    }

    [Fact]
    public async Task High_water_mark_does_not_decrease_after_backward_telemetry_frame()
    {
        var (connection, context) = await CreateDatabaseAsync("CARD-HWM");
        await using (connection)
        await using (context)
        {
            var repository = new ActivityRepository(context);
            var service = new CrewTachographService(
                new CrewTachographEngine(),
                repository,
                new ActivityRetentionService(repository));
            await service.RegisterCardAsync("CARD-HWM");
            service.InsertCard(TachographSlot.Driver, "CARD-HWM");

            await service.ProcessFrameAsync(new TelemetryFrame(
                new GameTime(50_000), Epoch, 0, GamePaused: false));
            await service.ProcessFrameAsync(new TelemetryFrame(
                new GameTime(100), Epoch.AddSeconds(1), 0, GamePaused: false));

            Assert.Equal(50_000, await repository.GetHighWaterMarkAsync("CARD-HWM"));
        }
    }

    [Fact]
    public async Task Raw_csv_stays_minute_level_after_warm_archiving()
    {
        var (connection, context) = await CreateDatabaseAsync("CARD-CSV");
        await using (connection)
        await using (context)
        {
            var repository = new ActivityRepository(context);
            await repository.EnsureSessionAsync("CARD-CSV", 0, new GameTime(0));
            await repository.AppendAsync("CARD-CSV", 0,
            [
                Record("CARD-CSV", 0, DriverActivity.Driving),
                Record("CARD-CSV", 1, DriverActivity.Driving),
                Record("CARD-CSV", 2, DriverActivity.Driving)
            ]);
            await repository.ObserveGameTimeAsync(
                "CARD-CSV",
                new GameTime(3 + ActivityRetentionPolicy.HotWindowMinutes));
            await repository.ArchiveWarmAsync("CARD-CSV");
            var reports = new ReportService(repository, new EmptyRegulationAnalyzer());
            var report = await reports.CreateAsync("CARD-CSV");
            await using var csv = new MemoryStream();

            await reports.ExportCsvAsync(report, csv);

            Assert.Single(report.Records);
            var lines = Encoding.UTF8.GetString(csv.ToArray())
                .Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(4, lines.Length);
        }
    }

    private static async Task AddOverlappingDrivingSessionsAsync(
        ActivityRepository repository,
        string cardId)
    {
        await repository.EnsureSessionAsync(cardId, 0, new GameTime(0));
        await repository.AppendAsync(cardId, 0, Enumerable.Range(0, 234)
            .Select(minute => Record(cardId, minute, DriverActivity.Driving))
            .ToList());
        await repository.EnsureSessionAsync(cardId, 1, new GameTime(233));
        await repository.AppendAsync(cardId, 1, Enumerable.Range(233, 94)
            .Select(minute => Record(cardId, minute, DriverActivity.Driving))
            .ToList());
    }

    private static ActivityRecord Record(
        string cardId,
        long minute,
        DriverActivity activity,
        ActivitySource source = ActivitySource.Telemetry,
        SpecialCondition condition = SpecialCondition.None) => new()
    {
        Id = Guid.NewGuid(),
        DriverCardId = cardId,
        Activity = activity,
        Start = new GameTime(minute),
        EndExclusive = new GameTime(minute + 1),
        RecordedAtUtc = Epoch.AddMinutes(minute),
        Source = source,
        Condition = condition
    };

    private static async Task<(SqliteConnection Connection, TachographDbContext Context)>
        CreateDatabaseAsync(string cardId)
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
            DisplayName = cardId,
            IsActive = true,
            CreatedAtUtc = Epoch,
            Cards = [new DriverCardEntity
            {
                Id = cardId,
                CountryCode = "PL",
                ValidFrom = new DateOnly(2026, 1, 1),
                ValidUntil = new DateOnly(2031, 1, 1)
            }]
        });
        await context.SaveChangesAsync();
        return (connection, context);
    }

    private sealed class EmptyRegulationAnalyzer : IRegulationReportAnalyzer
    {
        public Application.Dtos.RegulationReportAnalysisDto Analyze(
            GameTime now,
            IReadOnlyList<ActivityRecord> history) => Application.Dtos.RegulationReportAnalysisDto.Empty;
    }
}

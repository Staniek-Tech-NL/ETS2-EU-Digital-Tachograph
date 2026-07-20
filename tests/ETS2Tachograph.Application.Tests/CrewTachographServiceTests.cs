using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Application.Services;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Telemetry;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.Engine;

namespace ETS2Tachograph.Application.Tests;

public sealed class CrewTachographServiceTests
{
    private static readonly DateTimeOffset Epoch = new(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Completed_minutes_are_saved_under_the_card_from_each_slot()
    {
        var repository = new FakeActivityRepository();
        var crew = new CrewTachographEngine();
        var service = new CrewTachographService(crew, repository);
        await service.RegisterCardAsync("CARD-A");
        await service.RegisterCardAsync("CARD-B");
        service.InsertCard(TachographSlot.Driver, "CARD-A");
        service.InsertCard(TachographSlot.CoDriver, "CARD-B");

        await service.ProcessFrameAsync(Frame(0, 40));
        await service.ProcessFrameAsync(Frame(1, 40));
        await service.ProcessFrameAsync(Frame(2, 40));

        var driver = await service.LoadDriverHistoryAsync("CARD-A");
        var coDriver = await service.LoadDriverHistoryAsync("CARD-B");
        Assert.Contains(driver, x => x.Activity == DriverActivity.Driving);
        Assert.Contains(coDriver, x => x.Activity == DriverActivity.Availability);
        Assert.All(driver, x => Assert.Equal("CARD-A", x.DriverCardId));
        Assert.All(coDriver, x => Assert.Equal("CARD-B", x.DriverCardId));
    }

    [Fact]
    public async Task Ejecting_a_card_flushes_previous_minute_without_overlapping_gap_start()
    {
        var repository = new FakeActivityRepository();
        var crew = new CrewTachographEngine();
        var service = new CrewTachographService(crew, repository);
        await service.RegisterCardAsync("CARD-A");
        service.InsertCard(TachographSlot.Driver, "CARD-A");
        await service.ProcessFrameAsync(Frame(10, 0));
        await service.ProcessFrameAsync(Frame(11, 0));

        await service.EjectCardAsync(TachographSlot.Driver, Epoch.AddMinutes(11));

        var record = Assert.Single(await service.LoadDriverHistoryAsync("CARD-A"));
        Assert.Equal(new GameTime(10), record.Start);
        Assert.Equal(new GameTime(11), record.EndExclusive);
        Assert.Null(service.Current.DriverCardId);
    }

    [Fact]
    public async Task Registering_a_card_restores_its_persisted_sessions()
    {
        var repository = new FakeActivityRepository();
        await repository.EnsureSessionAsync("CARD-A", 0, new GameTime(0));
        await repository.AppendAsync("CARD-A", 0, [Record("CARD-A", 0, DriverActivity.Driving)]);
        var crew = new CrewTachographEngine();
        var service = new CrewTachographService(crew, repository);

        await service.RegisterCardAsync("CARD-A");

        Assert.Single(crew.GetEngine("CARD-A")!.History.CurrentTimeline.Records);
    }

    [Fact]
    public async Task Paused_frame_is_ignored_by_persistence_and_high_water_mark()
    {
        var repository = new FakeActivityRepository();
        var crew = new CrewTachographEngine();
        var service = new CrewTachographService(
            crew,
            repository,
            new ActivityRetentionService(repository));
        await service.RegisterCardAsync("CARD-A");
        service.InsertCard(TachographSlot.Driver, "CARD-A");
        repository.ObservedGameTimes.Clear();

        var paused = new TelemetryFrame(
            new GameTime(0),
            Epoch,
            SpeedKph: 0,
            GamePaused: true);
        await service.ProcessFrameAsync(paused);

        Assert.Empty(repository.ObservedGameTimes);
        Assert.Empty(await service.LoadDriverHistoryAsync("CARD-A"));
    }

    [Fact]
    public async Task Backward_clock_boundary_for_both_cards_is_applied_in_one_write_set()
    {
        var repository = new FakeActivityRepository();
        await repository.EnsureSessionAsync("CARD-A", 0, new GameTime(0));
        await repository.AppendAsync("CARD-A", 0,
            [Record("CARD-A", 0, DriverActivity.Driving)]);
        await repository.EnsureSessionAsync("CARD-A", 1, new GameTime(1));
        await repository.AppendAsync("CARD-A", 1,
            [Record("CARD-A", 1, DriverActivity.OtherWork)]);
        await repository.EnsureSessionAsync("CARD-B", 0, new GameTime(0));
        await repository.AppendAsync("CARD-B", 0,
            [Record("CARD-B", 0, DriverActivity.Availability)]);
        var crew = new CrewTachographEngine();
        var service = new CrewTachographService(crew, repository);
        await service.RegisterCardAsync("CARD-A");
        await service.RegisterCardAsync("CARD-B");
        service.InsertCard(TachographSlot.Driver, "CARD-A");
        service.InsertCard(TachographSlot.CoDriver, "CARD-B");

        await service.ProcessFrameAsync(FrameAt(20, 0, 40));
        await service.ProcessFrameAsync(FrameAt(21, 1, 40));
        var movedBack = await service.ProcessFrameAsync(FrameAt(5, 2, 0));

        Assert.Equal(2, movedBack.Driver!.SessionIndex);
        Assert.Equal(1, movedBack.CoDriver!.SessionIndex);
        Assert.Equal(1, Assert.Single(movedBack.Driver.CompletedBatches).SessionIndex);
        Assert.Equal(0, Assert.Single(movedBack.CoDriver.CompletedBatches).SessionIndex);

        var boundaryWriteSet = repository.AppliedWriteSets.Last();
        Assert.Equal(4, boundaryWriteSet.Count);
        foreach (var (cardId, oldIndex, newIndex) in new[]
                 {
                     ("CARD-A", 1, 2),
                     ("CARD-B", 0, 1)
                 })
        {
            var oldSession = Assert.Single(boundaryWriteSet,
                write => write.DriverCardId == cardId && write.SessionIndex == oldIndex);
            Assert.NotEmpty(oldSession.Records);
            var newSession = Assert.Single(boundaryWriteSet,
                write => write.DriverCardId == cardId && write.SessionIndex == newIndex);
            Assert.Equal(new GameTime(5), newSession.StartedAt);
            Assert.Empty(newSession.Records);
        }
    }

    [Fact]
    public async Task World_generation_change_opens_both_card_branches_in_one_write_set()
    {
        var repository = new FakeActivityRepository();
        var crew = new CrewTachographEngine();
        var service = new CrewTachographService(crew, repository);
        await service.RegisterCardAsync("CARD-A");
        await service.RegisterCardAsync("CARD-B");
        service.InsertCard(TachographSlot.Driver, "CARD-A");
        service.InsertCard(TachographSlot.CoDriver, "CARD-B");
        await service.ProcessFrameAsync(FrameAt(20, 0, 30, worldGeneration: 4));
        await service.ProcessFrameAsync(FrameAt(21, 1, 30, worldGeneration: 4));

        var loaded = await service.ProcessFrameAsync(FrameAt(21, 2, 0, worldGeneration: 7));

        Assert.True(loaded.Driver!.WorldGenerationChanged);
        Assert.True(loaded.CoDriver!.WorldGenerationChanged);
        var writes = repository.AppliedWriteSets.Last();
        Assert.Equal(4, writes.Count);
        foreach (var cardId in new[] { "CARD-A", "CARD-B" })
        {
            Assert.NotEmpty(Assert.Single(writes,
                write => write.DriverCardId == cardId && write.SessionIndex == 0).Records);
            var opened = Assert.Single(writes,
                write => write.DriverCardId == cardId && write.SessionIndex == 1);
            Assert.Equal(new GameTime(21), opened.StartedAt);
            Assert.Empty(opened.Records);
        }
    }

    private static TelemetryFrame Frame(long minute, double speed) =>
        new(new GameTime(minute), Epoch.AddMinutes(minute), speed, GamePaused: false);

    private static TelemetryFrame FrameAt(
        long minute,
        int recordedSecond,
        double speed,
        uint worldGeneration = 0) =>
        new(
            new GameTime(minute),
            Epoch.AddSeconds(recordedSecond),
            speed,
            GamePaused: false,
            WorldGeneration: worldGeneration);

    private static ActivityRecord Record(string cardId, long minute, DriverActivity activity) => new()
    {
        Id = Guid.NewGuid(), DriverCardId = cardId, Activity = activity,
        Start = new GameTime(minute), EndExclusive = new GameTime(minute + 1),
        RecordedAtUtc = Epoch.AddMinutes(minute), Source = ActivitySource.Telemetry
    };

    private sealed class FakeActivityRepository : IActivityRepository, IActivityRetentionRepository
    {
        private readonly Dictionary<(string Card, int Session), StoredActivitySession> _sessions = [];

        public List<long> ObservedGameTimes { get; } = [];
        public List<IReadOnlyList<ActivitySessionWrite>> AppliedWriteSets { get; } = [];

        public Task EnsureSessionAsync(string driverCardId, int sessionIndex, GameTime startedAt, CancellationToken cancellationToken = default)
        {
            _sessions.TryAdd((driverCardId, sessionIndex), new StoredActivitySession(sessionIndex, startedAt, []));
            return Task.CompletedTask;
        }

        public Task AppendAsync(string driverCardId, int sessionIndex, IReadOnlyList<ActivityRecord> records, CancellationToken cancellationToken = default)
        {
            var key = (driverCardId, sessionIndex);
            var old = _sessions[key];
            _sessions[key] = old with { Records = old.Records.Concat(records).DistinctBy(x => x.Id).ToList() };
            return Task.CompletedTask;
        }

        public async Task ApplySessionWritesAsync(
            IReadOnlyList<ActivitySessionWrite> writes,
            CancellationToken cancellationToken = default)
        {
            if (writes.Count > 0)
                AppliedWriteSets.Add(writes.ToList());
            foreach (var write in writes)
            {
                await EnsureSessionAsync(
                    write.DriverCardId, write.SessionIndex, write.StartedAt, cancellationToken);
                await AppendAsync(
                    write.DriverCardId, write.SessionIndex, write.Records, cancellationToken);
            }
        }

        public Task<IReadOnlyList<ActivityRecord>> LoadDriverHistoryAsync(string driverCardId, GameTime? from = null, GameTime? toExclusive = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ActivityRecord>>(_sessions.Where(x => x.Key.Card == driverCardId)
                .SelectMany(x => x.Value.Records).OrderBy(x => x.Start).ToList());

        public Task<IReadOnlyList<StoredActivitySession>> LoadSessionsAsync(string driverCardId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StoredActivitySession>>(_sessions.Where(x => x.Key.Card == driverCardId)
                .Select(x => x.Value).OrderBy(x => x.SessionIndex).ToList());

        public Task<ActivityRetentionResult> ArchiveWarmAsync(
            string driverCardId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ActivityRetentionResult(driverCardId, 0, 0, 0, 0, 0));

        public Task<long> ObserveGameTimeAsync(
            string driverCardId,
            GameTime gameTime,
            CancellationToken cancellationToken = default)
        {
            ObservedGameTimes.Add(gameTime.TotalMinutes);
            return Task.FromResult(gameTime.TotalMinutes);
        }

        public Task<long?> GetHighWaterMarkAsync(
            string driverCardId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<long?>(ObservedGameTimes.Count == 0 ? null : ObservedGameTimes.Max());
    }
}

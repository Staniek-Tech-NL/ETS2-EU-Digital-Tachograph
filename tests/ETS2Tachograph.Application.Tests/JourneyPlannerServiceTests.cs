using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Application.Services;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Telemetry;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.Engine;
using ETS2Tachograph.RuleEngine.JourneyPlanning;

namespace ETS2Tachograph.Application.Tests;

public sealed class JourneyPlannerServiceTests
{
    [Fact]
    public async Task Planning_uses_one_snapshot_and_does_not_write_hypothetical_history()
    {
        var repository = new RecordingRepository();
        var crew = await CreateCrewAsync(repository, TachographSlot.Driver, "S1");
        var planner = new JourneyPlannerService(crew);
        var writesBefore = repository.WriteCount;

        var result = await planner.PlanAsync(new JourneyPlannerInput(1, 60, 120, 15));

        Assert.Equal(JourneyPlanStatus.MeetsDeadline, result.Status);
        Assert.Equal(writesBefore, repository.WriteCount);
        Assert.Equal(1, result.SnapshotIdentity.DriverSlot);
    }

    [Fact]
    public async Task Selecting_slot_two_does_not_enable_multi_manning_without_two_inserted_cards()
    {
        var repository = new RecordingRepository();
        var crew = await CreateCrewAsync(repository, TachographSlot.CoDriver, "S2");
        var planner = new JourneyPlannerService(crew);

        var snapshot = await planner.GetSnapshotAsync(2);

        Assert.NotNull(snapshot);
        Assert.False(snapshot.MultiManningActive);
    }

    [Fact]
    public async Task New_telemetry_invalidates_previous_result_identity()
    {
        var repository = new RecordingRepository();
        var crew = await CreateCrewAsync(repository, TachographSlot.Driver, "S1");
        var planner = new JourneyPlannerService(crew);
        var result = await planner.PlanAsync(new JourneyPlannerInput(1, 60, 120, 0));

        await crew.ProcessFrameAsync(Frame(101));

        Assert.False(planner.IsCurrent(result.SnapshotIdentity));
    }

    [Fact]
    public async Task Same_minute_telemetry_during_snapshot_load_does_not_block_planning()
    {
        var repository = new RecordingRepository();
        var crew = await CreateCrewAsync(repository, TachographSlot.Driver, "S1");
        var planner = new JourneyPlannerService(crew);
        repository.BeforeHistoryRead = () => crew.Engine.ProcessFrame(Frame(100));

        var result = await planner.PlanAsync(new JourneyPlannerInput(1, 60, 120, 0));

        Assert.Equal(JourneyPlanStatus.MeetsDeadline, result.Status);
        Assert.True(planner.IsCurrent(result.SnapshotIdentity));
    }

    [Fact]
    public async Task Missing_card_returns_controlled_insufficient_data()
    {
        var crew = new CrewTachographService(
            new CrewTachographEngine(),
            new RecordingRepository());
        var planner = new JourneyPlannerService(crew);

        var result = await planner.PlanAsync(new JourneyPlannerInput(1, 60, 120, 0));

        Assert.Equal(JourneyPlanStatus.InsufficientData, result.Status);
    }

    private static async Task<CrewTachographService> CreateCrewAsync(
        RecordingRepository repository,
        TachographSlot slot,
        string cardId)
    {
        var engine = new CrewTachographEngine();
        var crew = new CrewTachographService(engine, repository);
        await crew.RegisterCardAsync(cardId);
        crew.InsertCard(slot, cardId);
        await crew.ProcessFrameAsync(Frame(100));
        return crew;
    }

    private static TelemetryFrame Frame(long minute) => new(
        new GameTime(minute),
        DateTimeOffset.UnixEpoch.AddMinutes(minute),
        SpeedKph: 0,
        GamePaused: false,
        WorldGeneration: 7);

    private sealed class RecordingRepository : IActivityRepository
    {
        private readonly Dictionary<(string Card, int Session), StoredActivitySession> _sessions = [];
        internal int WriteCount { get; private set; }
        internal Action? BeforeHistoryRead { get; set; }

        public Task EnsureSessionAsync(
            string driverCardId,
            int sessionIndex,
            GameTime startedAt,
            CancellationToken cancellationToken = default)
        {
            _sessions.TryAdd(
                (driverCardId, sessionIndex),
                new StoredActivitySession(sessionIndex, startedAt, []));
            return Task.CompletedTask;
        }

        public Task AppendAsync(
            string driverCardId,
            int sessionIndex,
            IReadOnlyList<ActivityRecord> records,
            CancellationToken cancellationToken = default)
        {
            var key = (driverCardId, sessionIndex);
            var session = _sessions[key];
            _sessions[key] = session with
            {
                Records = session.Records.Concat(records).DistinctBy(record => record.Id).ToArray()
            };
            return Task.CompletedTask;
        }

        public async Task ApplySessionWritesAsync(
            IReadOnlyList<ActivitySessionWrite> writes,
            CancellationToken cancellationToken = default)
        {
            WriteCount += writes.Count;
            foreach (var write in writes)
            {
                await EnsureSessionAsync(
                    write.DriverCardId,
                    write.SessionIndex,
                    write.StartedAt,
                    cancellationToken);
                await AppendAsync(
                    write.DriverCardId,
                    write.SessionIndex,
                    write.Records,
                    cancellationToken);
            }
        }

        public Task<IReadOnlyList<ActivityRecord>> LoadDriverHistoryAsync(
            string driverCardId,
            GameTime? from = null,
            GameTime? toExclusive = null,
            CancellationToken cancellationToken = default)
        {
            BeforeHistoryRead?.Invoke();
            return Task.FromResult<IReadOnlyList<ActivityRecord>>(_sessions
                .Where(pair => pair.Key.Card == driverCardId)
                .SelectMany(pair => pair.Value.Records)
                .ToArray());
        }

        public Task<IReadOnlyList<StoredActivitySession>> LoadSessionsAsync(
            string driverCardId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StoredActivitySession>>(_sessions
                .Where(pair => pair.Key.Card == driverCardId)
                .Select(pair => pair.Value)
                .OrderBy(session => session.SessionIndex)
                .ToArray());
    }
}

using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Application.Services;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Telemetry;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.Engine;
using ETS2Tachograph.RuleEngine.JourneyPlanning;

namespace ETS2Tachograph.Application.Tests;

public sealed class DeliveryPlannerServiceTests
{
    [Fact]
    public async Task Market_offer_uses_atomic_two_card_snapshot_without_history_writes()
    {
        var repository = new RecordingRepository();
        var crew = await CreateCrewAsync(repository, includeCoDriver: true);
        var service = new DeliveryPlannerService(crew);
        var writesBefore = repository.WriteCount;

        var result = await service.PlanMarketOfferAsync(new MarketOfferPlannerInput(
            InitialDrivingSlot: 1,
            DriveToPickupMinutes: 0,
            OfferExpiresInMinutes: 9_900,
            LoadedRouteDriveMinutes: 540,
            DeliveryWindowStart: At(GameWeekday.Monday, 1, 40),
            DeliveryWindowEnd: At(GameWeekday.Sunday, 23, 0),
            PickupWorkMinutes: 0,
            UnloadingWorkMinutes: 0,
            PostDeliveryWorkMinutes: 0,
            TightMarginThresholdMinutes: 60));

        Assert.Equal(DeliveryPlanVerdict.Take, result.Verdict);
        Assert.Equal(writesBefore, repository.WriteCount);
        Assert.True(result.SnapshotIdentity.MultiManningActive);
        Assert.Equal(1, result.SnapshotIdentity.Slot1.DriverSlot);
        Assert.Equal(2, result.SnapshotIdentity.Slot2.DriverSlot);
        Assert.Contains(result.Segments, segment => segment.DrivingSlot == 1);
        Assert.Contains(result.Segments, segment => segment.DrivingSlot == 2);
        Assert.Equal(1, repository.MaxConcurrentReads);
    }

    [Fact]
    public async Task Active_delivery_has_no_market_offer_expiry()
    {
        var repository = new RecordingRepository();
        var crew = await CreateCrewAsync(repository, includeCoDriver: true);
        var service = new DeliveryPlannerService(crew);

        var result = await service.PlanActiveDeliveryAsync(
            new ActiveDeliveryPlannerInput(
                InitialDrivingSlot: 1,
                RemainingLoadedRouteDriveMinutes: 60,
                DeliveryWindowStart: At(GameWeekday.Monday, 1, 40),
                DeliveryWindowEnd: At(GameWeekday.Monday, 16, 40),
                UnloadingWorkMinutes: 15,
                PostDeliveryWorkMinutes: 0,
                TightMarginThresholdMinutes: 60));

        Assert.Equal(DeliveryPlanningUseCase.ActiveDelivery, result.UseCase);
        Assert.Null(result.OfferExpiresAtGameMinuteExclusive);
        Assert.Equal(175, result.DeliveryCompletedAtGameMinute);
    }

    [Fact]
    public async Task Delivery_window_resolves_to_nearest_occurrences_from_snapshot()
    {
        var repository = new RecordingRepository();
        var crew = await CreateCrewAsync(repository, includeCoDriver: true);
        var service = new DeliveryPlannerService(crew);

        var result = await service.PlanActiveDeliveryAsync(
            new ActiveDeliveryPlannerInput(
                InitialDrivingSlot: 1,
                RemainingLoadedRouteDriveMinutes: 0,
                DeliveryWindowStart: At(GameWeekday.Saturday, 1, 16),
                DeliveryWindowEnd: At(GameWeekday.Thursday, 21, 54),
                UnloadingWorkMinutes: 0,
                PostDeliveryWorkMinutes: 0,
                TightMarginThresholdMinutes: 60));

        Assert.Equal(7_276, result.DeliveryWindowStartGameMinute);
        Assert.Equal(15_714, result.DeliveryWindowEndGameMinuteExclusive);
    }

    [Fact]
    public async Task New_telemetry_invalidates_two_card_result_identity()
    {
        var repository = new RecordingRepository();
        var crew = await CreateCrewAsync(repository, includeCoDriver: true);
        var service = new DeliveryPlannerService(crew);
        var result = await service.PlanActiveDeliveryAsync(
            new ActiveDeliveryPlannerInput(
                1,
                60,
                At(GameWeekday.Monday, 1, 40),
                At(GameWeekday.Monday, 16, 40),
                0,
                0,
                60));

        await crew.ProcessFrameAsync(Frame(101));

        Assert.False(service.IsCurrent(result.SnapshotIdentity));
    }

    [Fact]
    public async Task Missing_active_crew_returns_controlled_insufficient_data()
    {
        var repository = new RecordingRepository();
        var crew = await CreateCrewAsync(repository, includeCoDriver: false);
        var service = new DeliveryPlannerService(crew);

        var result = await service.PlanMarketOfferAsync(new MarketOfferPlannerInput(
            1,
            60,
            10_000,
            60,
            At(GameWeekday.Monday, 1, 40),
            At(GameWeekday.Monday, 16, 40),
            15,
            15,
            0,
            60));

        Assert.Equal(DeliveryPlanVerdict.Reject, result.Verdict);
        Assert.Equal(
            DeliveryPlanFailureReason.InsufficientData,
            result.FailureReason);
    }

    private static async Task<CrewTachographService> CreateCrewAsync(
        RecordingRepository repository,
        bool includeCoDriver)
    {
        var crew = new CrewTachographService(
            new CrewTachographEngine(),
            repository);
        await crew.RegisterCardAsync("S1");
        crew.InsertCard(TachographSlot.Driver, "S1");
        if (includeCoDriver)
        {
            await crew.RegisterCardAsync("S2");
            crew.InsertCard(TachographSlot.CoDriver, "S2");
        }
        await crew.ProcessFrameAsync(Frame(100));
        return crew;
    }

    private static TelemetryFrame Frame(long minute) => new(
        new GameTime(minute),
        DateTimeOffset.UnixEpoch.AddMinutes(minute),
        SpeedKph: 0,
        GamePaused: false,
        WorldGeneration: 7);

    private static GameWeekdayTime At(
        GameWeekday weekday,
        int hour,
        int minute) => new(weekday, hour, minute);

    private sealed class RecordingRepository : IActivityRepository
    {
        private readonly Dictionary<(string Card, int Session), StoredActivitySession>
            _sessions = [];

        internal int WriteCount { get; private set; }
        internal int MaxConcurrentReads { get; private set; }
        private int _activeReads;

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
                Records = session.Records
                    .Concat(records)
                    .DistinctBy(record => record.Id)
                    .ToArray()
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

        public async Task<IReadOnlyList<ActivityRecord>> LoadDriverHistoryAsync(
            string driverCardId,
            GameTime? from = null,
            GameTime? toExclusive = null,
            CancellationToken cancellationToken = default)
        {
            EnterRead();
            try
            {
                await Task.Delay(10, cancellationToken);
                return _sessions
                    .Where(pair => pair.Key.Card == driverCardId)
                    .SelectMany(pair => pair.Value.Records)
                    .ToArray();
            }
            finally
            {
                Interlocked.Decrement(ref _activeReads);
            }
        }

        public async Task<IReadOnlyList<ActivityGap>> LoadDriverGapsAsync(
            string driverCardId,
            GameTime? from = null,
            GameTime? toExclusive = null,
            CancellationToken cancellationToken = default)
        {
            EnterRead();
            try
            {
                await Task.Delay(10, cancellationToken);
                return [];
            }
            finally
            {
                Interlocked.Decrement(ref _activeReads);
            }
        }

        public Task<IReadOnlyList<StoredActivitySession>> LoadSessionsAsync(
            string driverCardId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StoredActivitySession>>(_sessions
                .Where(pair => pair.Key.Card == driverCardId)
                .Select(pair => pair.Value)
                .OrderBy(session => session.SessionIndex)
                .ToArray());

        private void EnterRead()
        {
            var active = Interlocked.Increment(ref _activeReads);
            MaxConcurrentReads = Math.Max(MaxConcurrentReads, active);
        }
    }
}

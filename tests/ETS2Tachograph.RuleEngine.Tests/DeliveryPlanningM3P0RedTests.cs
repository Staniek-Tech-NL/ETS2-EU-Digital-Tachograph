using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.RuleEngine.JourneyPlanning;

namespace ETS2Tachograph.RuleEngine.Tests;

public sealed class DeliveryPlanningM3P0RedTests
{
    private readonly DeliveryPlanningEngine _engine = new();

    [Fact(DisplayName = "M3-P0-01: delivery completion is derived from market components")]
    [Trait("Stage", "M3Red")]
    public void M3_P0_01()
    {
        var offer = Offer(
            driveToPickup: 60,
            pickup: 15,
            loadedDrive: 120,
            unloading: 30);

        var result = _engine.Plan(offer);

        Assert.DoesNotContain(
            typeof(MarketOffer).GetProperties(),
            property => property.Name is
                "RemainingDriveMinutes" or
                "DeliveryWindowMinutes");
        Assert.Equal(1_225, result.DeliveryCompletedAtGameMinute);
    }

    [Fact(DisplayName = "M3-P0-02: offer expiry is independent from the delivery window")]
    [Trait("Stage", "M3Red")]
    public void M3_P0_02()
    {
        var result = _engine.Plan(Offer(
            offerExpiresAt: 1_000,
            windowStart: 2_000,
            windowEnd: 3_000));

        Assert.Equal(DeliveryPlanFailureReason.OfferExpired, result.FailureReason);
        Assert.Equal(1_000, result.OfferExpiresAtGameMinuteExclusive);
        Assert.Equal(2_000, result.DeliveryWindowStartGameMinute);
        Assert.Equal(3_000, result.DeliveryWindowEndGameMinuteExclusive);
    }

    [Fact(DisplayName = "M3-P0-03: delivery window keeps separate start and exclusive end")]
    [Trait("Stage", "M3Red")]
    public void M3_P0_03()
    {
        var result = _engine.Plan(Offer(
            driveToPickup: 30,
            pickup: 15,
            loadedDrive: 30,
            unloading: 15,
            windowStart: 1_200,
            windowEnd: 1_260));

        var wait = Assert.Single(result.Segments, segment =>
            segment.Phase == DeliveryPlanPhase.WaitForDeliveryWindow);
        Assert.Equal(1_075, wait.StartGameMinute);
        Assert.Equal(1_200, wait.EndGameMinute);
        Assert.Equal(1_215, result.DeliveryCompletedAtGameMinute);
    }

    [Fact(DisplayName = "M3-P0-04: drive to pickup precedes pickup and loaded route")]
    [Trait("Stage", "M3Red")]
    public void M3_P0_04()
    {
        var result = _engine.Plan(Offer(
            driveToPickup: 45,
            pickup: 20,
            loadedDrive: 90));

        var relevant = result.Segments
            .Where(segment => segment.Phase is
                DeliveryPlanPhase.DriveToPickup or
                DeliveryPlanPhase.Pickup or
                DeliveryPlanPhase.DriveWithCargo)
            .ToList();
        Assert.Equal(
            [
                DeliveryPlanPhase.DriveToPickup,
                DeliveryPlanPhase.Pickup,
                DeliveryPlanPhase.DriveWithCargo
            ],
            relevant.Select(segment => segment.Phase));
        Assert.Equal(relevant[0].EndGameMinute, relevant[1].StartGameMinute);
        Assert.Equal(relevant[1].EndGameMinute, relevant[2].StartGameMinute);
    }

    [Fact(DisplayName = "M3-P0-05: market schedule delegates driving to the M2 crew model")]
    [Trait("Stage", "M3Red")]
    public void M3_P0_05()
    {
        var result = _engine.Plan(Offer(loadedDrive: 540));

        Assert.Contains(result.Segments, segment =>
            segment.Phase == DeliveryPlanPhase.DriveWithCargo &&
            segment.DrivingSlot == 1);
        Assert.Contains(result.Segments, segment =>
            segment.Phase == DeliveryPlanPhase.DriveWithCargo &&
            segment.DrivingSlot == 2);
    }

    [Fact(DisplayName = "M3-P0-06: qualified moving break does not stop the vehicle")]
    [Trait("Stage", "M3Red")]
    public void M3_P0_06()
    {
        var result = _engine.Plan(Offer(loadedDrive: 540));
        var loadedRoute = result.Segments
            .Where(segment => segment.Phase == DeliveryPlanPhase.DriveWithCargo)
            .ToList();

        Assert.Equal(540, loadedRoute.Sum(segment => segment.DurationMinutes));
        Assert.All(loadedRoute, segment => Assert.NotNull(segment.DrivingSlot));
        Assert.Contains(loadedRoute.Zip(loadedRoute.Skip(1)), pair =>
            pair.First.DrivingSlot == 1 &&
            pair.Second.DrivingSlot == 2 &&
            pair.First.EndGameMinute == pair.Second.StartGameMinute);
    }

    [Fact(DisplayName = "M3-P0-07: result distinguishes TAKE, TIGHT and REJECT")]
    [Trait("Stage", "M3Red")]
    public void M3_P0_07()
    {
        var take = _engine.Plan(Offer(
            loadedDrive: 60,
            windowEnd: 1_600,
            tightMargin: 60));
        var tight = _engine.Plan(Offer(
            loadedDrive: 60,
            windowEnd: 1_100,
            tightMargin: 60));
        var reject = _engine.Plan(Offer(
            loadedDrive: 120,
            windowEnd: 1_100,
            tightMargin: 60));

        Assert.Equal(DeliveryPlanVerdict.Take, take.Verdict);
        Assert.Equal(DeliveryPlanVerdict.Tight, tight.Verdict);
        Assert.Equal(DeliveryPlanVerdict.Reject, reject.Verdict);
    }

    [Theory(DisplayName = "M3-P0-08: calendar wait uses the raw snapshot week offset")]
    [InlineData(-1)]
    [InlineData(1)]
    [Trait("Stage", "M3Red")]
    public void M3_P0_08(int offset)
    {
        const long now = 10_000;
        var snapshot = Snapshot(
            start: now,
            weekOffset: offset,
            weeklyDrivingMinutes: 56 * 60);
        var result = _engine.Plan(Offer(
            snapshot: snapshot,
            loadedDrive: 1));

        var calendarWait = Assert.Single(result.Segments, segment =>
            segment.Phase == DeliveryPlanPhase.RegulatoryInterruption &&
            segment.RegulatoryReason ==
            JourneyPlanSegmentReason.WaitForNewRegulatoryWeek);
        Assert.Equal(
            GameWeek.From(new GameTime(now), offset)
                .GetBounds()
                .EndGameMinuteExclusive,
            calendarWait.EndGameMinute);
        Assert.Equal(offset, result.WeekEpochOffsetDays);
    }

    private static MarketOffer Offer(
        CrewJourneyPlanningSnapshot? snapshot = null,
        int driveToPickup = 0,
        long offerExpiresAt = 10_000,
        int loadedDrive = 0,
        long windowStart = 1_000,
        long windowEnd = 10_000,
        int pickup = 0,
        int unloading = 0,
        int postDeliveryWork = 0,
        int tightMargin = 60) => new(
        snapshot ?? Snapshot(),
        InitialDrivingSlot: 1,
        DriveToPickupMinutes: driveToPickup,
        OfferExpiresAtGameMinuteExclusive: offerExpiresAt,
        LoadedRouteDriveMinutes: loadedDrive,
        DeliveryWindowStartGameMinute: windowStart,
        DeliveryWindowEndGameMinuteExclusive: windowEnd,
        PickupWorkMinutes: pickup,
        UnloadingWorkMinutes: unloading,
        PostDeliveryWorkMinutes: postDeliveryWork,
        TightMarginThresholdMinutes: tightMargin,
        JourneyPlanningLimits.Default);

    private static CrewJourneyPlanningSnapshot Snapshot(
        long start = 1_000,
        int weekOffset = 0,
        long weeklyDrivingMinutes = 0)
    {
        var state = new RegulationState
        {
            WeeklyDrivingMinutes = weeklyDrivingMinutes,
            PreviousWeekDrivingMinutes = 0,
            MinutesUntilDailyRestDeadline = 1_800,
            MinutesUntilWeeklyRestDeadline = 8_640
        };
        return new CrewJourneyPlanningSnapshot(
            start,
            WorldGeneration: 7,
            WeekEpochOffsetDays: weekOffset,
            MultiManningActive: true,
            TelemetryAvailable: true,
            Driver(1, state),
            Driver(2, state));
    }

    private static CrewDriverPlanningSnapshot Driver(
        int slot,
        RegulationState state) => new(
        slot,
        $"CARD-S{slot}",
        Guid.Parse(slot == 1
            ? "11111111-2222-2222-2222-222222222222"
            : "22222222-2222-2222-2222-222222222222"),
        HistoryHighWaterMark: 42 + slot,
        new RegulationEvaluation(state, [], []),
        History: [],
        Gaps: []);
}

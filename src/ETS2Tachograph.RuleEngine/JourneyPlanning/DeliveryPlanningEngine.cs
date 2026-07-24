using ETS2Tachograph.Core.Enums;

namespace ETS2Tachograph.RuleEngine.JourneyPlanning;

/// <summary>
/// M3-R1 seam for the redesigned delivery planner. The blocking tests define
/// the required behavior before M3-R2 supplies the implementation.
/// </summary>
public sealed class DeliveryPlanningEngine
{
    private readonly CrewJourneyPlanningEngine _crewEngine = new();

    public DeliveryPlanResult Plan(MarketOffer offer)
    {
        ArgumentNullException.ThrowIfNull(offer);
        Validate(
            offer.Snapshot,
            offer.InitialDrivingSlot,
            offer.DriveToPickupMinutes,
            offer.LoadedRouteDriveMinutes,
            offer.DeliveryWindowStartGameMinute,
            offer.DeliveryWindowEndGameMinuteExclusive,
            offer.PickupWorkMinutes,
            offer.UnloadingWorkMinutes,
            offer.PostDeliveryWorkMinutes,
            offer.TightMarginThresholdMinutes);

        if (offer.Snapshot.StartGameMinute >=
            offer.OfferExpiresAtGameMinuteExclusive)
        {
            return Failure(
                DeliveryPlanningUseCase.MarketOffer,
                DeliveryPlanFailureReason.OfferExpired,
                offer.Snapshot,
                offer.OfferExpiresAtGameMinuteExclusive,
                offer.DeliveryWindowStartGameMinute,
                offer.DeliveryWindowEndGameMinuteExclusive);
        }

        var state = offer.Snapshot;
        var slot = offer.InitialDrivingSlot;
        var segments = new List<DeliveryPlanSegment>();
        if (!TryAddDriving(
                ref state,
                ref slot,
                offer.DriveToPickupMinutes,
                DeliveryPlanPhase.DriveToPickup,
                offer.Limits,
                segments,
                out var drivingFailure))
        {
            return Failure(
                DeliveryPlanningUseCase.MarketOffer,
                drivingFailure,
                offer.Snapshot,
                offer.OfferExpiresAtGameMinuteExclusive,
                offer.DeliveryWindowStartGameMinute,
                offer.DeliveryWindowEndGameMinuteExclusive,
                segments);
        }

        var pickupStartedAt = state.StartGameMinute;
        AddStationary(
            ref state,
            DeliveryPlanPhase.Pickup,
            offer.PickupWorkMinutes,
            DriverActivity.OtherWork,
            segments);
        var pickupCompletedAt = state.StartGameMinute;
        if (pickupCompletedAt >= offer.OfferExpiresAtGameMinuteExclusive)
        {
            return Failure(
                DeliveryPlanningUseCase.MarketOffer,
                DeliveryPlanFailureReason.OfferExpired,
                offer.Snapshot,
                offer.OfferExpiresAtGameMinuteExclusive,
                offer.DeliveryWindowStartGameMinute,
                offer.DeliveryWindowEndGameMinuteExclusive,
                segments,
                pickupStartedAt,
                pickupCompletedAt);
        }

        if (!TryAddDriving(
                ref state,
                ref slot,
                offer.LoadedRouteDriveMinutes,
                DeliveryPlanPhase.DriveWithCargo,
                offer.Limits,
                segments,
                out drivingFailure))
        {
            return Failure(
                DeliveryPlanningUseCase.MarketOffer,
                drivingFailure,
                offer.Snapshot,
                offer.OfferExpiresAtGameMinuteExclusive,
                offer.DeliveryWindowStartGameMinute,
                offer.DeliveryWindowEndGameMinuteExclusive,
                segments,
                pickupStartedAt,
                pickupCompletedAt);
        }

        return Complete(
            DeliveryPlanningUseCase.MarketOffer,
            offer.Snapshot,
            state,
            offer.OfferExpiresAtGameMinuteExclusive,
            offer.DeliveryWindowStartGameMinute,
            offer.DeliveryWindowEndGameMinuteExclusive,
            offer.UnloadingWorkMinutes,
            offer.PostDeliveryWorkMinutes,
            offer.TightMarginThresholdMinutes,
            segments,
            pickupStartedAt,
            pickupCompletedAt);
    }

    public DeliveryPlanResult Plan(ActiveDelivery delivery)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        Validate(
            delivery.Snapshot,
            delivery.InitialDrivingSlot,
            delivery.RemainingLoadedRouteDriveMinutes,
            delivery.DeliveryWindowStartGameMinute,
            delivery.DeliveryWindowEndGameMinuteExclusive,
            delivery.UnloadingWorkMinutes,
            delivery.PostDeliveryWorkMinutes,
            delivery.TightMarginThresholdMinutes);

        var state = delivery.Snapshot;
        var slot = delivery.InitialDrivingSlot;
        var segments = new List<DeliveryPlanSegment>();
        if (!TryAddDriving(
                ref state,
                ref slot,
                delivery.RemainingLoadedRouteDriveMinutes,
                DeliveryPlanPhase.DriveWithCargo,
                delivery.Limits,
                segments,
                out var drivingFailure))
        {
            return Failure(
                DeliveryPlanningUseCase.ActiveDelivery,
                drivingFailure,
                delivery.Snapshot,
                offerExpiresAt: null,
                delivery.DeliveryWindowStartGameMinute,
                delivery.DeliveryWindowEndGameMinuteExclusive,
                segments);
        }

        return Complete(
            DeliveryPlanningUseCase.ActiveDelivery,
            delivery.Snapshot,
            state,
            offerExpiresAt: null,
            delivery.DeliveryWindowStartGameMinute,
            delivery.DeliveryWindowEndGameMinuteExclusive,
            delivery.UnloadingWorkMinutes,
            delivery.PostDeliveryWorkMinutes,
            delivery.TightMarginThresholdMinutes,
            segments,
            pickupStartedAt: null,
            pickupCompletedAt: null);
    }

    private bool TryAddDriving(
        ref CrewJourneyPlanningSnapshot snapshot,
        ref int drivingSlot,
        int minutes,
        DeliveryPlanPhase drivingPhase,
        JourneyPlanningLimits limits,
        List<DeliveryPlanSegment> target,
        out DeliveryPlanFailureReason failure)
    {
        failure = DeliveryPlanFailureReason.None;
        if (minutes == 0)
            return true;

        var result = _crewEngine.Plan(new CrewJourneyPlanRequest(
            JourneyPlanningMode.MultiManningCrew,
            snapshot,
            drivingSlot,
            minutes,
            DeliveryWindowMinutes: limits.MaximumElapsedMinutes,
            OperationalBufferMinutes: 0,
            JourneyOperationalBufferPolicy.OtherWorkAfterArrival,
            limits));
        target.AddRange(result.Segments.Select(segment => new DeliveryPlanSegment(
            segment.DrivingSlot is null
                ? DeliveryPlanPhase.RegulatoryInterruption
                : drivingPhase,
            segment.StartGameMinute,
            segment.EndGameMinute,
            segment.DrivingSlot,
            segment.Slot1Activity,
            segment.Slot2Activity,
            segment.DrivingSlot is null ? segment.Reason : null)));

        if (result.Status != JourneyPlanStatus.MeetsDeadline ||
            result.EarliestArrivalGameMinute is null)
        {
            failure = result.Status switch
            {
                JourneyPlanStatus.CalculationLimitReached =>
                    DeliveryPlanFailureReason.CalculationLimitReached,
                JourneyPlanStatus.InsufficientData or
                    JourneyPlanStatus.BlockedByGap =>
                    DeliveryPlanFailureReason.InsufficientData,
                JourneyPlanStatus.StaleSnapshot =>
                    DeliveryPlanFailureReason.StaleSnapshot,
                _ => DeliveryPlanFailureReason.NoLegalContinuation
            };
            return false;
        }

        snapshot = AdvanceSnapshot(snapshot, result);
        drivingSlot = result.Segments.LastOrDefault(segment =>
            segment.DrivingSlot is not null)?.DrivingSlot ?? drivingSlot;
        return true;
    }

    private static DeliveryPlanResult Complete(
        DeliveryPlanningUseCase useCase,
        CrewJourneyPlanningSnapshot original,
        CrewJourneyPlanningSnapshot afterDriving,
        long? offerExpiresAt,
        long windowStart,
        long windowEnd,
        int unloadingMinutes,
        int postDeliveryWorkMinutes,
        int tightMarginThresholdMinutes,
        List<DeliveryPlanSegment> segments,
        long? pickupStartedAt,
        long? pickupCompletedAt)
    {
        var arrivedAtDelivery = afterDriving.StartGameMinute;
        var state = afterDriving;
        if (state.StartGameMinute < windowStart)
        {
            AddStationary(
                ref state,
                DeliveryPlanPhase.WaitForDeliveryWindow,
                checked((int)(windowStart - state.StartGameMinute)),
                DriverActivity.BreakOrRest,
                segments);
        }

        AddStationary(
            ref state,
            DeliveryPlanPhase.Unloading,
            unloadingMinutes,
            DriverActivity.OtherWork,
            segments);
        var deliveryCompletedAt = state.StartGameMinute;
        AddStationary(
            ref state,
            DeliveryPlanPhase.PostDeliveryWork,
            postDeliveryWorkMinutes,
            DriverActivity.OtherWork,
            segments);

        var margin = checked((int)(windowEnd - deliveryCompletedAt));
        var meetsWindow = deliveryCompletedAt < windowEnd;
        var verdict = !meetsWindow
            ? DeliveryPlanVerdict.Reject
            : margin < tightMarginThresholdMinutes
                ? DeliveryPlanVerdict.Tight
                : DeliveryPlanVerdict.Take;
        return new DeliveryPlanResult(
            useCase,
            verdict,
            meetsWindow
                ? DeliveryPlanFailureReason.None
                : DeliveryPlanFailureReason.DeliveryWindowMissed,
            original.StartGameMinute,
            offerExpiresAt,
            windowStart,
            windowEnd,
            pickupStartedAt,
            pickupCompletedAt,
            arrivedAtDelivery,
            deliveryCompletedAt,
            margin,
            original.WeekEpochOffsetDays,
            segments,
            Warnings: [],
            CrewDeliveryPlanSnapshotIdentity.From(original));
    }

    private static void AddStationary(
        ref CrewJourneyPlanningSnapshot snapshot,
        DeliveryPlanPhase phase,
        int duration,
        DriverActivity activity,
        List<DeliveryPlanSegment> segments)
    {
        if (duration == 0)
            return;
        var end = checked(snapshot.StartGameMinute + duration);
        segments.Add(new DeliveryPlanSegment(
            phase,
            snapshot.StartGameMinute,
            end,
            DrivingSlot: null,
            activity,
            snapshot.MultiManningActive ? activity : null));
        snapshot = AdvanceStationarySnapshot(snapshot, end, duration, activity);
    }

    private static CrewJourneyPlanningSnapshot AdvanceSnapshot(
        CrewJourneyPlanningSnapshot snapshot,
        CrewJourneyPlanResult result) => snapshot with
    {
        StartGameMinute = result.EarliestArrivalGameMinute!.Value,
        Slot1 = AdvanceDriver(snapshot.Slot1, result.Slot1),
        Slot2 = AdvanceDriver(snapshot.Slot2, result.Slot2)
    };

    private static CrewDriverPlanningSnapshot AdvanceDriver(
        CrewDriverPlanningSnapshot driver,
        CrewDriverPlanSummary summary)
    {
        var previous = driver.Evaluation.State;
        var state = previous with
        {
            ContinuousDrivingMinutes = summary.ContinuousDrivingMinutes,
            DailyDrivingMinutes = summary.DailyDrivingMinutes,
            WeeklyDrivingMinutes = summary.WeeklyDrivingMinutes,
            PreviousWeekDrivingMinutes = summary.PreviousWeekDrivingMinutes,
            CurrentContinuousBreakMinutes = summary.CurrentContinuousBreakMinutes,
            MinutesUntilDailyRestDeadline = summary.MinutesUntilDailyRestDeadline,
            MinutesUntilWeeklyRestDeadline = summary.MinutesUntilWeeklyRestDeadline,
            DailyExtensionsUsedThisWeek = summary.DailyDrivingExtensionsUsed,
            ReducedDailyRestsSinceWeeklyRest = summary.ReducedDailyRestsUsed
        };
        return driver with
        {
            Evaluation = new RegulationEvaluation(
                state,
                driver.Evaluation.Violations,
                driver.Evaluation.CompensationObligations,
                driver.Evaluation.RestAllocations)
        };
    }

    private static CrewJourneyPlanningSnapshot AdvanceStationarySnapshot(
        CrewJourneyPlanningSnapshot snapshot,
        long end,
        int duration,
        DriverActivity activity) => snapshot with
    {
        StartGameMinute = end,
        Slot1 = AdvanceStationaryDriver(snapshot.Slot1, duration, activity),
        Slot2 = AdvanceStationaryDriver(snapshot.Slot2, duration, activity)
    };

    private static CrewDriverPlanningSnapshot AdvanceStationaryDriver(
        CrewDriverPlanningSnapshot driver,
        int duration,
        DriverActivity activity)
    {
        var previous = driver.Evaluation.State;
        var breakMinutes = activity == DriverActivity.BreakOrRest
            ? checked(previous.CurrentContinuousBreakMinutes + duration)
            : 0;
        var state = previous with
        {
            CurrentContinuousBreakMinutes = breakMinutes,
            ContinuousDrivingMinutes = breakMinutes >= 45
                ? 0
                : previous.ContinuousDrivingMinutes,
            MinutesUntilDailyRestDeadline = Math.Max(
                0,
                previous.MinutesUntilDailyRestDeadline - duration),
            MinutesUntilWeeklyRestDeadline = Math.Max(
                0,
                previous.MinutesUntilWeeklyRestDeadline - duration)
        };
        return driver with
        {
            Evaluation = new RegulationEvaluation(
                state,
                driver.Evaluation.Violations,
                driver.Evaluation.CompensationObligations,
                driver.Evaluation.RestAllocations)
        };
    }

    private static DeliveryPlanResult Failure(
        DeliveryPlanningUseCase useCase,
        DeliveryPlanFailureReason reason,
        CrewJourneyPlanningSnapshot snapshot,
        long? offerExpiresAt,
        long windowStart,
        long windowEnd,
        IReadOnlyList<DeliveryPlanSegment>? segments = null,
        long? pickupStartedAt = null,
        long? pickupCompletedAt = null) => new(
        useCase,
        DeliveryPlanVerdict.Reject,
        reason,
        snapshot.StartGameMinute,
        offerExpiresAt,
        windowStart,
        windowEnd,
        pickupStartedAt,
        pickupCompletedAt,
        ArrivedAtDeliveryGameMinute: null,
        DeliveryCompletedAtGameMinute: null,
        MarginMinutes: 0,
        snapshot.WeekEpochOffsetDays,
        segments ?? [],
        Warnings: [],
        CrewDeliveryPlanSnapshotIdentity.From(snapshot));

    private static void Validate(
        CrewJourneyPlanningSnapshot snapshot,
        int initialDrivingSlot,
        int firstDuration,
        long windowStart,
        long windowEnd,
        params int[] durations)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (initialDrivingSlot is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(initialDrivingSlot));
        if (firstDuration < 0 || durations.Any(duration => duration < 0))
            throw new ArgumentOutOfRangeException(nameof(firstDuration));
        if (windowEnd <= windowStart)
            throw new ArgumentOutOfRangeException(nameof(windowEnd));
    }

    private static void Validate(
        CrewJourneyPlanningSnapshot snapshot,
        int initialDrivingSlot,
        int firstDuration,
        int secondDuration,
        long windowStart,
        long windowEnd,
        params int[] durations) =>
        Validate(
            snapshot,
            initialDrivingSlot,
            firstDuration,
            windowStart,
            windowEnd,
            [secondDuration, .. durations]);
}

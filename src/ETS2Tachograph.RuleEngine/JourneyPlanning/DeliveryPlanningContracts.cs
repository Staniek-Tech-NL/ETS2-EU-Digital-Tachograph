using ETS2Tachograph.Core.Enums;

namespace ETS2Tachograph.RuleEngine.JourneyPlanning;

public enum DeliveryPlanningUseCase
{
    MarketOffer,
    ActiveDelivery
}

public enum DeliveryPlanVerdict
{
    Take,
    Tight,
    Reject
}

public enum DeliveryPlanFailureReason
{
    None,
    OfferExpired,
    DeliveryWindowMissed,
    NoLegalContinuation,
    InsufficientData,
    StaleSnapshot,
    CalculationLimitReached,
    NotImplemented
}

public enum DeliveryPlanPhase
{
    DriveToPickup,
    Pickup,
    DriveWithCargo,
    WaitForDeliveryWindow,
    Unloading,
    PostDeliveryWork,
    RegulatoryInterruption
}

public sealed record MarketOffer(
    CrewJourneyPlanningSnapshot Snapshot,
    int InitialDrivingSlot,
    int DriveToPickupMinutes,
    long OfferExpiresAtGameMinuteExclusive,
    int LoadedRouteDriveMinutes,
    long DeliveryWindowStartGameMinute,
    long DeliveryWindowEndGameMinuteExclusive,
    int PickupWorkMinutes,
    int UnloadingWorkMinutes,
    int PostDeliveryWorkMinutes,
    int TightMarginThresholdMinutes,
    JourneyPlanningLimits Limits);

public sealed record ActiveDelivery(
    CrewJourneyPlanningSnapshot Snapshot,
    int InitialDrivingSlot,
    int RemainingLoadedRouteDriveMinutes,
    long DeliveryWindowStartGameMinute,
    long DeliveryWindowEndGameMinuteExclusive,
    int UnloadingWorkMinutes,
    int PostDeliveryWorkMinutes,
    int TightMarginThresholdMinutes,
    JourneyPlanningLimits Limits);

public sealed record DeliveryPlanSegment(
    DeliveryPlanPhase Phase,
    long StartGameMinute,
    long EndGameMinute,
    int? DrivingSlot,
    DriverActivity Slot1Activity,
    DriverActivity? Slot2Activity,
    JourneyPlanSegmentReason? RegulatoryReason = null)
{
    public int DurationMinutes => checked((int)(EndGameMinute - StartGameMinute));
}

public sealed record DeliveryPlanResult(
    DeliveryPlanningUseCase UseCase,
    DeliveryPlanVerdict Verdict,
    DeliveryPlanFailureReason FailureReason,
    long StartGameMinute,
    long? OfferExpiresAtGameMinuteExclusive,
    long DeliveryWindowStartGameMinute,
    long DeliveryWindowEndGameMinuteExclusive,
    long? PickupStartedAtGameMinute,
    long? PickupCompletedAtGameMinute,
    long? ArrivedAtDeliveryGameMinute,
    long? DeliveryCompletedAtGameMinute,
    int MarginMinutes,
    int WeekEpochOffsetDays,
    IReadOnlyList<DeliveryPlanSegment> Segments,
    IReadOnlyList<JourneyPlanWarning> Warnings);

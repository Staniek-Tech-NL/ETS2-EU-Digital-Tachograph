namespace ETS2Tachograph.RuleEngine.JourneyPlanning;

/// <summary>
/// M3-R1 seam for the redesigned delivery planner. The blocking tests define
/// the required behavior before M3-R2 supplies the implementation.
/// </summary>
public sealed class DeliveryPlanningEngine
{
    public DeliveryPlanResult Plan(MarketOffer offer)
    {
        ArgumentNullException.ThrowIfNull(offer);
        return NotImplemented(
            DeliveryPlanningUseCase.MarketOffer,
            offer.Snapshot,
            offer.OfferExpiresAtGameMinuteExclusive,
            offer.DeliveryWindowStartGameMinute,
            offer.DeliveryWindowEndGameMinuteExclusive);
    }

    public DeliveryPlanResult Plan(ActiveDelivery delivery)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        return NotImplemented(
            DeliveryPlanningUseCase.ActiveDelivery,
            delivery.Snapshot,
            offerExpiresAt: null,
            delivery.DeliveryWindowStartGameMinute,
            delivery.DeliveryWindowEndGameMinuteExclusive);
    }

    private static DeliveryPlanResult NotImplemented(
        DeliveryPlanningUseCase useCase,
        CrewJourneyPlanningSnapshot snapshot,
        long? offerExpiresAt,
        long windowStart,
        long windowEnd) => new(
        useCase,
        DeliveryPlanVerdict.Reject,
        DeliveryPlanFailureReason.NotImplemented,
        snapshot.StartGameMinute,
        offerExpiresAt,
        windowStart,
        windowEnd,
        PickupStartedAtGameMinute: null,
        PickupCompletedAtGameMinute: null,
        ArrivedAtDeliveryGameMinute: null,
        DeliveryCompletedAtGameMinute: null,
        MarginMinutes: 0,
        snapshot.WeekEpochOffsetDays,
        Segments: [],
        Warnings: []);
}

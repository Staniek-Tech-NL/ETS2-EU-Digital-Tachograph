namespace ETS2Tachograph.RuleEngine.JourneyPlanning;

/// <summary>
/// Public boundary reserved for the M2 event-driven implementation.
/// </summary>
public sealed class JourneyPlanningEngine
{
    public JourneyPlanResult Plan(JourneyPlanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        throw new NotImplementedException(
            "The journey-planning algorithm is intentionally deferred to milestone M2.");
    }
}

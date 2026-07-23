namespace ETS2Tachograph.RuleEngine;

public sealed record RegulationEvaluation
{
    public RegulationEvaluation(
        RegulationState state,
        IReadOnlyList<RuleViolation> violations,
        IReadOnlyList<WeeklyRestCompensation> compensationObligations,
        IReadOnlyList<RestAllocationProjection>? restAllocations = null)
    {
        State = state;
        Violations = violations;
        CompensationObligations = compensationObligations;
        Compensations = compensationObligations.Where(item => item.IsOpen).ToList();
        CompensationSummary = global::ETS2Tachograph.RuleEngine.CompensationSummary.From(
            compensationObligations);
        RestAllocations = restAllocations ?? [];
    }

    public RegulationState State { get; }
    public IReadOnlyList<RuleViolation> Violations { get; }
    public IReadOnlyList<WeeklyRestCompensation> CompensationObligations { get; }
    /// <summary>Compatibility projection containing only currently open obligations.</summary>
    public IReadOnlyList<WeeklyRestCompensation> Compensations { get; }
    public CompensationSummary CompensationSummary { get; }
    public IReadOnlyList<RestAllocationProjection> RestAllocations { get; }
    public IReadOnlyList<RestAllocationProjection> PendingRestAllocations =>
        RestAllocations.Where(item => item.IsPending).ToList();
    public IReadOnlyList<QualifiedRestPeriod> QualifiedRests { get; init; } = [];
}

namespace ETS2Tachograph.RuleEngine;

public sealed record RegulationEvaluation
{
    public RegulationEvaluation(
        RegulationState state,
        IReadOnlyList<RuleViolation> violations,
        IReadOnlyList<WeeklyRestCompensation> compensationObligations)
    {
        State = state;
        Violations = violations;
        CompensationObligations = compensationObligations;
        Compensations = compensationObligations.Where(item => item.IsOpen).ToList();
        CompensationSummary = global::ETS2Tachograph.RuleEngine.CompensationSummary.From(
            compensationObligations);
    }

    public RegulationState State { get; }
    public IReadOnlyList<RuleViolation> Violations { get; }
    public IReadOnlyList<WeeklyRestCompensation> CompensationObligations { get; }
    /// <summary>Compatibility projection containing only currently open obligations.</summary>
    public IReadOnlyList<WeeklyRestCompensation> Compensations { get; }
    public CompensationSummary CompensationSummary { get; }
    public IReadOnlyList<QualifiedRestPeriod> QualifiedRests { get; init; } = [];
}

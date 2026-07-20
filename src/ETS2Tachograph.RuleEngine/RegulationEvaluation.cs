namespace ETS2Tachograph.RuleEngine;

public sealed record RegulationEvaluation
{
    public RegulationEvaluation(
        RegulationState state,
        IReadOnlyList<RuleViolation> violations,
        IReadOnlyList<WeeklyRestCompensation> compensations)
    {
        State = state;
        Violations = violations;
        Compensations = compensations;
        CompensationSummary = global::ETS2Tachograph.RuleEngine.CompensationSummary.From(compensations);
    }

    public RegulationState State { get; }
    public IReadOnlyList<RuleViolation> Violations { get; }
    public IReadOnlyList<WeeklyRestCompensation> Compensations { get; }
    public CompensationSummary CompensationSummary { get; }
    public IReadOnlyList<QualifiedRestPeriod> QualifiedRests { get; init; } = [];
}

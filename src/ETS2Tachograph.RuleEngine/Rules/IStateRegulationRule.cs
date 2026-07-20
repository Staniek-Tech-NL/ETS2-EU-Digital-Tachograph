namespace ETS2Tachograph.RuleEngine.Rules;

public interface IStateRegulationRule
{
    IReadOnlyList<RuleViolation> Evaluate(RegulationRuleInput input);
}

public sealed record RegulationRuleInput(
    RegulationState State,
    ETS2Tachograph.Core.Time.GameTime Now,
    IReadOnlyList<WeeklyRestCompensation> Compensations,
    bool WeeklyPatternInvalid);

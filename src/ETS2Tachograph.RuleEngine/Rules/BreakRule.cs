namespace ETS2Tachograph.RuleEngine.Rules;

public sealed class BreakRule : IStateRegulationRule
{
    public IReadOnlyList<RuleViolation> Evaluate(RegulationRuleInput input) =>
        input.State.ContinuousDrivingMinutes > 270
            ?
            [
                new RuleViolation(
                    ViolationType.ContinuousDrivingExceeded,
                    "Art. 7",
                    "Continuous driving exceeded 4 hours 30 minutes.",
                    input.Now,
                    input.State.ContinuousDrivingMinutes - 270)
            ]
            : [];
}

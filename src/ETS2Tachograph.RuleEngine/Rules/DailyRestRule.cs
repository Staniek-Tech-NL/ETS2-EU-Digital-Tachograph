namespace ETS2Tachograph.RuleEngine.Rules;

public sealed class DailyRestRule : IStateRegulationRule
{
    public IReadOnlyList<RuleViolation> Evaluate(RegulationRuleInput input)
    {
        var result = new List<RuleViolation>();
        if (input.State.MinutesUntilDailyRestDeadline < 0)
        {
            result.Add(new(
                ViolationType.DailyRestMissing,
                "Art. 8(2)/(5)",
                "A qualifying daily rest was not completed within the daily window.",
                input.Now,
                -input.State.MinutesUntilDailyRestDeadline));
        }

        if (input.State.ReducedDailyRestsSinceWeeklyRest > 3)
        {
            result.Add(new(
                ViolationType.TooManyReducedDailyRests,
                "Art. 8(4)",
                "More than three reduced daily rests occurred between weekly rests.",
                input.Now));
        }

        return result;
    }
}

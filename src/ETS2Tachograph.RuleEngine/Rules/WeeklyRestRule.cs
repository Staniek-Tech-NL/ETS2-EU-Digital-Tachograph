namespace ETS2Tachograph.RuleEngine.Rules;

public sealed class WeeklyRestRule : IStateRegulationRule
{
    public IReadOnlyList<RuleViolation> Evaluate(RegulationRuleInput input)
    {
        var result = new List<RuleViolation>();
        if (input.State.MinutesUntilWeeklyRestDeadline < 0)
        {
            result.Add(new(
                ViolationType.WeeklyRestMissing,
                "Art. 8(6)",
                "A weekly rest did not start within six 24-hour periods.",
                input.Now,
                -input.State.MinutesUntilWeeklyRestDeadline));
        }

        if (input.WeeklyPatternInvalid)
        {
            result.Add(new(
                ViolationType.WeeklyRestPatternInvalid,
                "Art. 8(6)",
                "Two completed consecutive weeks do not contain the required weekly-rest pattern.",
                input.Now));
        }

        if (input.Compensations.Any(compensation => compensation.IsOverdue))
        {
            result.Add(new(
                ViolationType.WeeklyRestCompensationOverdue,
                "Art. 8(6b)",
                "Compensation for a reduced weekly rest is overdue.",
                input.Now));
        }

        return result;
    }
}

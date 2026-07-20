namespace ETS2Tachograph.RuleEngine.Rules;

public sealed class DrivingLimitsRule : IStateRegulationRule
{
    public IReadOnlyList<RuleViolation> Evaluate(RegulationRuleInput input)
    {
        var state = input.State;
        var result = new List<RuleViolation>();
        var dailyLimit = state.DailyExtensionsUsedThisWeek <= 2 ? 600 : 540;

        AddLimit(result, state.DailyDrivingMinutes, dailyLimit,
            ViolationType.DailyDrivingExceeded, "Art. 6(1)",
            "Daily driving time exceeded the available limit.", input.Now);
        if (state.DailyExtensionsUsedThisWeek > 2)
        {
            result.Add(new(
                ViolationType.TooManyDailyExtensions,
                "Art. 6(1)",
                "The 10-hour daily extension was used more than twice this week.",
                input.Now));
        }

        AddLimit(result, state.WeeklyDrivingMinutes, 3_360,
            ViolationType.WeeklyDrivingExceeded, "Art. 6(2)",
            "Weekly driving time exceeded 56 hours.", input.Now);
        AddLimit(result, state.FortnightlyDrivingMinutes, 5_400,
            ViolationType.FortnightlyDrivingExceeded, "Art. 6(3)",
            "Driving in two consecutive weeks exceeded 90 hours.", input.Now);
        return result;
    }

    private static void AddLimit(
        List<RuleViolation> result,
        long actual,
        long limit,
        ViolationType type,
        string article,
        string message,
        ETS2Tachograph.Core.Time.GameTime now)
    {
        if (actual > limit)
        {
            result.Add(new(type, article, message, now, actual - limit));
        }
    }
}

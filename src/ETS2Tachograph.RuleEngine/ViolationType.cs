namespace ETS2Tachograph.RuleEngine;

public enum ViolationType
{
    ContinuousDrivingExceeded,
    MissingRequiredBreak,
    DailyDrivingExceeded,
    WeeklyDrivingExceeded,
    FortnightlyDrivingExceeded,
    TooManyDailyExtensions,
    DailyRestMissing,
    TooManyReducedDailyRests,
    WeeklyRestMissing,
    WeeklyRestPatternInvalid,
    WeeklyRestCompensationOverdue
}

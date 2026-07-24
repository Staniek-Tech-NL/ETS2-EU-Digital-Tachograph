using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.RuleEngine;

public sealed record RegulationState
{
    public long ContinuousDrivingMinutes { get; init; }
    public long CurrentContinuousBreakMinutes { get; init; }
    public long DailyDrivingMinutes { get; init; }
    public long DailyWorkMinutes { get; init; }
    public long WeeklyDrivingMinutes { get; init; }
    public long PreviousWeekDrivingMinutes { get; init; }
    public long FortnightlyDrivingMinutes => WeeklyDrivingMinutes + PreviousWeekDrivingMinutes;
    public int DailyExtensionsUsedThisWeek { get; init; }
    public int ReducedDailyRestsSinceWeeklyRest { get; init; }
    public long MinutesUntilBreak { get; init; }
    public long MinutesUntilDailyRestDeadline { get; init; }
    public long DailyRestCompletionDeadlineGameMinute { get; init; }
    public long MinutesUntilWeeklyRestDeadline { get; init; }
    public long? WeeklyRestWindowElapsedMinutes { get; init; }
    public long? WeeklyRestStartDeadlineGameMinute { get; init; }
    public GameTime? LastDailyRestResetAt { get; init; }
    public bool PendingRestAllocation { get; init; }
}

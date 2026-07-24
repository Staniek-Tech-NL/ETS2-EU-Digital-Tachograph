namespace ETS2Tachograph.RuleEngine.JourneyPlanning;

internal sealed class JourneyPlanningState
{
    internal required long CurrentGameMinute { get; set; }
    internal required int RemainingDriveMinutes { get; set; }
    internal required long ContinuousDrivingMinutes { get; set; }
    internal required long DailyDrivingMinutes { get; set; }
    internal required long WeeklyDrivingMinutes { get; set; }
    internal required long PreviousWeekDrivingMinutes { get; set; }
    internal required int DailyExtensionsUsed { get; set; }
    internal required int ReducedDailyRestsUsed { get; set; }
    internal required long DailyRestCompletionDeadline { get; set; }
    internal required long WeeklyRestStartDeadline { get; set; }
    internal required bool MultiManningActive { get; init; }
    internal required bool ReducedWeeklyRestSupported { get; init; }
    internal required bool ExistingSplitBreakAvailable { get; set; }
    internal int VisitedStates { get; set; }
    internal long? ArrivalGameMinute { get; set; }
    internal List<JourneyPlanSegment> Segments { get; } = [];
    internal HashSet<JourneyPlanningStateKey> SeenStates { get; } = [];
}

internal readonly record struct JourneyPlanningStateKey(
    long CurrentGameMinute,
    int RemainingDriveMinutes,
    long ContinuousDrivingMinutes,
    long DailyDrivingMinutes,
    long WeeklyDrivingMinutes,
    long PreviousWeekDrivingMinutes,
    int DailyExtensionsUsed,
    int ReducedDailyRestsUsed,
    long DailyRestCompletionDeadline,
    long WeeklyRestStartDeadline);

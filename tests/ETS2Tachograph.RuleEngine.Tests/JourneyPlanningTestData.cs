using ETS2Tachograph.RuleEngine.JourneyPlanning;

namespace ETS2Tachograph.RuleEngine.Tests;

internal static class JourneyPlanningTestData
{
    internal static JourneyPlanningSnapshot Snapshot(
        long startGameMinute = 10_000,
        long weeklyDrivingMinutes = 0,
        long previousWeekDrivingMinutes = 0,
        long continuousDrivingMinutes = 0,
        long currentContinuousBreakMinutes = 0,
        long dailyDrivingMinutes = 0,
        long minutesUntilDailyRestDeadline = 24 * 60,
        long minutesUntilWeeklyRestDeadline = 6 * 24 * 60,
        int dailyExtensionsUsed = 0,
        int reducedDailyRestsUsed = 0,
        bool pendingRestAllocation = false,
        bool multiManningActive = false,
        int weekEpochOffsetDays = 0)
    {
        var state = new RegulationState
        {
            WeeklyDrivingMinutes = weeklyDrivingMinutes,
            PreviousWeekDrivingMinutes = previousWeekDrivingMinutes,
            ContinuousDrivingMinutes = continuousDrivingMinutes,
            CurrentContinuousBreakMinutes = currentContinuousBreakMinutes,
            DailyDrivingMinutes = dailyDrivingMinutes,
            MinutesUntilDailyRestDeadline = minutesUntilDailyRestDeadline,
            MinutesUntilWeeklyRestDeadline = minutesUntilWeeklyRestDeadline,
            DailyExtensionsUsedThisWeek = dailyExtensionsUsed,
            ReducedDailyRestsSinceWeeklyRest = reducedDailyRestsUsed,
            PendingRestAllocation = pendingRestAllocation,
            MinutesUntilBreak = 270
        };

        var evaluation = new RegulationEvaluation(state, [], []);
        return new JourneyPlanningSnapshot(
            DriverSlot: 1,
            StartGameMinute: startGameMinute,
            ActivitySessionId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            WorldGeneration: 7,
            HistoryHighWaterMark: 42,
            Evaluation: evaluation,
            History: [],
            Gaps: [],
            WeekEpochOffsetDays: weekEpochOffsetDays,
            MultiManningActive: multiManningActive,
            TelemetryAvailable: true);
    }

    internal static JourneyPlanRequest Request(
        JourneyPlanningSnapshot snapshot,
        int remainingDriveMinutes = 60,
        int deliveryWindowMinutes = 10_000,
        int operationalBufferMinutes = 0,
        JourneyPlanningLimits? limits = null) =>
        new(
            snapshot,
            remainingDriveMinutes,
            deliveryWindowMinutes,
            operationalBufferMinutes,
            JourneyOperationalBufferPolicy.OtherWorkAfterArrival,
            limits ?? JourneyPlanningLimits.Default);
}

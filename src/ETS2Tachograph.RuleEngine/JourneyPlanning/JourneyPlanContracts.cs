using ETS2Tachograph.Core.Enums;

namespace ETS2Tachograph.RuleEngine.JourneyPlanning;

public sealed record JourneyPlanRequest(
    JourneyPlanningSnapshot Snapshot,
    int RemainingDriveMinutes,
    int DeliveryWindowMinutes,
    int OperationalBufferMinutes,
    JourneyOperationalBufferPolicy BufferPolicy,
    JourneyPlanningLimits Limits);

public sealed record JourneyPlanningLimits(
    int MaximumSegments,
    int MaximumElapsedMinutes,
    int MaximumVisitedStates)
{
    public static JourneyPlanningLimits Default { get; } = new(
        MaximumSegments: 512,
        MaximumElapsedMinutes: 8 * 7 * 24 * 60,
        MaximumVisitedStates: 50_000);
}

public sealed record DailyRestPlanningWindow(
    long CompletionDeadlineGameMinute,
    long LatestRegularRestStartGameMinute,
    long? LatestReducedRestStartGameMinute);

public sealed record JourneyPlanSegment(
    JourneyPlanSegmentType Type,
    int DriverSlot,
    long StartGameMinute,
    long EndGameMinute,
    int DurationMinutes,
    JourneyPlanSegmentReason Reason,
    DriverActivity RegulatoryActivity,
    bool UsesRegulatoryException,
    string? WarningCode);

public sealed record JourneyPlanWarning(
    JourneyPlanWarningCode Code,
    JourneyPlanWarningSeverity Severity,
    string? Context = null);

public sealed record JourneyPlanUsageSummary(
    int DailyDrivingExtensionsUsed,
    int ReducedDailyRestsUsed,
    bool UsedReducedWeeklyRest,
    bool UsedRegularWeeklyRest,
    int RecognizedCompensationObligationMinutes,
    bool UsedExistingFifteenMinuteBreak,
    bool UsedThirtyHourWindow,
    bool UsedCalendarWait,
    bool ReachedWeeklyDrivingLimit,
    bool ReachedBiweeklyDrivingLimit)
{
    public static JourneyPlanUsageSummary Empty { get; } = new(
        DailyDrivingExtensionsUsed: 0,
        ReducedDailyRestsUsed: 0,
        UsedReducedWeeklyRest: false,
        UsedRegularWeeklyRest: false,
        RecognizedCompensationObligationMinutes: 0,
        UsedExistingFifteenMinuteBreak: false,
        UsedThirtyHourWindow: false,
        UsedCalendarWait: false,
        ReachedWeeklyDrivingLimit: false,
        ReachedBiweeklyDrivingLimit: false);
}

public sealed record JourneyPlanResult(
    JourneyPlanStatus Status,
    JourneyPlanConfidence Confidence,
    long StartGameMinute,
    long? EarliestArrivalGameMinute,
    long? EarliestCompletionGameMinute,
    int RequiredElapsedMinutes,
    int MarginMinutes,
    IReadOnlyList<JourneyPlanSegment> Segments,
    IReadOnlyList<JourneyPlanWarning> Warnings,
    JourneyPlanUsageSummary Usage,
    JourneyPlanSnapshotIdentity SnapshotIdentity);

using ETS2Tachograph.Core.Entities;
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

public sealed record CrewDriverPlanningSnapshot(
    int DriverSlot,
    string DriverCardId,
    Guid ActivitySessionId,
    long HistoryHighWaterMark,
    RegulationEvaluation Evaluation,
    IReadOnlyList<ActivityRecord> History,
    IReadOnlyList<ActivityGap> Gaps);

public sealed record CrewJourneyPlanningSnapshot(
    long StartGameMinute,
    long WorldGeneration,
    int WeekEpochOffsetDays,
    bool MultiManningActive,
    bool TelemetryAvailable,
    CrewDriverPlanningSnapshot Slot1,
    CrewDriverPlanningSnapshot Slot2);

public sealed record CrewJourneyPlanRequest(
    JourneyPlanningMode Mode,
    CrewJourneyPlanningSnapshot Snapshot,
    int InitialDrivingSlot,
    int RemainingDriveMinutes,
    int DeliveryWindowMinutes,
    int OperationalBufferMinutes,
    JourneyOperationalBufferPolicy BufferPolicy,
    JourneyPlanningLimits Limits);

public sealed record CrewJourneyPlanSegment(
    long StartGameMinute,
    long EndGameMinute,
    int? DrivingSlot,
    DriverActivity Slot1Activity,
    DriverActivity Slot2Activity,
    bool Slot1BreakQualifiedInMotion,
    bool Slot2BreakQualifiedInMotion,
    JourneyPlanSegmentReason Reason)
{
    public int DurationMinutes => checked((int)(EndGameMinute - StartGameMinute));
}

public sealed record CrewDriverPlanSummary(
    int DriverSlot,
    long ContinuousDrivingMinutes,
    long DailyDrivingMinutes,
    long WeeklyDrivingMinutes,
    long PreviousWeekDrivingMinutes,
    long CurrentContinuousBreakMinutes,
    long MinutesUntilDailyRestDeadline,
    long MinutesUntilWeeklyRestDeadline,
    int DailyDrivingExtensionsUsed,
    int ReducedDailyRestsUsed);

public sealed record CrewJourneyPlanResult(
    JourneyPlanStatus Status,
    JourneyPlanConfidence Confidence,
    long StartGameMinute,
    long? EarliestArrivalGameMinute,
    long? EarliestCompletionGameMinute,
    int RequiredElapsedMinutes,
    int MarginMinutes,
    IReadOnlyList<CrewJourneyPlanSegment> Segments,
    IReadOnlyList<JourneyPlanWarning> Warnings,
    CrewDriverPlanSummary Slot1,
    CrewDriverPlanSummary Slot2);

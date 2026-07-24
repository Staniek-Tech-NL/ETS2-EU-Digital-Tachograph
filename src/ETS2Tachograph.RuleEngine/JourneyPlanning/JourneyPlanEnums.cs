namespace ETS2Tachograph.RuleEngine.JourneyPlanning;

public enum JourneyOperationalBufferPolicy
{
    OtherWorkAfterArrival
}

public enum JourneyPlanStatus
{
    MeetsDeadline,
    MissesDeadline,
    BlockedByGap,
    InsufficientData,
    StaleSnapshot,
    UnsupportedScenario,
    NoLegalContinuation,
    CalculationLimitReached
}

public enum JourneyPlanConfidence
{
    VerifiedByCurrentRuleModel,
    LimitedByCompensationModel,
    BasedOnIncompleteHistory,
    BasedOnLastSavedState
}

public enum JourneyPlanSegmentType
{
    Drive,
    Break,
    DailyRest,
    WeeklyRest,
    CalendarWait,
    OtherWork,
    Availability
}

public enum JourneyPlanSegmentReason
{
    RemainingRouteDrive,
    ContinuousDrivingBreak,
    SplitBreakCompletion,
    DailyRestDeadline,
    DailyDrivingLimit,
    WeeklyRestRequirement,
    WeeklyDrivingLimitReached,
    BiweeklyDrivingLimitReached,
    WaitForNewRegulatoryWeek,
    WaitForBiweeklyCapacity,
    OperationalBufferAfterArrival
}

public enum JourneyPlanWarningCode
{
    IncompleteHistory,
    LastSavedState,
    CompensationModelLimited,
    ReducedWeeklyRestUnavailable,
    MultiManningPlanningUnsupported,
    RegulatoryExceptionUsed
}

public enum JourneyPlanWarningSeverity
{
    Information,
    Caution,
    Limitation
}

public enum JourneyPlanSnapshotMismatch
{
    None,
    DriverSlotChanged,
    GameTimeMovedBackward,
    ActivitySessionChanged,
    WorldGenerationChanged,
    HistoryChanged,
    StartGameMinuteChanged
}

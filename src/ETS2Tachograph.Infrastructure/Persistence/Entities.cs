using ETS2Tachograph.Core.Enums;

namespace ETS2Tachograph.Infrastructure.Persistence;

public sealed class DriverProfileEntity
{
    public Guid Id { get; set; }
    public required string DisplayName { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public List<DriverCardEntity> Cards { get; set; } = [];
}

public sealed class DriverCardEntity
{
    public required string Id { get; set; }
    public Guid DriverProfileId { get; set; }
    public DriverProfileEntity DriverProfile { get; set; } = null!;
    public required string CountryCode { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly ValidUntil { get; set; }
    public List<ActivitySessionEntity> Sessions { get; set; } = [];
}

public sealed class ActivitySessionEntity
{
    public Guid Id { get; set; }
    public required string DriverCardId { get; set; }
    public DriverCardEntity DriverCard { get; set; } = null!;
    public int SessionIndex { get; set; }
    public long StartedAtGameMinute { get; set; }
    public long? EndedAtGameMinute { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public List<ActivityRecordEntity> Records { get; set; } = [];
    public List<ActivityGapEntity> Gaps { get; set; } = [];
}

public sealed class ActivityRecordEntity
{
    public Guid Id { get; set; }
    public Guid ActivitySessionId { get; set; }
    public ActivitySessionEntity ActivitySession { get; set; } = null!;
    public DriverActivity Activity { get; set; }
    public long StartGameMinute { get; set; }
    public long EndGameMinuteExclusive { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
    public ActivitySource Source { get; set; }
    public SpecialCondition Condition { get; set; }
    public Guid? SourceGapId { get; set; }
    public ActivityGapEntity? SourceGap { get; set; }
    public bool IsArchivedToWarm { get; set; }
}

public sealed class ActivityGapEntity
{
    public Guid Id { get; set; }
    public required string DriverCardId { get; set; }
    public Guid ActivitySessionId { get; set; }
    public ActivitySessionEntity ActivitySession { get; set; } = null!;
    public int Slot { get; set; }
    public long StartGameMinute { get; set; }
    public long? EndGameMinuteExclusive { get; set; }
    public ActivityGapReason Reason { get; set; }
    public ActivityGapState State { get; set; }
    public long? ResolvedAtGameMinute { get; set; }
    public Guid? ProjectionSourceGapId { get; set; }
    public ActivityGapEntity? ProjectionSourceGap { get; set; }
    public List<ActivityRecordEntity> ManualEntryRecords { get; set; } = [];
}

public sealed class WarmActivityBlockEntity
{
    public Guid Id { get; set; }
    public required string DriverCardId { get; set; }
    public DriverCardEntity DriverCard { get; set; } = null!;
    public long StartGameMinute { get; set; }
    public long EndGameMinuteExclusive { get; set; }
    public long DurationMinutes { get; set; }
    public DriverActivity Activity { get; set; }
    public ActivitySource Source { get; set; }
    public SpecialCondition Condition { get; set; }
    public Guid? SourceGapId { get; set; }
}

public sealed class ActivityRetentionStateEntity
{
    public required string DriverCardId { get; set; }
    public DriverCardEntity DriverCard { get; set; } = null!;
    public long HighWaterMarkGameMinute { get; set; }
}

public sealed class RegulationSnapshotEntity
{
    public Guid Id { get; set; }
    public required string DriverCardId { get; set; }
    public long GameMinute { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
    public long ContinuousDrivingMinutes { get; set; }
    public long DailyDrivingMinutes { get; set; }
    public long WeeklyDrivingMinutes { get; set; }
    public long FortnightlyDrivingMinutes { get; set; }
    public long MinutesUntilBreak { get; set; }
    public long MinutesUntilDailyRestDeadline { get; set; }
    public required string ViolationsJson { get; set; }
}

public sealed class FerryRestRecordEntity
{
    public Guid Id { get; set; }
    public required string DriverCardId { get; set; }
    public long StartGameMinute { get; set; }
    public long EndGameMinuteExclusive { get; set; }
    public int InterruptionCount { get; set; }
    public long InterruptionMinutes { get; set; }
    public bool Accepted { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
}

public sealed class TachographSettingsEntity
{
    public int Id { get; set; } = 1;
    public double DrivingSpeedThresholdKph { get; set; } = 1;
    public int WeekEpochOffsetDays { get; set; }
}

public sealed class RestAllocationDecisionEntity
{
    public Guid DecisionId { get; set; }
    public required string DriverCardId { get; set; }
    public DriverCardEntity DriverCard { get; set; } = null!;
    public required string RestBlockId { get; set; }
    public required string CandidateId { get; set; }
    public long EffectiveAtGameMinute { get; set; }
    public DateTimeOffset DecidedAtUtc { get; set; }
    public int DecisionSchemeVersion { get; set; }
    public int Status { get; set; }
    public Guid? SupersedesDecisionId { get; set; }
}

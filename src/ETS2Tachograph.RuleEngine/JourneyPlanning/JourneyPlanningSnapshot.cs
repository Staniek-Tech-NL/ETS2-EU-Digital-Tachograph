using ETS2Tachograph.Core.Entities;

namespace ETS2Tachograph.RuleEngine.JourneyPlanning;

public sealed record JourneyPlanningSnapshot(
    int DriverSlot,
    long StartGameMinute,
    Guid ActivitySessionId,
    long WorldGeneration,
    long HistoryHighWaterMark,
    RegulationEvaluation Evaluation,
    IReadOnlyList<ActivityRecord> History,
    IReadOnlyList<ActivityGap> Gaps,
    bool MultiManningActive,
    bool TelemetryAvailable)
{
    public JourneyPlanSnapshotIdentity Identity => new(
        DriverSlot,
        StartGameMinute,
        ActivitySessionId,
        WorldGeneration,
        HistoryHighWaterMark);
}

public sealed record JourneyPlanSnapshotIdentity(
    int DriverSlot,
    long StartGameMinute,
    Guid ActivitySessionId,
    long WorldGeneration,
    long HistoryHighWaterMark)
{
    public JourneyPlanSnapshotMismatch CompareTo(JourneyPlanningSnapshot current)
    {
        ArgumentNullException.ThrowIfNull(current);

        if (DriverSlot != current.DriverSlot)
        {
            return JourneyPlanSnapshotMismatch.DriverSlotChanged;
        }

        if (current.StartGameMinute < StartGameMinute)
        {
            return JourneyPlanSnapshotMismatch.GameTimeMovedBackward;
        }

        if (ActivitySessionId != current.ActivitySessionId)
        {
            return JourneyPlanSnapshotMismatch.ActivitySessionChanged;
        }

        if (WorldGeneration != current.WorldGeneration)
        {
            return JourneyPlanSnapshotMismatch.WorldGenerationChanged;
        }

        if (HistoryHighWaterMark != current.HistoryHighWaterMark)
        {
            return JourneyPlanSnapshotMismatch.HistoryChanged;
        }

        return StartGameMinute == current.StartGameMinute
            ? JourneyPlanSnapshotMismatch.None
            : JourneyPlanSnapshotMismatch.StartGameMinuteChanged;
    }

    public bool IsCurrentFor(JourneyPlanningSnapshot current) =>
        CompareTo(current) == JourneyPlanSnapshotMismatch.None;
}

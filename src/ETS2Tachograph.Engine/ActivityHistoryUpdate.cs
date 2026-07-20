using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;

namespace ETS2Tachograph.Engine;

public sealed record ActivityHistoryUpdate(
    IReadOnlyList<ActivityCompletionBatch> CompletedBatches,
    IReadOnlyList<ActivitySessionStart> OpenedSessions,
    bool ClockMovedBackward,
    bool GameTimeJumpDetected,
    bool WorldGenerationChanged,
    int SessionIndex,
    DriverActivity? ProvisionalActivity)
{
    public IReadOnlyList<ActivityRecord> CompletedRecords =>
        CompletedBatches.SelectMany(batch => batch.Records).ToList();

    public IReadOnlyList<ActivityGapBatch> CreatedGapBatches { get; init; } = [];
    public IReadOnlyList<ActivityGap> CreatedGaps =>
        CreatedGapBatches.SelectMany(batch => batch.Gaps).ToList();
}

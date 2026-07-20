using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Telemetry;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.RuleEngine;

namespace ETS2Tachograph.Engine;

/// <summary>Immutable, complete public view of the tachograph after a command or frame.</summary>
public sealed record TachographSnapshot
{
    public TelemetryFrame? Frame { get; init; }
    public GameTime? GameTime => Frame?.GameTime;
    public bool GamePaused => Frame?.GamePaused ?? false;
    public DriverActivity ManualActivity { get; init; } = DriverActivity.OtherWork;
    public DriverActivity? ProvisionalActivity { get; init; }
    public ActivityRecord? LastClosedRecord { get; init; }
    public IReadOnlyList<ActivityCompletionBatch> CompletedBatches { get; init; } = [];
    public IReadOnlyList<ActivityGapBatch> CreatedGapBatches { get; init; } = [];
    public IReadOnlyList<ActivitySessionStart> OpenedSessions { get; init; } = [];
    public IReadOnlyList<ActivityRecord> CompletedRecords { get; init; } = [];
    public IReadOnlyList<ActivityGap> CreatedGaps { get; init; } = [];
    public IReadOnlyList<ActivityRecord> CurrentSessionRecords { get; init; } = [];
    public IReadOnlyList<ActivityGap> CurrentSessionGaps { get; init; } = [];
    public ActivityGap? RequiredManualEntryGap { get; init; }
    public ActivityGap? OptionalManualEntryGap { get; init; }
    public bool ManualEntryRequired => RequiredManualEntryGap is not null;
    public RegulationEvaluation? Regulation { get; init; }
    public bool OutModeEnabled { get; init; }
    public bool FerryModeEnabled { get; init; }
    public bool MultiManningEnabled { get; init; }
    public int DailyRestWindowMinutes => MultiManningEnabled ? 1_800 : 1_440;
    public bool ClockMovedBackward { get; init; }
    public bool GameTimeJumpDetected { get; init; }
    public bool WorldGenerationChanged { get; init; }
    public int SessionIndex { get; init; }
}

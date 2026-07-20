using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Engine;

/// <summary>
/// Closed activity records together with the session that owned them when
/// they were completed. The current session may already be a newer branch.
/// </summary>
public sealed record ActivityCompletionBatch(
    int SessionIndex,
    GameTime SessionStartedAt,
    IReadOnlyList<ActivityRecord> Records);

/// <summary>Audit-gap changes together with the clock branch that owns them.</summary>
public sealed record ActivityGapBatch(
    int SessionIndex,
    GameTime SessionStartedAt,
    IReadOnlyList<ActivityGap> Gaps,
    IReadOnlyList<Guid>? RemovedGapIds = null);

/// <summary>A newly opened clock branch that must be persisted as a boundary.</summary>
public sealed record ActivitySessionStart(
    int SessionIndex,
    GameTime StartedAt);

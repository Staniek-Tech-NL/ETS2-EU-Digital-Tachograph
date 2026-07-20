using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Engine;

/// <summary>
/// Preserves the branch anchor even when every record in a restored session has
/// already moved to the warm retention tier.
/// </summary>
public sealed record RestoredActivitySession(
    int SessionIndex,
    GameTime? StartedAt,
    IReadOnlyList<ActivityRecord> Records,
    IReadOnlyList<ActivityGap>? Gaps = null);

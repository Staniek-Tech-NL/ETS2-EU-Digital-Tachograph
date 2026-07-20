using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Core.Entities;

/// <summary>
/// A game-time interval for which no trustworthy driver activity is available.
/// Gaps are audit data and are never projected as a driver activity.
/// </summary>
public sealed record ActivityGap
{
    public required Guid Id { get; init; }
    public required string DriverCardId { get; init; }
    public required int Slot { get; init; }
    public required int SessionIndex { get; init; }
    public required GameTime Start { get; init; }
    public GameTime? EndExclusive { get; init; }
    public required ActivityGapReason Reason { get; init; }
    public ActivityGapState State { get; init; } = ActivityGapState.Unresolved;
    public GameTime? ResolvedAt { get; init; }

    /// <summary>
    /// Identifies the source gap when this row materializes a clipped interval
    /// from the canonical clock branch. The source row remains untouched as an
    /// audit trail of the abandoned branch.
    /// </summary>
    public Guid? ProjectionSourceGapId { get; init; }

    public bool IsOpen => EndExclusive is null;
    public long? DurationMinutes => EndExclusive is null ? null : EndExclusive.Value - Start;
}

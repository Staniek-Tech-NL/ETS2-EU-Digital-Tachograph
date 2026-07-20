using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Core.Entities;

/// <summary>An immutable segment in the append-only activity history.</summary>
public sealed record ActivityRecord
{
    public required Guid Id { get; init; }
    public required string DriverCardId { get; init; }
    public required DriverActivity Activity { get; init; }
    public required GameTime Start { get; init; }
    public required GameTime EndExclusive { get; init; }
    public required DateTimeOffset RecordedAtUtc { get; init; }
    public ActivitySource Source { get; init; } = ActivitySource.Telemetry;
    public SpecialCondition Condition { get; init; } = SpecialCondition.None;
    public Guid? SourceGapId { get; init; }

    public long DurationMinutes => EndExclusive - Start;
    public string StartGameTimeText => GameClockFormatter.Format(Start);
    public string EndGameTimeText => GameClockFormatter.Format(EndExclusive);
}

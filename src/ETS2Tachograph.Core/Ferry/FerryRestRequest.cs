namespace ETS2Tachograph.Core.Ferry;

/// <summary>Player-declared ferry conditions which ETS2 telemetry cannot provide.</summary>
public sealed record FerryRestRequest
{
    public required FerryRestType RestType { get; init; }
    public required TimeSpan RestExcludingInterruptions { get; init; }
    public IReadOnlyList<TimeSpan> Interruptions { get; init; } = [];
    public bool HasSleepingFacility { get; init; }
    public TimeSpan ScheduledCrossingDuration { get; init; }
}

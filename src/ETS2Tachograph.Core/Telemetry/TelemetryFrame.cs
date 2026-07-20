using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Core.Telemetry;

public sealed record TelemetryFrame(
    GameTime GameTime,
    DateTimeOffset RecordedAtUtc,
    double SpeedKph,
    bool GamePaused,
    uint WorldGeneration = 0,
    uint CargoOperationGeneration = 0);

namespace ETS2Tachograph.Telemetry.Scs;

public readonly record struct ScsTelemetrySnapshot(
    uint Sequence,
    bool Running,
    uint GameTimeMinutes,
    float SpeedMetersPerSecond,
    uint WorldGeneration,
    uint CargoOperationGeneration = 0);

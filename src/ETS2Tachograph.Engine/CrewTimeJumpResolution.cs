using ETS2Tachograph.Core.Enums;

namespace ETS2Tachograph.Engine;

public sealed record CrewTimeJumpResolution(
    long StartGameMinute,
    long EndGameMinuteExclusive,
    bool VehicleStationaryBeforeAndAfter,
    bool ExplainedByCrewRest,
    IReadOnlyDictionary<int, DriverActivity?> ReconstructedActivities);

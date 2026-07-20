using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.RuleEngine;

public sealed record RuleViolation(
    ViolationType Type,
    string Article,
    string Message,
    GameTime DetectedAt,
    long ExcessMinutes = 0);

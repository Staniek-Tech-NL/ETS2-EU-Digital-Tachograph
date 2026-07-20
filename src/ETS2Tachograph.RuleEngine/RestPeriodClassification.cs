using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.RuleEngine;

public enum DailyRestClassification
{
    Reduced = 0,
    Regular = 1
}

public enum WeeklyRestClassification
{
    Reduced = 0,
    Regular = 1
}

/// <summary>
/// A rest block which actually qualifies as a daily rest. Classifications are
/// derived exclusively from its measured or manually declared duration.
/// </summary>
public sealed record QualifiedRestPeriod(
    GameTime Start,
    GameTime EndExclusive,
    Guid? SourceGapId,
    DailyRestClassification DailyClassification,
    WeeklyRestClassification? WeeklyClassification)
{
    public long DurationMinutes => EndExclusive - Start;
}

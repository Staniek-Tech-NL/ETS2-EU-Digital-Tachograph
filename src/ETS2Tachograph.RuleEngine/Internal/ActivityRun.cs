using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.RuleEngine.Internal;

internal sealed record ActivityRun(
    string DriverCardId,
    GameTime Start,
    GameTime EndExclusive,
    DriverActivity Activity,
    SpecialCondition Condition,
    Guid? SourceGapId,
    IReadOnlyList<ActivitySourceRange> SourceRanges)
{
    public long DurationMinutes => EndExclusive - Start;
}

internal sealed record ActivitySourceRange(
    GameTime Start,
    GameTime EndExclusive,
    Guid? SourceGapId);

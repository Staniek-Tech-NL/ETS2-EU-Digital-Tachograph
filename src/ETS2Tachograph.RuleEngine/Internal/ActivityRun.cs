using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.RuleEngine.Internal;

internal sealed record ActivityRun(
    GameTime Start,
    GameTime EndExclusive,
    DriverActivity Activity,
    Guid? SourceGapId)
{
    public long DurationMinutes => EndExclusive - Start;
}

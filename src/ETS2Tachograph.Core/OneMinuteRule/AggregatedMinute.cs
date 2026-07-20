using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Core.OneMinuteRule;

public sealed record AggregatedMinute(
    GameTime Minute,
    DriverActivity Activity,
    bool DrivingPrecedenceApplied,
    ActivitySource Source,
    SpecialCondition Condition);

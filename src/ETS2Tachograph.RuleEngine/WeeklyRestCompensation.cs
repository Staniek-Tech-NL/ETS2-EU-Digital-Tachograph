using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.RuleEngine;

public sealed record WeeklyRestCompensation(
    long OwedMinutes,
    GameWeek ReductionWeek,
    GameWeek DueByEndOfWeek,
    bool IsOverdue);

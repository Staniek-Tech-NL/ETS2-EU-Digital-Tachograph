using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.RuleEngine;

/// <summary>
/// Stable public projection of all outstanding weekly-rest compensation obligations.
/// Consumers do not need to repeat aggregation rules in each UI or report.
/// </summary>
public sealed record CompensationSummary(
    long TotalOwedMinutes,
    GameWeek? NearestDueByEndOfWeek,
    int Count,
    bool HasOverdue)
{
    public static CompensationSummary Empty { get; } = new(0, null, 0, false);

    public static CompensationSummary From(IReadOnlyList<WeeklyRestCompensation> compensations)
    {
        ArgumentNullException.ThrowIfNull(compensations);
        if (compensations.Count == 0)
            return Empty;

        return new CompensationSummary(
            compensations.Sum(item => item.OwedMinutes),
            compensations.MinBy(item => item.DueByEndOfWeek.Index)!.DueByEndOfWeek,
            compensations.Count,
            compensations.Any(item => item.IsOverdue));
    }
}

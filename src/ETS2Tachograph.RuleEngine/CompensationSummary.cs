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
        var open = compensations.Where(item => item.IsOpen).ToList();
        if (open.Count == 0)
            return compensations.Any(item => item.IsOverdue)
                ? Empty with { HasOverdue = true }
                : Empty;

        return new CompensationSummary(
            open.Sum(item => item.RemainingMinutes),
            open.MinBy(item => item.DueAtExclusive)!.DueByEndOfWeek,
            open.Count,
            compensations.Any(item => item.IsOverdue));
    }
}

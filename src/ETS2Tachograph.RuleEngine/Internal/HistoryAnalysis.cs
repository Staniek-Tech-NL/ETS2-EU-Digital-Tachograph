using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.RuleEngine.Internal;

internal static class HistoryAnalysis
{
    public static IReadOnlyList<ActivityRun> Runs(IReadOnlyList<ActivityRecord> history, GameTime now)
    {
        var ordered = history
            .Where(record => record.Start < now && record.EndExclusive > record.Start)
            .OrderBy(record => record.Start)
            .ToList();
        var result = new List<ActivityRun>();

        foreach (var record in ordered)
        {
            var end = record.EndExclusive > now ? now : record.EndExclusive;
            if (result.Count > 0 &&
                result[^1].DriverCardId == record.DriverCardId &&
                result[^1].Activity == record.Activity &&
                result[^1].EndExclusive == record.Start &&
                CanJoinAcrossManualEntryBoundary(result[^1], record))
            {
                result[^1] = result[^1] with
                {
                    EndExclusive = end,
                    SourceGapId = result[^1].SourceGapId ?? record.SourceGapId,
                    SourceRanges = AppendSourceRange(
                        result[^1].SourceRanges,
                        new ActivitySourceRange(record.Start, end, record.SourceGapId))
                };
            }
            else
            {
                result.Add(new ActivityRun(
                    record.DriverCardId,
                    record.Start,
                    end,
                    record.Activity,
                    record.SourceGapId,
                    [new ActivitySourceRange(record.Start, end, record.SourceGapId)]));
            }
        }

        return result;
    }

    private static bool CanJoinAcrossManualEntryBoundary(
        ActivityRun previous,
        ActivityRecord current)
    {
        if (previous.SourceGapId == current.SourceGapId)
            return true;

        // Removing a card stops automatic recording, not the driver's actual
        // rest. Once the missing interval is resolved as rest, contiguous rest
        // on either side remains one regulatory block. Keep the gap id on the
        // merged run so the audit trail still identifies the manual entry.
        return previous.Activity == DriverActivity.BreakOrRest &&
               (previous.SourceGapId is null || current.SourceGapId is null);
    }

    private static IReadOnlyList<ActivitySourceRange> AppendSourceRange(
        IReadOnlyList<ActivitySourceRange> existing,
        ActivitySourceRange incoming)
    {
        if (existing.Count == 0)
            return [incoming];

        var result = existing.ToList();
        var previous = result[^1];
        if (previous.EndExclusive == incoming.Start &&
            previous.SourceGapId == incoming.SourceGapId)
        {
            result[^1] = previous with { EndExclusive = incoming.EndExclusive };
        }
        else
        {
            result.Add(incoming);
        }

        return result;
    }

    public static long DrivingOverlap(
        IReadOnlyList<ActivityRecord> history,
        long startMinute,
        long endMinute) => history
        .Where(record => record.Activity == DriverActivity.Driving)
        .Sum(record => Math.Max(
            0,
            Math.Min(record.EndExclusive.TotalMinutes, endMinute) -
            Math.Max(record.Start.TotalMinutes, startMinute)));

    public static (long Start, long End) WeekBounds(GameWeek week, int offsetDays)
    {
        var start = checked((week.Index * GameWeek.MinutesPerWeek) -
            ((long)offsetDays * GameWeek.MinutesPerDay));
        return (start, start + GameWeek.MinutesPerWeek);
    }
}

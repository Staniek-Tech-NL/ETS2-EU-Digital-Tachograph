using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Core.OneMinuteRule;

/// <summary>The requirement-052 candidate for one calendar minute.</summary>
public sealed record MinuteActivity
{
    private MinuteActivity(
        GameTime minute,
        DriverActivity longestContinuousActivity,
        ActivitySource source,
        SpecialCondition condition)
    {
        Minute = minute;
        LongestContinuousActivity = longestContinuousActivity;
        Source = source;
        Condition = condition;
    }

    public GameTime Minute { get; }
    public DriverActivity LongestContinuousActivity { get; }
    public ActivitySource Source { get; }
    public SpecialCondition Condition { get; }

    public static MinuteActivity FromSlices(
        GameTime minute,
        IEnumerable<ActivitySlice> slices,
        ActivitySource source = ActivitySource.Telemetry,
        SpecialCondition condition = SpecialCondition.None)
    {
        ArgumentNullException.ThrowIfNull(slices);
        var sliceList = slices.ToList();
        if (sliceList.Count == 0)
        {
            throw new ArgumentException("At least one activity slice is required.", nameof(slices));
        }

        // Adjacent slices of the same type form one continuous activity.
        var runs = new List<(DriverActivity Activity, long Ticks, int LastIndex)>();
        for (var index = 0; index < sliceList.Count; index++)
        {
            var slice = sliceList[index];
            if (runs.Count > 0 && runs[^1].Activity == slice.Activity)
            {
                var previous = runs[^1];
                runs[^1] = (previous.Activity, previous.Ticks + slice.Duration.Ticks, index);
            }
            else
            {
                runs.Add((slice.Activity, slice.Duration.Ticks, index));
            }
        }

        // Requirement 052: longest continuous activity; latest wins equal duration.
        var winner = runs.OrderBy(run => run.Ticks).ThenBy(run => run.LastIndex).Last();
        return new MinuteActivity(minute, winner.Activity, source, condition);
    }
}

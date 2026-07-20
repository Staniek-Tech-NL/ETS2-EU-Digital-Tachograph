using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.OneMinuteRule;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Core.Tests.OneMinuteRule;

public sealed class MinuteActivityTests
{
    [Fact]
    public void Longest_continuous_activity_wins()
    {
        var minute = MinuteActivity.FromSlices(new GameTime(5),
        [
            new(DriverActivity.Driving, TimeSpan.FromSeconds(15)),
            new(DriverActivity.OtherWork, TimeSpan.FromSeconds(30)),
            new(DriverActivity.Driving, TimeSpan.FromSeconds(15))
        ]);

        Assert.Equal(DriverActivity.OtherWork, minute.LongestContinuousActivity);
    }

    [Fact]
    public void Latest_activity_wins_equal_duration()
    {
        var minute = MinuteActivity.FromSlices(new GameTime(5),
        [
            new(DriverActivity.OtherWork, TimeSpan.FromSeconds(30)),
            new(DriverActivity.BreakOrRest, TimeSpan.FromSeconds(30))
        ]);

        Assert.Equal(DriverActivity.BreakOrRest, minute.LongestContinuousActivity);
    }

    [Fact]
    public void Adjacent_equal_slices_are_one_continuous_run()
    {
        var minute = MinuteActivity.FromSlices(new GameTime(5),
        [
            new(DriverActivity.Driving, TimeSpan.FromSeconds(20)),
            new(DriverActivity.Driving, TimeSpan.FromSeconds(20)),
            new(DriverActivity.OtherWork, TimeSpan.FromSeconds(20))
        ]);

        Assert.Equal(DriverActivity.Driving, minute.LongestContinuousActivity);
    }
}

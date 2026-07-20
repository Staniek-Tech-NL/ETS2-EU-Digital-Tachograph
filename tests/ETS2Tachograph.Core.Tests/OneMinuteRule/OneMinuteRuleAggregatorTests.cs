using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.OneMinuteRule;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Core.Tests.OneMinuteRule;

public sealed class OneMinuteRuleAggregatorTests
{
    [Fact]
    public void Minute_between_two_driving_minutes_becomes_driving()
    {
        var aggregator = new OneMinuteRuleAggregator();

        Assert.Null(aggregator.Push(Minute(10, DriverActivity.Driving)));
        var first = aggregator.Push(Minute(11, DriverActivity.BreakOrRest));
        var middle = aggregator.Push(Minute(12, DriverActivity.Driving));

        Assert.Equal(DriverActivity.Driving, first!.Activity);
        Assert.Equal(DriverActivity.Driving, middle!.Activity);
        Assert.True(middle.DrivingPrecedenceApplied);
    }

    [Fact]
    public void First_and_flushed_minutes_use_requirement_052()
    {
        var aggregator = new OneMinuteRuleAggregator();

        aggregator.Push(Minute(1, DriverActivity.OtherWork));
        var first = aggregator.Push(Minute(2, DriverActivity.BreakOrRest));
        var last = aggregator.Flush();

        Assert.Equal(DriverActivity.OtherWork, first!.Activity);
        Assert.Equal(DriverActivity.BreakOrRest, last!.Activity);
        Assert.Null(aggregator.ProvisionalActivity);
    }

    [Fact]
    public void Non_consecutive_minutes_require_explicit_reset()
    {
        var aggregator = new OneMinuteRuleAggregator();
        aggregator.Push(Minute(100, DriverActivity.Driving));

        Assert.Throws<InvalidOperationException>(() =>
            aggregator.Push(Minute(50, DriverActivity.Driving)));

        aggregator.Reset();
        Assert.Null(aggregator.Push(Minute(50, DriverActivity.Driving)));
    }

    private static MinuteActivity Minute(long minute, DriverActivity activity) =>
        MinuteActivity.FromSlices(
            new GameTime(minute),
            [new ActivitySlice(activity, TimeSpan.FromMinutes(1))]);
}

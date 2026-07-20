using ETS2Tachograph.Core.Ferry;

namespace ETS2Tachograph.Core.Tests.Ferry;

public sealed class FerryRestDerogationTests
{
    [Fact]
    public void Two_interruptions_totalling_60_minutes_are_valid()
    {
        var result = FerryRestDerogation.Evaluate(Request(
            FerryRestType.RegularDaily,
            TimeSpan.FromHours(11),
            [TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30)]));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Interruptions_over_60_minutes_are_invalid()
    {
        var result = FerryRestDerogation.Evaluate(Request(
            FerryRestType.RegularDaily,
            TimeSpan.FromHours(11),
            [TimeSpan.FromMinutes(31), TimeSpan.FromMinutes(30)]));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void More_than_two_interruptions_are_invalid()
    {
        var result = FerryRestDerogation.Evaluate(Request(
            FerryRestType.RegularDaily,
            TimeSpan.FromHours(11),
            [TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10)]));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(FerryRestType.ReducedDaily)]
    [InlineData(FerryRestType.SplitDaily)]
    public void Unsupported_daily_rest_types_are_invalid(FerryRestType type)
    {
        Assert.False(FerryRestDerogation.Evaluate(Request(type, TimeSpan.FromHours(11), [])).IsValid);
    }

    [Fact]
    public void Weekly_regular_rest_requires_eight_hour_crossing()
    {
        var request = Request(FerryRestType.RegularWeekly, TimeSpan.FromHours(45), []);
        request = request with { ScheduledCrossingDuration = TimeSpan.FromHours(7.99) };

        Assert.False(FerryRestDerogation.Evaluate(request).IsValid);
    }

    [Fact]
    public void Rest_duration_excludes_interruptions()
    {
        var result = FerryRestDerogation.Evaluate(Request(
            FerryRestType.ReducedWeekly,
            TimeSpan.FromHours(23.5),
            [TimeSpan.FromMinutes(30)]));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Sleeping_facility_is_required()
    {
        var request = Request(FerryRestType.RegularDaily, TimeSpan.FromHours(11), []) with
        {
            HasSleepingFacility = false
        };

        Assert.False(FerryRestDerogation.Evaluate(request).IsValid);
    }

    private static FerryRestRequest Request(
        FerryRestType type,
        TimeSpan rest,
        IReadOnlyList<TimeSpan> interruptions) =>
        new()
        {
            RestType = type,
            RestExcludingInterruptions = rest,
            Interruptions = interruptions,
            HasSleepingFacility = true,
            ScheduledCrossingDuration = TimeSpan.FromHours(8)
        };
}

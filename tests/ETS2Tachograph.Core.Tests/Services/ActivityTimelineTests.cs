using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Services;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Core.Tests.Services;

public sealed class ActivityTimelineTests
{
    [Fact]
    public void Consecutive_immutable_records_are_appended()
    {
        var timeline = new ActivityTimeline();
        timeline.Append(Record(0, 10));
        timeline.Append(Record(10, 20));

        Assert.Collection(
            timeline.Records,
            first => Assert.Equal(10, first.DurationMinutes),
            second => Assert.Equal(10, second.DurationMinutes));
    }

    [Fact]
    public void Overlapping_record_is_rejected()
    {
        var timeline = new ActivityTimeline();
        timeline.Append(Record(10, 20));

        Assert.Throws<InvalidOperationException>(() => timeline.Append(Record(19, 30)));
    }

    [Fact]
    public void Zero_duration_record_is_rejected()
    {
        var timeline = new ActivityTimeline();
        Assert.Throws<ArgumentException>(() => timeline.Append(Record(5, 5)));
    }

    private static ActivityRecord Record(long start, long end) => new()
    {
        Id = Guid.NewGuid(),
        DriverCardId = "PL-TEST",
        Activity = DriverActivity.Driving,
        Start = new GameTime(start),
        EndExclusive = new GameTime(end),
        RecordedAtUtc = DateTimeOffset.UtcNow
    };
}

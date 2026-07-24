using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.RuleEngine.JourneyPlanning;

namespace ETS2Tachograph.RuleEngine.Tests;

public sealed class JourneyPlanningP0RedTests
{
    private readonly JourneyPlanningEngine _engine = new();

    [Fact(DisplayName = "JP-P0-01: 56 h forces CalendarWait until a new regulatory week")]
    [Trait("Stage", "M2Red")]
    public void JP_P0_01()
    {
        var request = JourneyPlanningTestData.Request(
            JourneyPlanningTestData.Snapshot(weeklyDrivingMinutes: 56 * 60));

        var result = _engine.Plan(request);

        Assert.Contains(result.Segments, segment =>
            segment.Type == JourneyPlanSegmentType.CalendarWait &&
            segment.Reason == JourneyPlanSegmentReason.WaitForNewRegulatoryWeek);
    }

    [Fact(DisplayName = "JP-P0-02: a 24 h weekly rest does not itself reset the 56 h limit")]
    [Trait("Stage", "M2Red")]
    public void JP_P0_02()
    {
        var request = JourneyPlanningTestData.Request(
            JourneyPlanningTestData.Snapshot(
                startGameMinute: 3 * 24 * 60,
                weeklyDrivingMinutes: 56 * 60));

        var result = _engine.Plan(request);

        var weeklyRest = Assert.Single(result.Segments, segment =>
            segment.Type == JourneyPlanSegmentType.WeeklyRest &&
            segment.DurationMinutes == 24 * 60);
        var firstDriveAfterRest = Assert.Single(result.Segments, segment =>
            segment.Type == JourneyPlanSegmentType.Drive &&
            segment.StartGameMinute >= weeklyRest.EndGameMinute);
        var calendarWait = Assert.Single(result.Segments, segment =>
            segment.Type == JourneyPlanSegmentType.CalendarWait);
        Assert.True(firstDriveAfterRest.StartGameMinute >= calendarWait.EndGameMinute);
    }

    [Fact(DisplayName = "JP-P0-03: 90 h forces wait until fortnight capacity is actually released")]
    [Trait("Stage", "M2Red")]
    public void JP_P0_03()
    {
        var request = JourneyPlanningTestData.Request(
            JourneyPlanningTestData.Snapshot(
                weeklyDrivingMinutes: 40 * 60,
                previousWeekDrivingMinutes: 50 * 60));

        var result = _engine.Plan(request);

        Assert.Contains(result.Segments, segment =>
            segment.Type == JourneyPlanSegmentType.CalendarWait &&
            segment.Reason == JourneyPlanSegmentReason.WaitForBiweeklyCapacity);
        Assert.True(result.Usage.ReachedBiweeklyDrivingLimit);
    }

    [Fact(DisplayName = "JP-P0-04: rest overlapping CalendarWait is not counted twice")]
    [Trait("Stage", "M2Red")]
    public void JP_P0_04()
    {
        var request = JourneyPlanningTestData.Request(
            JourneyPlanningTestData.Snapshot(weeklyDrivingMinutes: 56 * 60));

        var result = _engine.Plan(request);

        Assert.All(
            result.Segments.Zip(result.Segments.Skip(1)),
            pair => Assert.True(pair.First.EndGameMinute <= pair.Second.StartGameMinute));
        Assert.Equal(
            result.EarliestCompletionGameMinute - result.StartGameMinute,
            result.RequiredElapsedMinutes);
    }

    [Fact(DisplayName = "JP-P0-05: daily rest must finish within the 24 h window")]
    [Trait("Stage", "M2Red")]
    public void JP_P0_05()
    {
        var snapshot = JourneyPlanningTestData.Snapshot(
            startGameMinute: 10_000,
            minutesUntilDailyRestDeadline: 10 * 60,
            reducedDailyRestsUsed: 3);
        var result = _engine.Plan(JourneyPlanningTestData.Request(
            snapshot,
            remainingDriveMinutes: 4 * 60));

        var rest = Assert.Single(result.Segments, segment =>
            segment.Type == JourneyPlanSegmentType.DailyRest);
        Assert.True(rest.EndGameMinute <= snapshot.StartGameMinute + (10 * 60));
        Assert.Equal(11 * 60, rest.DurationMinutes);
    }

    [Fact(DisplayName = "JP-P0-06: daily rest must finish within an active 30 h crew window")]
    [Trait("Stage", "M2Red")]
    public void JP_P0_06()
    {
        var snapshot = JourneyPlanningTestData.Snapshot(
            startGameMinute: 10_000,
            minutesUntilDailyRestDeadline: 10 * 60,
            multiManningActive: true);
        var result = _engine.Plan(JourneyPlanningTestData.Request(
            snapshot,
            remainingDriveMinutes: 4 * 60));

        var rest = Assert.Single(result.Segments, segment =>
            segment.Type == JourneyPlanSegmentType.DailyRest);
        Assert.True(rest.EndGameMinute <= snapshot.StartGameMinute + (10 * 60));
        Assert.True(result.Usage.UsedThirtyHourWindow);
    }

    [Fact(DisplayName = "JP-P0-07: OtherWork buffer cannot cross the daily-rest completion deadline")]
    [Trait("Stage", "M2Red")]
    public void JP_P0_07()
    {
        var snapshot = JourneyPlanningTestData.Snapshot(
            startGameMinute: 10_000,
            minutesUntilDailyRestDeadline: 10 * 60);
        var result = _engine.Plan(JourneyPlanningTestData.Request(
            snapshot,
            remainingDriveMinutes: 60,
            operationalBufferMinutes: 60));

        var buffer = Assert.Single(result.Segments, segment =>
            segment.Reason == JourneyPlanSegmentReason.OperationalBufferAfterArrival);
        var rest = Assert.Single(result.Segments, segment =>
            segment.Type == JourneyPlanSegmentType.DailyRest);
        Assert.Equal(DriverActivity.OtherWork, buffer.RegulatoryActivity);
        Assert.True(rest.EndGameMinute <= snapshot.StartGameMinute + (10 * 60));
    }

    [Fact(DisplayName = "JP-P0-08: unsupported 24 h compensation falls back to 45 h or limited confidence")]
    [Trait("Stage", "M2Red")]
    public void JP_P0_08()
    {
        var request = JourneyPlanningTestData.Request(
            JourneyPlanningTestData.Snapshot(pendingRestAllocation: true),
            remainingDriveMinutes: 10 * 60);

        var result = _engine.Plan(request);

        Assert.True(
            result.Segments.Any(segment =>
                segment.Type == JourneyPlanSegmentType.WeeklyRest &&
                segment.DurationMinutes == 45 * 60) ||
            result.Confidence == JourneyPlanConfidence.LimitedByCompensationModel);
    }
}

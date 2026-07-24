using ETS2Tachograph.RuleEngine.JourneyPlanning;

namespace ETS2Tachograph.RuleEngine.Tests;

public sealed class JourneyPlanningSafetyRedTests
{
    private readonly JourneyPlanningEngine _engine = new();

    [Theory]
    [InlineData(1, 10_000, 10_000)]
    [InlineData(100, 1, 10_000)]
    [InlineData(100, 10_000, 1)]
    [Trait("Stage", "M2Red")]
    public void Safety_limit_terminates_calculation(
        int maximumSegments,
        int maximumElapsedMinutes,
        int maximumVisitedStates)
    {
        var limits = new JourneyPlanningLimits(
            maximumSegments,
            maximumElapsedMinutes,
            maximumVisitedStates);
        var request = JourneyPlanningTestData.Request(
            JourneyPlanningTestData.Snapshot(),
            remainingDriveMinutes: 10 * 60,
            limits: limits);

        var result = _engine.Plan(request);

        Assert.Equal(JourneyPlanStatus.CalculationLimitReached, result.Status);
    }

    [Fact]
    [Trait("Stage", "M2Red")]
    public void Every_segment_advances_time_and_has_positive_duration()
    {
        var result = _engine.Plan(JourneyPlanningTestData.Request(
            JourneyPlanningTestData.Snapshot(),
            remainingDriveMinutes: 8 * 60));

        Assert.All(result.Segments, segment =>
        {
            Assert.True(segment.DurationMinutes > 0);
            Assert.Equal(
                segment.StartGameMinute + segment.DurationMinutes,
                segment.EndGameMinute);
        });
    }

    [Fact]
    [Trait("Stage", "M2Red")]
    public void Same_snapshot_and_request_produce_identical_result()
    {
        var request = JourneyPlanningTestData.Request(
            JourneyPlanningTestData.Snapshot(),
            remainingDriveMinutes: 8 * 60,
            operationalBufferMinutes: 30);

        var first = _engine.Plan(request);
        var second = _engine.Plan(request);

        Assert.Equal(first, second);
    }
}

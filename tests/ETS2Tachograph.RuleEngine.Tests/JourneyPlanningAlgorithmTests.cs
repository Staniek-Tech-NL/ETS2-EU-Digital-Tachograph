using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.RuleEngine.JourneyPlanning;

namespace ETS2Tachograph.RuleEngine.Tests;

public sealed class JourneyPlanningAlgorithmTests
{
    private readonly JourneyPlanningEngine _engine = new();

    [Fact]
    public void Zero_length_route_finishes_at_snapshot_without_segments()
    {
        var snapshot = JourneyPlanningTestData.Snapshot();

        var result = _engine.Plan(JourneyPlanningTestData.Request(
            snapshot,
            remainingDriveMinutes: 0));

        Assert.Equal(JourneyPlanStatus.MeetsDeadline, result.Status);
        Assert.Equal(snapshot.StartGameMinute, result.EarliestArrivalGameMinute);
        Assert.Equal(snapshot.StartGameMinute, result.EarliestCompletionGameMinute);
        Assert.Empty(result.Segments);
    }

    [Fact]
    public void Continuous_driving_limit_adds_full_45_minute_break()
    {
        var result = _engine.Plan(JourneyPlanningTestData.Request(
            JourneyPlanningTestData.Snapshot(),
            remainingDriveMinutes: 271));

        Assert.Collection(
            result.Segments,
            drive => Assert.Equal(270, drive.DurationMinutes),
            pause =>
            {
                Assert.Equal(JourneyPlanSegmentType.Break, pause.Type);
                Assert.Equal(45, pause.DurationMinutes);
                Assert.Equal(JourneyPlanSegmentReason.ContinuousDrivingBreak, pause.Reason);
            },
            drive => Assert.Equal(1, drive.DurationMinutes));
    }

    [Fact]
    public void Existing_15_minute_part_allows_30_minute_split_completion_once()
    {
        var snapshot = JourneyPlanningTestData.Snapshot(
            continuousDrivingMinutes: 270,
            currentContinuousBreakMinutes: 15);

        var result = _engine.Plan(JourneyPlanningTestData.Request(
            snapshot,
            remainingDriveMinutes: 271));

        var breaks = result.Segments
            .Where(segment => segment.Type == JourneyPlanSegmentType.Break)
            .ToArray();
        Assert.Equal(2, breaks.Length);
        Assert.Equal(30, breaks[0].DurationMinutes);
        Assert.Equal(45, breaks[1].DurationMinutes);
        Assert.True(result.Usage.UsedExistingFifteenMinuteBreak);
    }

    [Fact]
    public void Third_daily_extension_is_not_planned()
    {
        var snapshot = JourneyPlanningTestData.Snapshot(
            dailyDrivingMinutes: 540,
            dailyExtensionsUsed: 2);

        var result = _engine.Plan(JourneyPlanningTestData.Request(
            snapshot,
            remainingDriveMinutes: 60));

        Assert.Equal(JourneyPlanSegmentType.DailyRest, result.Segments[0].Type);
        Assert.Equal(0, result.Usage.DailyDrivingExtensionsUsed);
    }

    [Fact]
    public void Available_extension_allows_ten_hours_then_is_reported()
    {
        var snapshot = JourneyPlanningTestData.Snapshot(
            dailyDrivingMinutes: 540,
            dailyExtensionsUsed: 1);

        var result = _engine.Plan(JourneyPlanningTestData.Request(
            snapshot,
            remainingDriveMinutes: 61));

        Assert.Equal(60, result.Segments[0].DurationMinutes);
        Assert.Contains(result.Segments, segment =>
            segment.Type == JourneyPlanSegmentType.DailyRest);
        Assert.Equal(1, result.Usage.DailyDrivingExtensionsUsed);
    }

    [Theory]
    [InlineData(2, 540)]
    [InlineData(3, 660)]
    public void Daily_rest_uses_nine_or_eleven_hours_according_to_available_reductions(
        int reducedRestsUsed,
        int expectedDuration)
    {
        var snapshot = JourneyPlanningTestData.Snapshot(
            dailyDrivingMinutes: 600,
            reducedDailyRestsUsed: reducedRestsUsed);

        var result = _engine.Plan(JourneyPlanningTestData.Request(
            snapshot,
            remainingDriveMinutes: 1));

        var rest = Assert.Single(result.Segments, segment =>
            segment.Type == JourneyPlanSegmentType.DailyRest);
        Assert.Equal(expectedDuration, rest.DurationMinutes);
    }

    [Fact]
    public void Operational_buffer_changes_completion_but_not_arrival()
    {
        var snapshot = JourneyPlanningTestData.Snapshot();
        var result = _engine.Plan(JourneyPlanningTestData.Request(
            snapshot,
            remainingDriveMinutes: 60,
            operationalBufferMinutes: 30));

        Assert.Equal(snapshot.StartGameMinute + 60, result.EarliestArrivalGameMinute);
        Assert.Equal(snapshot.StartGameMinute + 90, result.EarliestCompletionGameMinute);
        var buffer = Assert.Single(result.Segments, segment =>
            segment.Reason == JourneyPlanSegmentReason.OperationalBufferAfterArrival);
        Assert.Equal(DriverActivity.OtherWork, buffer.RegulatoryActivity);
    }

    [Theory]
    [InlineData(60, JourneyPlanStatus.MeetsDeadline, 0)]
    [InlineData(59, JourneyPlanStatus.MissesDeadline, -1)]
    public void Deadline_status_and_margin_are_derived_from_completion(
        int deliveryWindow,
        JourneyPlanStatus expectedStatus,
        int expectedMargin)
    {
        var result = _engine.Plan(JourneyPlanningTestData.Request(
            JourneyPlanningTestData.Snapshot(),
            remainingDriveMinutes: 60,
            deliveryWindowMinutes: deliveryWindow));

        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(expectedMargin, result.MarginMinutes);
    }

    [Fact]
    public void Unresolved_card_removed_gap_blocks_planning()
    {
        var gap = new ActivityGap
        {
            Id = Guid.NewGuid(),
            DriverCardId = "S1",
            Slot = 1,
            SessionIndex = 0,
            Start = new GameTime(9_000),
            EndExclusive = new GameTime(9_100),
            Reason = ActivityGapReason.CardRemoved,
            State = ActivityGapState.Unresolved
        };
        var snapshot = JourneyPlanningTestData.Snapshot() with { Gaps = [gap] };

        var result = _engine.Plan(JourneyPlanningTestData.Request(snapshot));

        Assert.Equal(JourneyPlanStatus.BlockedByGap, result.Status);
        Assert.Empty(result.Segments);
    }

    [Fact]
    public void Invalid_negative_request_is_controlled_insufficient_data()
    {
        var result = _engine.Plan(JourneyPlanningTestData.Request(
            JourneyPlanningTestData.Snapshot(),
            remainingDriveMinutes: -1));

        Assert.Equal(JourneyPlanStatus.InsufficientData, result.Status);
    }

    [Fact]
    public void Planning_does_not_mutate_snapshot_regulation_state_or_history()
    {
        var snapshot = JourneyPlanningTestData.Snapshot(
            weeklyDrivingMinutes: 56 * 60);
        var originalState = snapshot.Evaluation.State;
        var originalHistory = snapshot.History;

        _engine.Plan(JourneyPlanningTestData.Request(snapshot));

        Assert.Same(originalState, snapshot.Evaluation.State);
        Assert.Same(originalHistory, snapshot.History);
        Assert.Equal(56 * 60, snapshot.Evaluation.State.WeeklyDrivingMinutes);
        Assert.Empty(snapshot.History);
    }

    [Fact]
    public void Calendar_wait_uses_the_same_regulatory_week_offset_as_rule_engine()
    {
        var snapshot = JourneyPlanningTestData.Snapshot(
            startGameMinute: 8_000,
            weeklyDrivingMinutes: 56 * 60,
            weekEpochOffsetDays: 1);

        var result = _engine.Plan(JourneyPlanningTestData.Request(snapshot));

        var wait = Assert.Single(result.Segments, segment =>
            segment.Type == JourneyPlanSegmentType.CalendarWait);
        Assert.Equal(640, wait.DurationMinutes);
        Assert.Equal(8_640, wait.EndGameMinute);
    }

    [Fact]
    public void Missing_telemetry_preserves_plan_with_last_saved_state_confidence()
    {
        var snapshot = JourneyPlanningTestData.Snapshot() with
        {
            TelemetryAvailable = false
        };

        var result = _engine.Plan(JourneyPlanningTestData.Request(snapshot));

        Assert.Equal(JourneyPlanConfidence.BasedOnLastSavedState, result.Confidence);
        Assert.Contains(result.Warnings, warning =>
            warning.Code == JourneyPlanWarningCode.LastSavedState);
    }

    [Fact]
    public void Reduced_weekly_rest_reports_the_compensation_created_by_current_model()
    {
        var snapshot = JourneyPlanningTestData.Snapshot(
            minutesUntilWeeklyRestDeadline: 0);

        var result = _engine.Plan(JourneyPlanningTestData.Request(snapshot));

        Assert.True(result.Usage.UsedReducedWeeklyRest);
        Assert.Equal(1_260, result.Usage.RecognizedCompensationObligationMinutes);
    }
}

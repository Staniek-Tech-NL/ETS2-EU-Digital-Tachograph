using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Rules;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.RuleEngine.Tests;

public sealed class ManualGapDailyResetTests
{
    private readonly RegulationEngine _engine = new();

    [Fact]
    public void Ten_hour_manual_gap_rest_resets_day_at_rest_block_end()
    {
        var gapId = Guid.NewGuid();

        var result = Evaluate(
        [
            Record(0, 120, DriverActivity.Driving),
            Manual(120, 720, DriverActivity.BreakOrRest, gapId),
            Record(720, 780, DriverActivity.Driving)
        ], 780);

        Assert.Equal(60, result.State.DailyDrivingMinutes);
        Assert.Equal(new GameTime(720), result.State.LastDailyRestResetAt);
        var rest = Assert.Single(result.QualifiedRests, item => item.SourceGapId == gapId);
        Assert.Equal(600, rest.DurationMinutes);
        Assert.Equal(DailyRestClassification.Reduced, rest.DailyClassification);
        Assert.Null(rest.WeeklyClassification);
    }

    [Fact]
    public void Six_hours_rest_two_hours_work_four_hours_rest_does_not_reset_day()
    {
        var gapId = Guid.NewGuid();

        var result = Evaluate(
        [
            Record(0, 60, DriverActivity.Driving),
            Manual(60, 420, DriverActivity.BreakOrRest, gapId),
            Manual(420, 540, DriverActivity.OtherWork, gapId),
            Manual(540, 780, DriverActivity.BreakOrRest, gapId),
            Record(780, 840, DriverActivity.Driving)
        ], 840);

        Assert.Null(result.State.LastDailyRestResetAt);
        Assert.Equal(120, result.State.DailyDrivingMinutes);
        Assert.Equal(240, result.State.DailyWorkMinutes);
        Assert.DoesNotContain(result.QualifiedRests, item => item.SourceGapId == gapId);
    }

    [Fact]
    public void Six_hours_rest_two_hours_availability_four_hours_rest_does_not_reset_day()
    {
        var gapId = Guid.NewGuid();

        var result = Evaluate(
        [
            Record(0, 60, DriverActivity.Driving),
            Manual(60, 420, DriverActivity.BreakOrRest, gapId),
            Manual(420, 540, DriverActivity.Availability, gapId),
            Manual(540, 780, DriverActivity.BreakOrRest, gapId),
            Record(780, 840, DriverActivity.Driving)
        ], 840);

        Assert.Null(result.State.LastDailyRestResetAt);
        Assert.Equal(120, result.State.DailyDrivingMinutes);
        Assert.Equal(120, result.State.DailyWorkMinutes);
        Assert.DoesNotContain(result.QualifiedRests, item => item.SourceGapId == gapId);
    }

    [Theory]
    [InlineData(540, true)]
    [InlineData(539, false)]
    public void Nine_hours_resets_but_eight_hours_fifty_nine_minutes_does_not(
        long restMinutes,
        bool expectedReset)
    {
        var gapId = Guid.NewGuid();
        var restEnd = 60 + restMinutes;

        var result = Evaluate(
        [
            Record(0, 60, DriverActivity.Driving),
            Manual(60, restEnd, DriverActivity.BreakOrRest, gapId),
            Record(restEnd, restEnd + 30, DriverActivity.Driving)
        ], restEnd + 30);

        Assert.Equal(expectedReset ? new GameTime(restEnd) : null, result.State.LastDailyRestResetAt);
        Assert.Equal(expectedReset ? 30 : 90, result.State.DailyDrivingMinutes);
    }

    [Fact]
    public void Reset_is_retroactive_and_only_activity_after_stamp_belongs_to_new_day()
    {
        var gapId = Guid.NewGuid();

        var result = Evaluate(
        [
            Record(0, 300, DriverActivity.Driving),
            Manual(300, 840, DriverActivity.BreakOrRest, gapId),
            Record(840, 900, DriverActivity.OtherWork),
            Record(900, 990, DriverActivity.Driving)
        ], 990);

        Assert.Equal(new GameTime(840), result.State.LastDailyRestResetAt);
        Assert.Equal(90, result.State.DailyDrivingMinutes);
        Assert.Equal(150, result.State.DailyWorkMinutes);
    }

    [Fact]
    public void Mixed_gap_uses_only_longest_uninterrupted_rest_block()
    {
        var gapId = Guid.NewGuid();

        var result = Evaluate(
        [
            Record(0, 60, DriverActivity.Driving),
            Manual(60, 300, DriverActivity.BreakOrRest, gapId),
            Manual(300, 360, DriverActivity.OtherWork, gapId),
            Manual(360, 900, DriverActivity.BreakOrRest, gapId),
            Manual(900, 960, DriverActivity.Availability, gapId),
            Manual(960, 1_020, DriverActivity.BreakOrRest, gapId),
            Record(1_020, 1_050, DriverActivity.Driving)
        ], 1_050);

        Assert.Equal(new GameTime(900), result.State.LastDailyRestResetAt);
        Assert.Equal(30, result.State.DailyDrivingMinutes);
        var rest = Assert.Single(result.QualifiedRests, item => item.SourceGapId == gapId);
        Assert.Equal(new GameTime(360), rest.Start);
        Assert.Equal(new GameTime(900), rest.EndExclusive);
        Assert.Equal(540, rest.DurationMinutes);
    }

    [Fact]
    public void Two_hours_measured_rest_plus_seven_hours_manual_rest_is_continuous()
    {
        var gapId = Guid.NewGuid();

        var result = Evaluate(
        [
            Record(0, 60, DriverActivity.Driving),
            Record(60, 180, DriverActivity.BreakOrRest),
            Manual(180, 600, DriverActivity.BreakOrRest, gapId),
            Record(600, 660, DriverActivity.Driving)
        ], 660);

        Assert.Equal(new GameTime(600), result.State.LastDailyRestResetAt);
        Assert.Equal(60, result.State.DailyDrivingMinutes);
        var rest = Assert.Single(result.QualifiedRests);
        Assert.Equal(new GameTime(60), rest.Start);
        Assert.Equal(new GameTime(600), rest.EndExclusive);
        Assert.Equal(540, rest.DurationMinutes);
        Assert.Equal(gapId, rest.SourceGapId);
    }

    [Fact]
    public void Seven_hours_manual_rest_plus_two_hours_measured_rest_is_continuous()
    {
        var gapId = Guid.NewGuid();

        var result = Evaluate(
        [
            Record(0, 60, DriverActivity.Driving),
            Manual(60, 480, DriverActivity.BreakOrRest, gapId),
            Record(480, 600, DriverActivity.BreakOrRest),
            Record(600, 660, DriverActivity.Driving)
        ], 660);

        Assert.Equal(new GameTime(600), result.State.LastDailyRestResetAt);
        Assert.Equal(60, result.State.DailyDrivingMinutes);
        var rest = Assert.Single(result.QualifiedRests);
        Assert.Equal(new GameTime(60), rest.Start);
        Assert.Equal(new GameTime(600), rest.EndExclusive);
        Assert.Equal(gapId, rest.SourceGapId);
    }

    [Fact]
    public void Rest_can_continue_on_both_sides_of_a_resolved_manual_gap()
    {
        var gapId = Guid.NewGuid();

        var result = Evaluate(
        [
            Record(0, 60, DriverActivity.Driving),
            Record(60, 180, DriverActivity.BreakOrRest),
            Manual(180, 480, DriverActivity.BreakOrRest, gapId),
            Record(480, 600, DriverActivity.BreakOrRest),
            Record(600, 660, DriverActivity.Driving)
        ], 660);

        Assert.Equal(new GameTime(600), result.State.LastDailyRestResetAt);
        var rest = Assert.Single(result.QualifiedRests);
        Assert.Equal(540, rest.DurationMinutes);
        Assert.Equal(gapId, rest.SourceGapId);
    }

    [Fact]
    public void Unresolved_gap_still_breaks_rest_continuity()
    {
        var result = Evaluate(
        [
            Record(0, 60, DriverActivity.Driving),
            Record(60, 180, DriverActivity.BreakOrRest),
            // [180, 300) is intentionally absent: an unresolved gap.
            Record(300, 720, DriverActivity.BreakOrRest),
            Record(720, 780, DriverActivity.Driving)
        ], 780);

        Assert.Null(result.State.LastDailyRestResetAt);
        Assert.Equal(120, result.State.DailyDrivingMinutes);
    }

    [Theory]
    [InlineData(1_200, DailyRestClassification.Regular, null)]
    [InlineData(1_440, DailyRestClassification.Regular, WeeklyRestClassification.Reduced)]
    [InlineData(2_700, DailyRestClassification.Regular, WeeklyRestClassification.Regular)]
    public void Rest_classification_uses_actual_duration(
        long duration,
        DailyRestClassification daily,
        WeeklyRestClassification? weekly)
    {
        var gapId = Guid.NewGuid();

        var result = Evaluate(
            [Manual(0, duration, DriverActivity.BreakOrRest, gapId)],
            duration);

        var rest = Assert.Single(result.QualifiedRests, item => item.SourceGapId == gapId);
        Assert.Equal(daily, rest.DailyClassification);
        Assert.Equal(weekly, rest.WeeklyClassification);
    }

    private RegulationEvaluation Evaluate(IReadOnlyList<ActivityRecord> history, long now) =>
        _engine.Evaluate(new RuleContext(new GameTime(now), history));

    private static ActivityRecord Manual(
        long start,
        long end,
        DriverActivity activity,
        Guid gapId) => Record(start, end, activity) with
    {
        Source = ActivitySource.ManualEntry,
        SourceGapId = gapId
    };

    private static ActivityRecord Record(long start, long end, DriverActivity activity) => new()
    {
        Id = Guid.NewGuid(),
        DriverCardId = "CARD-MANUAL-RESET",
        Activity = activity,
        Start = new GameTime(start),
        EndExclusive = new GameTime(end),
        RecordedAtUtc = DateTimeOffset.UtcNow,
        Source = ActivitySource.Telemetry
    };
}

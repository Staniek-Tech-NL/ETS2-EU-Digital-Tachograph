using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Rules;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.RuleEngine.Tests;

public sealed class RegulationEngineTests
{
    private readonly RegulationEngine _engine = new();

    [Theory]
    [InlineData(DriverActivity.Driving)]
    [InlineData(DriverActivity.OutOfScope)]
    [InlineData(DriverActivity.Unknown)]
    public void Rule_engine_rejects_disallowed_manual_entry_activity(DriverActivity activity)
    {
        var record = Record(0, 1, activity) with
        {
            Source = ActivitySource.ManualEntry,
            SourceGapId = Guid.NewGuid()
        };

        Assert.Throws<InvalidOperationException>(() => Evaluate([record], 1));
    }

    [Theory]
    [InlineData(270, false)]
    [InlineData(271, true)]
    public void Continuous_driving_limit_is_four_and_half_hours(long minutes, bool violated)
    {
        var result = Evaluate([Record(0, minutes, DriverActivity.Driving)], minutes);

        Assert.Equal(violated, Has(result, ViolationType.ContinuousDrivingExceeded));
        Assert.Equal(270 - minutes, result.State.MinutesUntilBreak);
    }

    [Fact]
    public void Uninterrupted_45_minute_break_resets_continuous_driving()
    {
        var result = Evaluate(
        [
            Record(0, 270, DriverActivity.Driving),
            Record(270, 315, DriverActivity.BreakOrRest),
            Record(315, 415, DriverActivity.Driving)
        ], 415);

        Assert.Equal(100, result.State.ContinuousDrivingMinutes);
    }

    [Fact]
    public void Split_15_then_30_minute_break_resets_continuous_driving()
    {
        var result = Evaluate(
        [
            Record(0, 200, DriverActivity.Driving),
            Record(200, 215, DriverActivity.BreakOrRest),
            Record(215, 285, DriverActivity.Driving),
            Record(285, 315, DriverActivity.BreakOrRest),
            Record(315, 325, DriverActivity.Driving)
        ], 325);

        Assert.Equal(10, result.State.ContinuousDrivingMinutes);
    }

    [Fact]
    public void Reversed_30_then_15_split_does_not_reset_driving()
    {
        var result = Evaluate(
        [
            Record(0, 200, DriverActivity.Driving),
            Record(200, 230, DriverActivity.BreakOrRest),
            Record(230, 300, DriverActivity.Driving),
            Record(300, 315, DriverActivity.BreakOrRest),
            Record(315, 316, DriverActivity.Driving)
        ], 316);

        Assert.Equal(271, result.State.ContinuousDrivingMinutes);
    }

    [Fact]
    public void More_than_10_hours_daily_driving_is_a_violation()
    {
        var result = Evaluate([Record(0, 601, DriverActivity.Driving)], 601);

        Assert.True(Has(result, ViolationType.DailyDrivingExceeded));
    }

    [Fact]
    public void Exactly_nine_hours_does_not_use_daily_extension()
    {
        var result = Evaluate([Record(0, 540, DriverActivity.Driving)], 540);

        Assert.Equal(540, result.State.DailyDrivingMinutes);
        Assert.Equal(0, result.State.DailyExtensionsUsedThisWeek);
        Assert.False(Has(result, ViolationType.DailyDrivingExceeded));
        Assert.False(Has(result, ViolationType.TooManyDailyExtensions));
    }

    [Fact]
    public void Nine_hours_and_one_minute_uses_daily_extension()
    {
        var result = Evaluate([Record(0, 541, DriverActivity.Driving)], 541);

        Assert.Equal(541, result.State.DailyDrivingMinutes);
        Assert.Equal(1, result.State.DailyExtensionsUsedThisWeek);
        Assert.False(Has(result, ViolationType.DailyDrivingExceeded));
        Assert.False(Has(result, ViolationType.TooManyDailyExtensions));
    }

    [Fact]
    public void Third_daily_extension_in_one_week_is_a_violation()
    {
        var history = new List<ActivityRecord>();
        long cursor = 0;
        for (var day = 0; day < 3; day++)
        {
            history.Add(Record(cursor, cursor + 550, DriverActivity.Driving));
            cursor += 550;
            history.Add(Record(cursor, cursor + 540, DriverActivity.BreakOrRest));
            cursor += 540;
        }

        var result = Evaluate(history, cursor);

        Assert.Equal(3, result.State.DailyExtensionsUsedThisWeek);
        Assert.True(Has(result, ViolationType.TooManyDailyExtensions));
    }

    [Fact]
    public void Daily_extensions_reset_at_shifted_regulatory_week_boundary()
    {
        var options = new RegulationOptions { WeekEpochOffsetDays = 1 };
        var completedOldWeekPeriod = new[]
        {
            Record(6_500, 7_041, DriverActivity.Driving),
            Record(7_041, 7_581, DriverActivity.BreakOrRest)
        };

        var beforeBoundary = Evaluate(completedOldWeekPeriod, 7_581, options);
        var afterBoundary = Evaluate(
        [
            .. completedOldWeekPeriod,
            Record(7_581, 8_641, DriverActivity.OtherWork)
        ], 8_641, options);

        Assert.Equal(1, beforeBoundary.State.DailyExtensionsUsedThisWeek);
        Assert.Equal(0, afterBoundary.State.DailyExtensionsUsedThisWeek);
    }

    [Fact]
    public void Daily_period_crossing_regulatory_week_belongs_to_week_where_period_ends()
    {
        var result = Evaluate(
            [Record(8_500, 9_041, DriverActivity.Driving)],
            9_041,
            new RegulationOptions { WeekEpochOffsetDays = 1 });

        Assert.Equal(1, result.State.DailyExtensionsUsedThisWeek);
    }

    [Fact]
    public void Weekly_driving_over_56_hours_is_a_violation()
    {
        var result = Evaluate([Record(0, 3_361, DriverActivity.Driving)], 3_361);

        Assert.True(Has(result, ViolationType.WeeklyDrivingExceeded));
    }

    [Fact]
    public void Two_week_driving_over_90_hours_is_a_violation()
    {
        var result = Evaluate(
        [
            Record(0, 2_700, DriverActivity.Driving),
            Record(10_080, 12_781, DriverActivity.Driving)
        ], 12_781);

        Assert.Equal(5_401, result.State.FortnightlyDrivingMinutes);
        Assert.True(Has(result, ViolationType.FortnightlyDrivingExceeded));
    }

    [Fact]
    public void Single_manning_requires_daily_rest_within_24_hours()
    {
        var result = Evaluate([Record(0, 1_441, DriverActivity.OtherWork)], 1_441);

        Assert.True(Has(result, ViolationType.DailyRestMissing));
    }

    [Fact]
    public void Multi_manning_uses_30_hour_daily_window()
    {
        var result = Evaluate(
            [Record(0, 1_441, DriverActivity.OtherWork)],
            1_441,
            new RegulationOptions { MultiManning = true });

        Assert.False(Has(result, ViolationType.DailyRestMissing));
        Assert.Equal(359, result.State.MinutesUntilDailyRestDeadline);
    }

    [Fact]
    public void Gap_does_not_join_rest_blocks_on_its_two_sides()
    {
        var result = Evaluate(
        [
            Record(0, 60, DriverActivity.Driving),
            Record(60, 420, DriverActivity.BreakOrRest),
            Record(421, 661, DriverActivity.BreakOrRest)
        ], 661);

        Assert.Equal(60, result.State.DailyDrivingMinutes);
    }

    [Fact]
    public void Four_reduced_daily_rests_between_weekly_rests_are_invalid()
    {
        var history = new List<ActivityRecord> { Record(0, 1_440, DriverActivity.BreakOrRest) };
        long cursor = 1_440;
        for (var index = 0; index < 4; index++)
        {
            history.Add(Record(cursor, cursor + 60, DriverActivity.OtherWork));
            cursor += 60;
            history.Add(Record(cursor, cursor + 540, DriverActivity.BreakOrRest));
            cursor += 540;
        }

        var result = Evaluate(history, cursor);

        Assert.Equal(4, result.State.ReducedDailyRestsSinceWeeklyRest);
        Assert.True(Has(result, ViolationType.TooManyReducedDailyRests));
    }

    [Fact]
    public void Weekly_rest_must_start_within_six_24_hour_periods()
    {
        var result = Evaluate([Record(0, 8_641, DriverActivity.OtherWork)], 8_641);

        Assert.True(Has(result, ViolationType.WeeklyRestMissing));
    }

    [Fact]
    public void Reduced_weekly_rest_creates_compensation_obligation()
    {
        var result = Evaluate([Record(0, 2_400, DriverActivity.BreakOrRest)], 2_400);

        var compensation = Assert.Single(result.Compensations);
        Assert.Equal(300, compensation.OwedMinutes);
        Assert.False(compensation.IsOverdue);
        Assert.Equal(300, result.CompensationSummary.TotalOwedMinutes);
        Assert.Equal(1, result.CompensationSummary.Count);
        Assert.Equal(compensation.DueByEndOfWeek, result.CompensationSummary.NearestDueByEndOfWeek);
        Assert.False(result.CompensationSummary.HasOverdue);
    }

    [Fact]
    public void No_weekly_rest_debt_exposes_empty_compensation_summary()
    {
        var result = Evaluate([Record(0, 60, DriverActivity.OtherWork)], 60);

        Assert.Equal(CompensationSummary.Empty, result.CompensationSummary);
    }

    [Fact]
    public void Compensation_summary_preserves_count_total_nearest_due_and_overdue_state()
    {
        var summary = CompensationSummary.From(
        [
            new WeeklyRestCompensation(900, new GameWeek(31), new GameWeek(34), false),
            new WeeklyRestCompensation(360, new GameWeek(30), new GameWeek(33), true)
        ]);

        Assert.Equal(1_260, summary.TotalOwedMinutes);
        Assert.Equal(2, summary.Count);
        Assert.Equal(new GameWeek(33), summary.NearestDueByEndOfWeek);
        Assert.True(summary.HasOverdue);
    }

    [Fact]
    public void Extra_regular_rest_can_satisfy_oldest_compensation()
    {
        var result = Evaluate(
        [
            Record(0, 2_400, DriverActivity.BreakOrRest),
            Record(2_400, 2_460, DriverActivity.OtherWork),
            Record(2_460, 5_460, DriverActivity.BreakOrRest)
        ], 5_460);

        Assert.Empty(result.Compensations);
    }

    [Fact]
    public void Twenty_hour_rest_attaches_eleven_hours_to_weekly_compensation()
    {
        var result = Evaluate(
        [
            Record(0, 1_440, DriverActivity.BreakOrRest),
            Record(1_440, 1_500, DriverActivity.OtherWork),
            Record(1_500, 2_700, DriverActivity.BreakOrRest)
        ], 2_700);

        var compensation = Assert.Single(result.Compensations);
        Assert.Equal(600, compensation.OwedMinutes);
    }

    [Fact]
    public void Two_completed_weeks_require_at_least_one_regular_weekly_rest()
    {
        var result = Evaluate(
        [
            Record(0, 2_000, DriverActivity.OtherWork),
            Record(2_000, 3_440, DriverActivity.BreakOrRest),
            Record(3_440, 12_000, DriverActivity.OtherWork),
            Record(12_000, 13_440, DriverActivity.BreakOrRest),
            Record(13_440, 20_161, DriverActivity.OtherWork)
        ], 20_161);

        Assert.True(Has(result, ViolationType.WeeklyRestPatternInvalid));
    }

    [Fact]
    public void Regular_and_reduced_weekly_rest_satisfy_two_week_pattern()
    {
        var result = Evaluate(
        [
            Record(0, 2_000, DriverActivity.OtherWork),
            Record(2_000, 4_700, DriverActivity.BreakOrRest),
            Record(4_700, 12_000, DriverActivity.OtherWork),
            Record(12_000, 13_440, DriverActivity.BreakOrRest),
            Record(13_440, 20_161, DriverActivity.OtherWork)
        ], 20_161);

        Assert.False(Has(result, ViolationType.WeeklyRestPatternInvalid));
    }

    private RegulationEvaluation Evaluate(
        IReadOnlyList<ActivityRecord> history,
        long now,
        RegulationOptions? options = null) =>
        _engine.Evaluate(new RuleContext(new GameTime(now), history), options);

    private static bool Has(RegulationEvaluation result, ViolationType type) =>
        result.Violations.Any(violation => violation.Type == type);

    private static ActivityRecord Record(long start, long end, DriverActivity activity) => new()
    {
        Id = Guid.NewGuid(),
        DriverCardId = "PL-TEST",
        Activity = activity,
        Start = new GameTime(start),
        EndExclusive = new GameTime(end),
        RecordedAtUtc = DateTimeOffset.UtcNow
    };
}

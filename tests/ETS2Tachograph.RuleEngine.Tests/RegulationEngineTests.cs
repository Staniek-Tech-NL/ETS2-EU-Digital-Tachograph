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
        var result = Evaluate(
        [
            Record(0, 2_400, DriverActivity.BreakOrRest),
            Record(2_400, 2_401, DriverActivity.OtherWork)
        ], 2_401);

        var compensation = Assert.Single(result.Compensations);
        Assert.Equal(300, compensation.OwedMinutes);
        Assert.Equal(300, compensation.OriginalOwedMinutes);
        Assert.Equal(WeeklyRestCompensationStatus.OpenOnTime, compensation.Status);
        Assert.False(compensation.IsOverdue);
        Assert.Equal(300, result.CompensationSummary.TotalOwedMinutes);
        Assert.Equal(1, result.CompensationSummary.Count);
        Assert.Equal(compensation.DueByEndOfWeek, result.CompensationSummary.NearestDueByEndOfWeek);
        Assert.False(result.CompensationSummary.HasOverdue);
    }

    [Fact]
    public void OngoingReducedWeeklyRest_DoesNotCreateObligationUntilClosed()
    {
        var ongoing = Evaluate(
            [Record(0, 2_400, DriverActivity.BreakOrRest)],
            2_400);
        var closed = Evaluate(
        [
            Record(0, 2_400, DriverActivity.BreakOrRest),
            Record(2_400, 2_401, DriverActivity.OtherWork)
        ], 2_401);

        Assert.Empty(ongoing.CompensationObligations);
        Assert.Single(closed.CompensationObligations);
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
            Record(2_460, 5_460, DriverActivity.BreakOrRest),
            Record(5_460, 5_461, DriverActivity.OtherWork)
        ], 5_461);

        Assert.Empty(result.Compensations);
        var compensation = Assert.Single(result.CompensationObligations);
        Assert.Equal(WeeklyRestCompensationStatus.PaidOnTime, compensation.Status);
        Assert.Equal(0, compensation.RemainingMinutes);
    }

    [Fact]
    public void Twenty_hour_rest_does_not_partially_reduce_weekly_compensation()
    {
        var result = Evaluate(
        [
            Record(0, 1_440, DriverActivity.BreakOrRest),
            Record(1_440, 1_500, DriverActivity.OtherWork),
            Record(1_500, 2_700, DriverActivity.BreakOrRest),
            Record(2_700, 2_701, DriverActivity.OtherWork)
        ], 2_701);

        var compensation = Assert.Single(result.Compensations);
        Assert.Equal(1_260, compensation.OwedMinutes);
    }

    [Fact]
    public void Staniek_DoesNotAggregateCompensationFragments_Leaves1253MinutesOpen()
    {
        // Approved reference data: WEEKLY_REST_COMPENSATION_REFERENCE_DATA_2026-07-22.md.
        const string cardId = "Staniek";
        var history = new[]
        {
            RestRecord(cardId, 186_055, 187_502, ActivitySource.ManualEntry,
                "0F368EE5-460D-43C8-9059-F28B5165C7E3"),
            RestRecord(cardId, 188_105, 188_767, ActivitySource.Mixed),
            RestRecord(cardId, 190_059, 190_743, ActivitySource.Mixed),
            RestRecord(cardId, 192_051, 192_774, ActivitySource.ManualEntry,
                "2231AB20-B921-4442-80AB-49FBDDA88E22"),
            RestRecord(cardId, 194_086, 194_749, ActivitySource.Mixed),
            RestRecord(cardId, 195_807, 196_474, ActivitySource.Mixed),
            RestRecord(cardId, 196_476, 199_712, ActivitySource.Mixed,
                "ACC1278D-0FB1-4591-8006-ECE231EF7350")
        };

        var result = Evaluate(history, 199_714);

        var compensation = Assert.Single(result.Compensations);
        Assert.Equal(1_253, compensation.OwedMinutes);
        Assert.Equal(1_253, result.CompensationSummary.TotalOwedMinutes);
    }

    [Fact]
    public void Dobos_DoesNotAggregateCompensationFragments_Leaves1192MinutesOpen()
    {
        // Approved reference data: WEEKLY_REST_COMPENSATION_REFERENCE_DATA_2026-07-22.md.
        const string cardId = "Doboś";
        var history = new[]
        {
            RestRecord(cardId, 187_260, 188_768, ActivitySource.Mixed,
                "81C8CB6D-1FE0-4ADF-9E0A-AF91910573EC"),
            RestRecord(cardId, 190_059, 190_742, ActivitySource.Mixed),
            RestRecord(cardId, 192_051, 192_775, ActivitySource.ManualEntry,
                "2B70FAC9-B06F-4EF7-ADDA-C34E3B98F4CE"),
            RestRecord(cardId, 194_086, 194_749, ActivitySource.Mixed),
            RestRecord(cardId, 195_807, 196_474, ActivitySource.Mixed),
            RestRecord(cardId, 196_751, 199_713, ActivitySource.Mixed,
                "EFDC2D8D-7CE7-4525-A16C-70D585269377")
        };

        var result = Evaluate(history, 199_714);

        var compensation = Assert.Single(result.Compensations);
        Assert.Equal(1_192, compensation.OwedMinutes);
        Assert.Equal(1_192, result.CompensationSummary.TotalOwedMinutes);
    }

    [Fact]
    public void SingleCompensationBlock_OneMinuteTooShort_DoesNotReduceDebt()
    {
        var result = Evaluate(
        [
            Record(0, 2_400, DriverActivity.BreakOrRest),
            Record(2_400, 2_460, DriverActivity.OtherWork),
            Record(2_460, 3_299, DriverActivity.BreakOrRest),
            Record(3_299, 3_300, DriverActivity.OtherWork)
        ], 3_300);

        var compensation = Assert.Single(result.Compensations);
        Assert.Equal(300, compensation.OriginalOwedMinutes);
        Assert.Equal(300, compensation.RemainingMinutes);
        Assert.Null(compensation.PaymentRestBlockId);
        Assert.Null(compensation.PaymentRange);
        Assert.Null(compensation.SettledAt);
    }

    [Fact]
    public void SingleCompensationBlock_ExactlySufficient_SettlesDebtEnBloc()
    {
        var result = Evaluate(
        [
            Record(0, 2_400, DriverActivity.BreakOrRest),
            Record(2_400, 2_460, DriverActivity.OtherWork),
            Record(2_460, 3_300, DriverActivity.BreakOrRest),
            Record(3_300, 3_301, DriverActivity.OtherWork)
        ], 3_301);

        Assert.Empty(result.Compensations);
        var compensation = Assert.Single(result.CompensationObligations);
        Assert.Equal(0, compensation.RemainingMinutes);
        Assert.NotNull(compensation.PaymentRestBlockId);
        Assert.Equal(new GameTime(3_000), compensation.PaymentRange!.Start);
        Assert.Equal(new GameTime(3_300), compensation.PaymentRange.EndExclusive);
        Assert.Equal(300, compensation.PaymentRange.DurationMinutes);
        Assert.Equal(new GameTime(3_300), compensation.SettledAt);
        Assert.Equal(WeeklyRestCompensationStatus.PaidOnTime, compensation.Status);
    }

    [Fact]
    public void SingleBlock_CanSettleSeveralWholeObligations()
    {
        var result = Evaluate(
        [
            Record(0, 1_440, DriverActivity.BreakOrRest),
            Record(1_440, 1_500, DriverActivity.OtherWork),
            Record(3_000, 4_500, DriverActivity.BreakOrRest),
            Record(4_500, 4_560, DriverActivity.OtherWork),
            Record(6_000, 11_160, DriverActivity.BreakOrRest),
            Record(11_160, 11_161, DriverActivity.OtherWork)
        ], 11_161);

        Assert.Empty(result.Compensations);
        Assert.Equal(2, result.CompensationObligations.Count);
        var ordered = result.CompensationObligations
            .OrderBy(item => item.OriginalOwedMinutes)
            .ToList();
        Assert.Equal([1_200L, 1_260L], ordered.Select(item => item.OriginalOwedMinutes));
        Assert.All(ordered, item => Assert.Equal(0, item.RemainingMinutes));
        Assert.Single(ordered.Select(item => item.PaymentRestBlockId).Distinct());
        var firstByFifo = result.CompensationObligations
            .OrderBy(item => item.SourceRestEndExclusive)
            .First();
        var secondByFifo = result.CompensationObligations
            .OrderBy(item => item.SourceRestEndExclusive)
            .Last();
        Assert.Equal(new GameTime(8_700), firstByFifo.PaymentRange!.Start);
        Assert.Equal(new GameTime(9_960), firstByFifo.PaymentRange.EndExclusive);
        Assert.Equal(new GameTime(9_960), secondByFifo.PaymentRange!.Start);
        Assert.Equal(new GameTime(11_160), secondByFifo.PaymentRange.EndExclusive);
    }

    [Fact]
    public void Fifo_DoesNotSkipEarlierDebtWhenBlockCannotSettleIt()
    {
        var result = Evaluate(
        [
            Record(0, 1_440, DriverActivity.BreakOrRest),
            Record(1_440, 1_441, DriverActivity.OtherWork),
            Record(10_080, 12_480, DriverActivity.BreakOrRest),
            Record(12_480, 12_481, DriverActivity.OtherWork),
            Record(13_000, 13_840, DriverActivity.BreakOrRest),
            Record(13_840, 13_841, DriverActivity.OtherWork)
        ], 13_841);

        Assert.Equal(2, result.Compensations.Count);
        var first = Assert.Single(
            result.Compensations,
            item => item.ReductionWeek == new GameWeek(0));
        var second = Assert.Single(
            result.Compensations,
            item => item.ReductionWeek == new GameWeek(1));
        Assert.Equal(1_260, first.RemainingMinutes);
        Assert.Equal(300, second.RemainingMinutes);
        Assert.Equal(1_560, result.CompensationSummary.TotalOwedMinutes);
        Assert.Null(first.PaymentRestBlockId);
        Assert.Null(second.PaymentRestBlockId);
    }

    [Fact]
    public void Deadline_CompletedOneMinuteBeforeExclusiveBoundary_IsPaidOnTime()
    {
        var result = Evaluate(
        [
            Record(0, 2_400, DriverActivity.BreakOrRest),
            Record(2_400, 2_401, DriverActivity.OtherWork),
            Record(39_479, 40_319, DriverActivity.BreakOrRest),
            Record(40_319, 40_320, DriverActivity.OtherWork)
        ], 40_320);

        var compensation = Assert.Single(result.CompensationObligations);
        Assert.Equal(new GameTime(40_320), compensation.DueAtExclusive);
        Assert.Equal(new GameTime(40_319), compensation.SettledAt);
        Assert.Equal(WeeklyRestCompensationStatus.PaidOnTime, compensation.Status);
        Assert.False(Has(result, ViolationType.WeeklyRestCompensationOverdue));
    }

    [Fact]
    public void Deadline_CompletedAtExclusiveBoundary_IsPaidLate()
    {
        var result = Evaluate(
        [
            Record(0, 2_400, DriverActivity.BreakOrRest),
            Record(2_400, 2_401, DriverActivity.OtherWork),
            Record(39_480, 40_320, DriverActivity.BreakOrRest),
            Record(40_320, 40_321, DriverActivity.OtherWork)
        ], 40_321);

        var compensation = Assert.Single(result.CompensationObligations);
        Assert.Equal(new GameTime(40_320), compensation.DueAtExclusive);
        Assert.Equal(new GameTime(40_320), compensation.SettledAt);
        Assert.Equal(WeeklyRestCompensationStatus.PaidLate, compensation.Status);
        Assert.True(Has(result, ViolationType.WeeklyRestCompensationOverdue));
    }

    [Fact]
    public void Deadline_UnpaidAtExclusiveBoundary_IsOverdue()
    {
        var result = Evaluate(
        [
            Record(0, 2_400, DriverActivity.BreakOrRest),
            Record(2_400, 2_401, DriverActivity.OtherWork)
        ], 40_320);

        var compensation = Assert.Single(result.Compensations);
        Assert.Equal(300, compensation.RemainingMinutes);
        Assert.Equal(WeeklyRestCompensationStatus.Overdue, compensation.Status);
        Assert.True(Has(result, ViolationType.WeeklyRestCompensationOverdue));
    }

    [Fact]
    public void Restart_SameCanonicalHistory_RecreatesIdenticalOpenObligationIdentity()
    {
        var first = new RegulationEngine().Evaluate(
            new RuleContext(new GameTime(2_401), BuildOpenCompensationHistory(splitRest: false)));
        var second = new RegulationEngine().Evaluate(
            new RuleContext(new GameTime(2_401), BuildOpenCompensationHistory(splitRest: true)));

        var firstObligation = Assert.Single(first.CompensationObligations);
        var secondObligation = Assert.Single(second.CompensationObligations);
        Assert.Equal(1, firstObligation.IdentitySchemeVersion);
        Assert.Equal(firstObligation.SourceRestBlockId, secondObligation.SourceRestBlockId);
        Assert.Equal(firstObligation.ObligationId, secondObligation.ObligationId);
        Assert.Equal(firstObligation, secondObligation);
    }

    [Fact]
    public void Restart_SameCanonicalHistory_RecreatesIdenticalPaymentTrace()
    {
        var first = new RegulationEngine().Evaluate(
            new RuleContext(new GameTime(3_301), BuildPaidCompensationHistory(splitRests: false)));
        var second = new RegulationEngine().Evaluate(
            new RuleContext(new GameTime(3_301), BuildPaidCompensationHistory(splitRests: true)));

        var firstObligation = Assert.Single(first.CompensationObligations);
        var secondObligation = Assert.Single(second.CompensationObligations);
        Assert.Equal(firstObligation.SourceRestBlockId, secondObligation.SourceRestBlockId);
        Assert.Equal(firstObligation.ObligationId, secondObligation.ObligationId);
        Assert.Equal(firstObligation.PaymentRestBlockId, secondObligation.PaymentRestBlockId);
        Assert.Equal(firstObligation.PaymentRange, secondObligation.PaymentRange);
        Assert.Equal(firstObligation.SettledAt, secondObligation.SettledAt);
        Assert.Equal(firstObligation.Status, secondObligation.Status);
        Assert.Equal(firstObligation, secondObligation);
    }

    [Fact]
    public void Identity_ChangedSourceRestRange_CreatesNewBlockAndObligationIds()
    {
        var first = new RegulationEngine().Evaluate(new RuleContext(
            new GameTime(2_401),
            [
                Record(0, 2_400, DriverActivity.BreakOrRest),
                Record(2_400, 2_401, DriverActivity.OtherWork)
            ]));
        var changed = new RegulationEngine().Evaluate(new RuleContext(
            new GameTime(2_401),
            [
                Record(0, 2_399, DriverActivity.BreakOrRest),
                Record(2_399, 2_401, DriverActivity.OtherWork)
            ]));

        var firstObligation = Assert.Single(first.CompensationObligations);
        var changedObligation = Assert.Single(changed.CompensationObligations);
        Assert.NotEqual(firstObligation.SourceRestBlockId, changedObligation.SourceRestBlockId);
        Assert.NotEqual(firstObligation.ObligationId, changedObligation.ObligationId);
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

    private static ActivityRecord RestRecord(
        string driverCardId,
        long start,
        long end,
        ActivitySource source,
        string? sourceGapId = null) => Record(start, end, DriverActivity.BreakOrRest) with
    {
        DriverCardId = driverCardId,
        Source = source,
        SourceGapId = sourceGapId is null ? null : Guid.Parse(sourceGapId)
    };

    private static IReadOnlyList<ActivityRecord> BuildOpenCompensationHistory(bool splitRest) =>
        splitRest
            ?
            [
                Record(0, 1_200, DriverActivity.BreakOrRest),
                Record(1_200, 2_400, DriverActivity.BreakOrRest),
                Record(2_400, 2_401, DriverActivity.OtherWork)
            ]
            :
            [
                Record(0, 2_400, DriverActivity.BreakOrRest),
                Record(2_400, 2_401, DriverActivity.OtherWork)
            ];

    private static IReadOnlyList<ActivityRecord> BuildPaidCompensationHistory(bool splitRests) =>
        splitRests
            ?
            [
                Record(0, 1_200, DriverActivity.BreakOrRest),
                Record(1_200, 2_400, DriverActivity.BreakOrRest),
                Record(2_400, 2_460, DriverActivity.OtherWork),
                Record(2_460, 3_000, DriverActivity.BreakOrRest),
                Record(3_000, 3_300, DriverActivity.BreakOrRest),
                Record(3_300, 3_301, DriverActivity.OtherWork)
            ]
            :
            [
                Record(0, 2_400, DriverActivity.BreakOrRest),
                Record(2_400, 2_460, DriverActivity.OtherWork),
                Record(2_460, 3_300, DriverActivity.BreakOrRest),
                Record(3_300, 3_301, DriverActivity.OtherWork)
            ];
}

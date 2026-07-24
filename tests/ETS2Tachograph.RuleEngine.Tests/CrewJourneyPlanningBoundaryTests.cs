using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Rules;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.RuleEngine.JourneyPlanning;

namespace ETS2Tachograph.RuleEngine.Tests;

public sealed class CrewJourneyPlanningBoundaryTests
{
    private readonly CrewJourneyPlanningEngine _engine = new();

    [Theory]
    [InlineData(1, 539, 2)]
    [InlineData(2, 539, 2)]
    [InlineData(1, 599, 1)]
    [InlineData(2, 599, 1)]
    [Trait("Stage", "M2CrewBoundary")]
    public void Daily_9h_and_10h_limits_are_applied_per_card(
        int testedSlot,
        long dailyMinutes,
        int extensionsUsed)
    {
        var tested = State(
            daily: dailyMinutes,
            extensions: extensionsUsed);
        var result = _engine.Plan(Request(
            remainingDriveMinutes: 2,
            initialDrivingSlot: testedSlot,
            slot1: testedSlot == 1 ? tested : State(),
            slot2: testedSlot == 2 ? tested : State()));

        Assert.Equal(testedSlot, result.Segments[0].DrivingSlot);
        Assert.Equal(1, result.Segments[0].DurationMinutes);
        Assert.Equal(testedSlot == 1 ? 2 : 1, result.Segments[1].DrivingSlot);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [Trait("Stage", "M2CrewBoundary")]
    public void Ten_hour_extension_is_available_before_two_extensions_are_used(
        int testedSlot)
    {
        var tested = State(daily: 540, extensions: 1);
        var result = _engine.Plan(Request(
            remainingDriveMinutes: 1,
            initialDrivingSlot: testedSlot,
            slot1: testedSlot == 1 ? tested : State(),
            slot2: testedSlot == 2 ? tested : State()));

        var drive = Assert.Single(result.Segments);
        Assert.Equal(testedSlot, drive.DrivingSlot);
    }

    [Theory]
    [InlineData(1, 3_359, 0)]
    [InlineData(2, 3_359, 0)]
    [InlineData(1, 2_999, 2_400)]
    [InlineData(2, 2_999, 2_400)]
    [Trait("Stage", "M2CrewBoundary")]
    public void Weekly_56h_and_biweekly_90h_limits_are_applied_per_card(
        int testedSlot,
        long weeklyMinutes,
        long previousWeekMinutes)
    {
        var tested = State(
            weekly: weeklyMinutes,
            previousWeek: previousWeekMinutes);
        var result = _engine.Plan(Request(
            remainingDriveMinutes: 2,
            initialDrivingSlot: testedSlot,
            slot1: testedSlot == 1 ? tested : State(),
            slot2: testedSlot == 2 ? tested : State()));

        Assert.Equal(testedSlot, result.Segments[0].DrivingSlot);
        Assert.Equal(1, result.Segments[0].DurationMinutes);
        Assert.Equal(testedSlot == 1 ? 2 : 1, result.Segments[1].DrivingSlot);
    }

    [Fact]
    [Trait("Stage", "M2CrewBoundary")]
    public void Both_weekly_limits_add_calendar_wait_before_vehicle_continues()
    {
        var result = _engine.Plan(Request(
            remainingDriveMinutes: 1,
            slot1: State(weekly: 3_360),
            slot2: State(weekly: 3_360)));

        Assert.Collection(
            result.Segments,
            wait =>
            {
                Assert.Null(wait.DrivingSlot);
                Assert.Equal(
                    JourneyPlanSegmentReason.WaitForNewRegulatoryWeek,
                    wait.Reason);
            },
            drive => Assert.Equal(1, drive.DrivingSlot));
        Assert.Equal(1, result.Slot1.WeeklyDrivingMinutes);
        Assert.Equal(3_360, result.Slot1.PreviousWeekDrivingMinutes);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [Trait("Stage", "M2CrewBoundary")]
    public void Earlier_30h_deadline_of_either_card_controls_the_vehicle(
        int urgentSlot)
    {
        const long start = 1_000;
        var urgent = State(dailyDeadline: 600);
        var result = _engine.Plan(Request(
            remainingDriveMinutes: 120,
            startGameMinute: start,
            slot1: urgentSlot == 1 ? urgent : State(),
            slot2: urgentSlot == 2 ? urgent : State()));

        var rest = Assert.Single(
            result.Segments,
            segment => segment.Reason == JourneyPlanSegmentReason.DailyRestDeadline);
        Assert.Equal(540, rest.DurationMinutes);
        Assert.Equal(start + 600, rest.EndGameMinute);
        Assert.Equal(60, result.Segments[0].DurationMinutes);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [Trait("Stage", "M2CrewBoundary")]
    public void Fourth_reduced_daily_rest_of_either_card_forces_regular_11h_rest(
        int exhaustedSlot)
    {
        const long start = 1_000;
        var exhausted = State(dailyDeadline: 660, reducedRests: 3);
        var result = _engine.Plan(Request(
            remainingDriveMinutes: 1,
            startGameMinute: start,
            slot1: exhaustedSlot == 1
                ? exhausted
                : State(dailyDeadline: 660),
            slot2: exhaustedSlot == 2
                ? exhausted
                : State(dailyDeadline: 660)));

        var rest = result.Segments[0];
        Assert.Equal(JourneyPlanSegmentReason.DailyRestDeadline, rest.Reason);
        Assert.Equal(660, rest.DurationMinutes);
        Assert.Equal(start + 660, rest.EndGameMinute);
        var exhaustedSummary = exhaustedSlot == 1 ? result.Slot1 : result.Slot2;
        Assert.Equal(3, exhaustedSummary.ReducedDailyRestsUsed);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [Trait("Stage", "M2CrewBoundary")]
    public void Too_late_snapshot_of_either_card_returns_no_legal_continuation(
        int lateSlot)
    {
        var late = State(dailyDeadline: 500);
        var result = _engine.Plan(Request(
            remainingDriveMinutes: 1,
            slot1: lateSlot == 1 ? late : State(),
            slot2: lateSlot == 2 ? late : State()));

        Assert.Equal(JourneyPlanStatus.NoLegalContinuation, result.Status);
        Assert.Empty(result.Segments);
        Assert.Null(result.EarliestArrivalGameMinute);
    }

    [Fact]
    [Trait("Stage", "M2CrewBoundary")]
    public void Reduced_daily_rest_and_daily_extensions_are_counted_for_each_card()
    {
        var result = _engine.Plan(Request(
            remainingDriveMinutes: 1,
            slot1: State(daily: 600),
            slot2: State(daily: 600)));

        var rest = result.Segments[0];
        Assert.Equal(JourneyPlanSegmentReason.DailyDrivingLimit, rest.Reason);
        Assert.Equal(540, rest.DurationMinutes);
        Assert.Equal(1, result.Slot1.DailyDrivingExtensionsUsed);
        Assert.Equal(1, result.Slot2.DailyDrivingExtensionsUsed);
        Assert.Equal(1, result.Slot1.ReducedDailyRestsUsed);
        Assert.Equal(1, result.Slot2.ReducedDailyRestsUsed);
    }

    [Fact]
    [Trait("Stage", "M2CrewBoundary")]
    public void Biweekly_capacity_uses_its_own_calendar_wait_reason()
    {
        var result = _engine.Plan(Request(
            remainingDriveMinutes: 1,
            slot1: State(weekly: 3_000, previousWeek: 2_400),
            slot2: State(weekly: 3_000, previousWeek: 2_400)));

        Assert.Equal(
            JourneyPlanSegmentReason.WaitForBiweeklyCapacity,
            result.Segments[0].Reason);
        Assert.Null(result.Segments[0].DrivingSlot);
        Assert.Equal(3_000, result.Slot1.PreviousWeekDrivingMinutes);
    }

    [Fact]
    [Trait("Stage", "M2CrewBoundary")]
    public void Short_calendar_wait_resets_week_but_not_daily_driving()
    {
        var result = _engine.Plan(Request(
            remainingDriveMinutes: 1,
            startGameMinute: 10_000,
            slot1: State(daily: 100, weekly: 3_360),
            slot2: State(daily: 200, weekly: 3_360)));

        Assert.Equal(80, result.Segments[0].DurationMinutes);
        Assert.Equal(101, result.Slot1.DailyDrivingMinutes);
        Assert.Equal(200, result.Slot2.DailyDrivingMinutes);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [Trait("Stage", "M2CrewBoundary")]
    public void Weekly_rest_deadline_of_either_card_stops_the_vehicle(
        int urgentSlot)
    {
        var urgent = State(weeklyDeadline: 60);
        var result = _engine.Plan(Request(
            remainingDriveMinutes: 120,
            slot1: urgentSlot == 1 ? urgent : State(),
            slot2: urgentSlot == 2 ? urgent : State()));

        Assert.Equal(60, result.Segments[0].DurationMinutes);
        var rest = result.Segments[1];
        Assert.Equal(
            JourneyPlanSegmentReason.WeeklyRestRequirement,
            rest.Reason);
        Assert.Equal(2_700, rest.DurationMinutes);
        Assert.Null(rest.DrivingSlot);
    }

    [Fact]
    [Trait("Stage", "M2CrewBoundary")]
    public void Weekly_rest_crossing_week_boundary_moves_driving_to_previous_week()
    {
        var result = _engine.Plan(Request(
            remainingDriveMinutes: 1,
            startGameMinute: 10_000,
            slot1: State(weekly: 100, weeklyDeadline: 0),
            slot2: State(weekly: 200, weeklyDeadline: 0)));

        Assert.Equal(
            JourneyPlanSegmentReason.WeeklyRestRequirement,
            result.Segments[0].Reason);
        Assert.Equal(100, result.Slot1.PreviousWeekDrivingMinutes);
        Assert.Equal(1, result.Slot1.WeeklyDrivingMinutes);
        Assert.Equal(200, result.Slot2.PreviousWeekDrivingMinutes);
        Assert.Equal(0, result.Slot2.WeeklyDrivingMinutes);
    }

    [Fact]
    [Trait("Stage", "M2CrewBoundary")]
    public void Calendar_wait_cannot_cross_maximum_elapsed_limit()
    {
        var result = _engine.Plan(Request(
            remainingDriveMinutes: 1,
            slot1: State(weekly: 3_360),
            slot2: State(weekly: 3_360),
            limits: new JourneyPlanningLimits(
                MaximumSegments: 10,
                MaximumElapsedMinutes: 100,
                MaximumVisitedStates: 100)));

        Assert.Equal(JourneyPlanStatus.CalculationLimitReached, result.Status);
        Assert.Empty(result.Segments);
    }

    [Theory]
    [InlineData(1, 100)]
    [InlineData(10, 1)]
    [Trait("Stage", "M2CrewBoundary")]
    public void Segment_and_visited_state_limits_end_calculation_controlled(
        int maximumSegments,
        int maximumVisitedStates)
    {
        var result = _engine.Plan(Request(
            remainingDriveMinutes: 540,
            limits: new JourneyPlanningLimits(
                maximumSegments,
                MaximumElapsedMinutes: 10_000,
                maximumVisitedStates)));

        Assert.Equal(JourneyPlanStatus.CalculationLimitReached, result.Status);
        Assert.Single(result.Segments);
        Assert.Null(result.EarliestArrivalGameMinute);
    }

    [Fact]
    [Trait("Stage", "M2CrewBoundary")]
    public void Identical_request_is_deterministic_and_does_not_mutate_snapshot()
    {
        var slot1 = State(continuous: 10, daily: 20, weekly: 30);
        var slot2 = State(continuous: 40, daily: 50, weekly: 60);
        var request = Request(
            remainingDriveMinutes: 700,
            slot1: slot1,
            slot2: slot2);

        var first = _engine.Plan(request);
        var second = _engine.Plan(request);

        Assert.Equal(first.Status, second.Status);
        Assert.Equal(first.EarliestArrivalGameMinute, second.EarliestArrivalGameMinute);
        Assert.Equal(first.Segments, second.Segments);
        Assert.Equal(first.Slot1, second.Slot1);
        Assert.Equal(first.Slot2, second.Slot2);
        Assert.Equal(10, slot1.ContinuousDrivingMinutes);
        Assert.Equal(20, slot1.DailyDrivingMinutes);
        Assert.Equal(30, slot1.WeeklyDrivingMinutes);
        Assert.Equal(40, slot2.ContinuousDrivingMinutes);
        Assert.Equal(50, slot2.DailyDrivingMinutes);
        Assert.Equal(60, slot2.WeeklyDrivingMinutes);
    }

    [Fact]
    [Trait("Stage", "M2CrewBoundary")]
    public void Projected_driver_states_match_regulation_engine()
    {
        const long start = 1_000;
        var result = _engine.Plan(Request(
            remainingDriveMinutes: 540,
            startGameMinute: start));
        var end = Assert.IsType<long>(result.EarliestArrivalGameMinute);

        var slot1History = ProjectHistory(result.Segments, slot: 1, "CARD-S1");
        var slot2History = ProjectHistory(result.Segments, slot: 2, "CARD-S2");
        var regulation = new RegulationEngine();
        var options = new RegulationOptions { MultiManning = true };
        var slot1 = regulation.Evaluate(
            new RuleContext(new GameTime(end), slot1History),
            options).State;
        var slot2 = regulation.Evaluate(
            new RuleContext(new GameTime(end), slot2History),
            options).State;

        AssertStateParity(result.Slot1, slot1);
        AssertStateParity(result.Slot2, slot2);
    }

    [Fact]
    [Trait("Stage", "M2CrewBoundary")]
    public void Daily_rest_deadline_projection_matches_regulation_engine()
    {
        const long start = 2_000;
        var slot1Prior = PriorDailyWindow("CARD-S1", start);
        var slot2Prior = PriorDailyWindow("CARD-S2", start);
        var request = RequestFromHistory(
            start,
            remainingDriveMinutes: 120,
            slot1Prior,
            slot2Prior);

        Assert.Equal(
            600,
            request.Snapshot.Slot1.Evaluation.State.MinutesUntilDailyRestDeadline);
        var result = _engine.Plan(request);
        var end = Assert.IsType<long>(result.EarliestArrivalGameMinute);
        var slot1History = slot1Prior
            .Concat(ProjectHistory(result.Segments, 1, "CARD-S1"))
            .ToList();
        var slot2History = slot2Prior
            .Concat(ProjectHistory(result.Segments, 2, "CARD-S2"))
            .ToList();
        var regulation = new RegulationEngine();
        var options = new RegulationOptions { MultiManning = true };

        AssertStateParity(
            result.Slot1,
            regulation.Evaluate(
                new RuleContext(new GameTime(end), slot1History),
                options).State);
        AssertStateParity(
            result.Slot2,
            regulation.Evaluate(
                new RuleContext(new GameTime(end), slot2History),
                options).State);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [Trait("Stage", "M2CrewBoundary")]
    public void Unresolved_card_gap_on_either_card_blocks_crew_plan(int slot)
    {
        var request = WithGap(
            Request(remainingDriveMinutes: 1),
            slot,
            ActivityGapReason.CardRemoved);

        var result = _engine.Plan(request);

        Assert.Equal(JourneyPlanStatus.BlockedByGap, result.Status);
        Assert.Empty(result.Segments);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [Trait("Stage", "M2CrewBoundary")]
    public void Forward_time_gap_on_either_card_reduces_confidence(int slot)
    {
        var request = WithGap(
            Request(remainingDriveMinutes: 1),
            slot,
            ActivityGapReason.ForwardTimeJump);

        var result = _engine.Plan(request);

        Assert.Equal(
            JourneyPlanConfidence.BasedOnIncompleteHistory,
            result.Confidence);
        Assert.Contains(result.Warnings,
            warning => warning.Code == JourneyPlanWarningCode.IncompleteHistory);
    }

    [Fact]
    [Trait("Stage", "M2CrewBoundary")]
    public void Missing_telemetry_uses_last_saved_state_confidence()
    {
        var request = Request(remainingDriveMinutes: 1);
        request = request with
        {
            Snapshot = request.Snapshot with { TelemetryAvailable = false }
        };

        var result = _engine.Plan(request);

        Assert.Equal(
            JourneyPlanConfidence.BasedOnLastSavedState,
            result.Confidence);
        Assert.Contains(result.Warnings,
            warning => warning.Code == JourneyPlanWarningCode.LastSavedState);
    }

    private static void AssertStateParity(
        CrewDriverPlanSummary planned,
        RegulationState evaluated)
    {
        Assert.Equal(evaluated.ContinuousDrivingMinutes, planned.ContinuousDrivingMinutes);
        Assert.Equal(evaluated.DailyDrivingMinutes, planned.DailyDrivingMinutes);
        Assert.Equal(evaluated.WeeklyDrivingMinutes, planned.WeeklyDrivingMinutes);
        Assert.Equal(
            evaluated.PreviousWeekDrivingMinutes,
            planned.PreviousWeekDrivingMinutes);
        Assert.Equal(
            evaluated.CurrentContinuousBreakMinutes,
            planned.CurrentContinuousBreakMinutes);
        Assert.Equal(
            evaluated.MinutesUntilDailyRestDeadline,
            planned.MinutesUntilDailyRestDeadline);
        Assert.Equal(
            evaluated.MinutesUntilWeeklyRestDeadline,
            planned.MinutesUntilWeeklyRestDeadline);
        Assert.Equal(
            evaluated.DailyExtensionsUsedThisWeek,
            planned.DailyDrivingExtensionsUsed);
        Assert.Equal(
            evaluated.ReducedDailyRestsSinceWeeklyRest,
            planned.ReducedDailyRestsUsed);
    }

    private static IReadOnlyList<ActivityRecord> ProjectHistory(
        IReadOnlyList<CrewJourneyPlanSegment> segments,
        int slot,
        string cardId)
    {
        var result = new List<ActivityRecord>();
        foreach (var segment in segments)
        {
            var activity = slot == 1
                ? segment.Slot1Activity
                : segment.Slot2Activity;
            var qualified = slot == 1
                ? segment.Slot1BreakQualifiedInMotion
                : segment.Slot2BreakQualifiedInMotion;
            if (qualified)
            {
                AddRecord(
                    result,
                    cardId,
                    segment.StartGameMinute,
                    segment.StartGameMinute + 45,
                    DriverActivity.BreakOrRest,
                    SpecialCondition.CrewBreakInMotion);
                if (segment.DurationMinutes > 45)
                    AddRecord(
                        result,
                        cardId,
                        segment.StartGameMinute + 45,
                        segment.EndGameMinute,
                        DriverActivity.Availability);
                continue;
            }

            AddRecord(
                result,
                cardId,
                segment.StartGameMinute,
                segment.EndGameMinute,
                activity);
        }

        return result;
    }

    private static void AddRecord(
        ICollection<ActivityRecord> records,
        string cardId,
        long start,
        long end,
        DriverActivity activity,
        SpecialCondition condition = SpecialCondition.None)
    {
        records.Add(new ActivityRecord
        {
            Id = Guid.NewGuid(),
            DriverCardId = cardId,
            Activity = activity,
            Start = new GameTime(start),
            EndExclusive = new GameTime(end),
            RecordedAtUtc = DateTimeOffset.UnixEpoch.AddMinutes(start),
            Source = ActivitySource.Telemetry,
            Condition = condition
        });
    }

    private static CrewJourneyPlanRequest WithGap(
        CrewJourneyPlanRequest request,
        int slot,
        ActivityGapReason reason)
    {
        var gap = new ActivityGap
        {
            Id = Guid.NewGuid(),
            DriverCardId = $"CARD-S{slot}",
            Slot = slot,
            SessionIndex = 0,
            Start = new GameTime(request.Snapshot.StartGameMinute - 10),
            EndExclusive = new GameTime(request.Snapshot.StartGameMinute),
            Reason = reason,
            State = ActivityGapState.Unresolved
        };
        return request with
        {
            Snapshot = slot == 1
                ? request.Snapshot with
                {
                    Slot1 = request.Snapshot.Slot1 with { Gaps = [gap] }
                }
                : request.Snapshot with
                {
                    Slot2 = request.Snapshot.Slot2 with { Gaps = [gap] }
                }
        };
    }

    private static CrewJourneyPlanRequest Request(
        int remainingDriveMinutes,
        int initialDrivingSlot = 1,
        long startGameMinute = 1_000,
        RegulationState? slot1 = null,
        RegulationState? slot2 = null,
        JourneyPlanningLimits? limits = null)
    {
        var snapshot = new CrewJourneyPlanningSnapshot(
            startGameMinute,
            WorldGeneration: 7,
            WeekEpochOffsetDays: 0,
            MultiManningActive: true,
            TelemetryAvailable: true,
            Driver(1, slot1 ?? State()),
            Driver(2, slot2 ?? State()));
        return new CrewJourneyPlanRequest(
            JourneyPlanningMode.MultiManningCrew,
            snapshot,
            initialDrivingSlot,
            remainingDriveMinutes,
            DeliveryWindowMinutes: 50_000,
            OperationalBufferMinutes: 0,
            JourneyOperationalBufferPolicy.OtherWorkAfterArrival,
            limits ?? JourneyPlanningLimits.Default);
    }

    private static CrewJourneyPlanRequest RequestFromHistory(
        long startGameMinute,
        int remainingDriveMinutes,
        IReadOnlyList<ActivityRecord> slot1History,
        IReadOnlyList<ActivityRecord> slot2History)
    {
        var regulation = new RegulationEngine();
        var options = new RegulationOptions { MultiManning = true };
        var slot1Evaluation = regulation.Evaluate(
            new RuleContext(new GameTime(startGameMinute), slot1History),
            options);
        var slot2Evaluation = regulation.Evaluate(
            new RuleContext(new GameTime(startGameMinute), slot2History),
            options);
        var snapshot = new CrewJourneyPlanningSnapshot(
            startGameMinute,
            WorldGeneration: 7,
            WeekEpochOffsetDays: 0,
            MultiManningActive: true,
            TelemetryAvailable: true,
            new CrewDriverPlanningSnapshot(
                1,
                "CARD-S1",
                Guid.Parse("44444444-4444-4444-4444-444444444441"),
                HistoryHighWaterMark: startGameMinute,
                slot1Evaluation,
                slot1History,
                Gaps: []),
            new CrewDriverPlanningSnapshot(
                2,
                "CARD-S2",
                Guid.Parse("44444444-4444-4444-4444-444444444442"),
                HistoryHighWaterMark: startGameMinute,
                slot2Evaluation,
                slot2History,
                Gaps: []));
        return new CrewJourneyPlanRequest(
            JourneyPlanningMode.MultiManningCrew,
            snapshot,
            InitialDrivingSlot: 1,
            remainingDriveMinutes,
            DeliveryWindowMinutes: 50_000,
            OperationalBufferMinutes: 0,
            JourneyOperationalBufferPolicy.OtherWorkAfterArrival,
            JourneyPlanningLimits.Default);
    }

    private static IReadOnlyList<ActivityRecord> PriorDailyWindow(
        string cardId,
        long startGameMinute)
    {
        var records = new List<ActivityRecord>();
        AddRecord(
            records,
            cardId,
            startGameMinute - 1_860,
            startGameMinute - 1_200,
            DriverActivity.BreakOrRest);
        AddRecord(
            records,
            cardId,
            startGameMinute - 1_200,
            startGameMinute,
            DriverActivity.Availability);
        return records;
    }

    private static CrewDriverPlanningSnapshot Driver(
        int slot,
        RegulationState state) => new(
        slot,
        $"CARD-S{slot}",
        Guid.Parse(slot == 1
            ? "33333333-3333-3333-3333-333333333331"
            : "33333333-3333-3333-3333-333333333332"),
        HistoryHighWaterMark: 100 + slot,
        new RegulationEvaluation(state, [], []),
        History: [],
        Gaps: []);

    private static RegulationState State(
        long continuous = 0,
        long currentBreak = 0,
        long daily = 0,
        long weekly = 0,
        long previousWeek = 0,
        int extensions = 0,
        int reducedRests = 0,
        long dailyDeadline = 1_800,
        long weeklyDeadline = 8_640) => new()
    {
        ContinuousDrivingMinutes = continuous,
        CurrentContinuousBreakMinutes = currentBreak,
        DailyDrivingMinutes = daily,
        WeeklyDrivingMinutes = weekly,
        PreviousWeekDrivingMinutes = previousWeek,
        DailyExtensionsUsedThisWeek = extensions,
        ReducedDailyRestsSinceWeeklyRest = reducedRests,
        MinutesUntilDailyRestDeadline = dailyDeadline,
        MinutesUntilWeeklyRestDeadline = weeklyDeadline
    };
}

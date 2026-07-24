using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.RuleEngine.JourneyPlanning;

namespace ETS2Tachograph.RuleEngine.Tests;

public sealed class CrewJourneyPlanningP0Tests
{
    private readonly CrewJourneyPlanningEngine _engine = new();

    [Fact(DisplayName = "JP-CREW-P0-01: S2 takes over after a qualified break in motion")]
    [Trait("Stage", "M2Crew")]
    public void JP_CREW_P0_01()
    {
        var result = _engine.Plan(Request(remainingDriveMinutes: 540));

        Assert.Collection(
            result.Segments,
            first =>
            {
                Assert.Equal(1, first.DrivingSlot);
                Assert.Equal(270, first.DurationMinutes);
                Assert.True(first.Slot2BreakQualifiedInMotion);
            },
            second =>
            {
                Assert.Equal(2, second.DrivingSlot);
                Assert.Equal(
                    result.Segments[0].EndGameMinute,
                    second.StartGameMinute);
            });
        Assert.DoesNotContain(result.Segments, segment => segment.DrivingSlot is null);
    }

    [Fact(DisplayName = "JP-CREW-P0-02: an incomplete moving break cannot reset S2")]
    [Trait("Stage", "M2Crew")]
    public void JP_CREW_P0_02()
    {
        var result = _engine.Plan(Request(
            remainingDriveMinutes: 1,
            slot1: State(continuous: 270),
            slot2: State(continuous: 270, currentBreak: 44)));

        Assert.Collection(
            result.Segments,
            pause =>
            {
                Assert.Null(pause.DrivingSlot);
                Assert.Equal(1, pause.DurationMinutes);
                Assert.Equal(JourneyPlanSegmentReason.ContinuousDrivingBreak, pause.Reason);
            },
            drive => Assert.Equal(2, drive.DrivingSlot));
    }

    [Fact(DisplayName = "JP-CREW-P0-03: moving break resets continuous but not daily driving")]
    [Trait("Stage", "M2Crew")]
    public void JP_CREW_P0_03()
    {
        var result = _engine.Plan(Request(
            remainingDriveMinutes: 271,
            slot2: State(continuous: 270, daily: 120)));

        Assert.True(result.Segments[0].Slot2BreakQualifiedInMotion);
        Assert.Equal(2, result.Segments[1].DrivingSlot);
        Assert.Equal(1, result.Slot2.ContinuousDrivingMinutes);
        Assert.Equal(121, result.Slot2.DailyDrivingMinutes);
    }

    [Fact(DisplayName = "JP-CREW-P0-04: driver changes beat a single-card arrival")]
    [Trait("Stage", "M2Crew")]
    public void JP_CREW_P0_04()
    {
        var crew = _engine.Plan(Request(remainingDriveMinutes: 540));
        var single = new JourneyPlanningEngine().Plan(
            JourneyPlanningTestData.Request(
                JourneyPlanningTestData.Snapshot(),
                remainingDriveMinutes: 540));

        Assert.True(crew.EarliestArrivalGameMinute < single.EarliestArrivalGameMinute);
    }

    [Fact(DisplayName = "JP-CREW-P0-05: 30 h window requires confirmed active crew")]
    [Trait("Stage", "M2Crew")]
    public void JP_CREW_P0_05()
    {
        var result = _engine.Plan(Request(
            remainingDriveMinutes: 1,
            multiManningActive: false));

        Assert.Equal(JourneyPlanStatus.UnsupportedScenario, result.Status);
        Assert.Empty(result.Segments);
        Assert.Contains(result.Warnings,
            warning => warning.Code == JourneyPlanWarningCode.MultiManningPlanningUnsupported);
    }

    [Fact(DisplayName = "JP-CREW-P0-06: a segment never assigns Driving to both slots")]
    [Trait("Stage", "M2Crew")]
    public void JP_CREW_P0_06()
    {
        var result = _engine.Plan(Request(remainingDriveMinutes: 900));

        Assert.All(result.Segments, segment =>
        {
            var drivingActivities =
                (segment.Slot1Activity == DriverActivity.Driving ? 1 : 0) +
                (segment.Slot2Activity == DriverActivity.Driving ? 1 : 0);
            Assert.InRange(drivingActivities, 0, 1);
            Assert.Equal(segment.DrivingSlot is null ? 0 : 1, drivingActivities);
        });
    }

    private static CrewJourneyPlanRequest Request(
        int remainingDriveMinutes,
        RegulationState? slot1 = null,
        RegulationState? slot2 = null,
        bool multiManningActive = true)
    {
        var snapshot = new CrewJourneyPlanningSnapshot(
            StartGameMinute: 10_000,
            WorldGeneration: 7,
            WeekEpochOffsetDays: 0,
            MultiManningActive: multiManningActive,
            TelemetryAvailable: true,
            Slot1: Driver(1, slot1 ?? State()),
            Slot2: Driver(2, slot2 ?? State()));
        return new CrewJourneyPlanRequest(
            JourneyPlanningMode.MultiManningCrew,
            snapshot,
            InitialDrivingSlot: 1,
            remainingDriveMinutes,
            DeliveryWindowMinutes: 10_000,
            OperationalBufferMinutes: 0,
            JourneyOperationalBufferPolicy.OtherWorkAfterArrival,
            JourneyPlanningLimits.Default);
    }

    private static CrewDriverPlanningSnapshot Driver(
        int slot,
        RegulationState state) => new(
        slot,
        $"CARD-S{slot}",
        Guid.Parse(slot == 1
            ? "11111111-2222-2222-2222-222222222222"
            : "22222222-2222-2222-2222-222222222222"),
        HistoryHighWaterMark: 42 + slot,
        new RegulationEvaluation(state, [], []),
        History: [],
        Gaps: []);

    private static RegulationState State(
        long continuous = 0,
        long currentBreak = 0,
        long daily = 0,
        long weekly = 0,
        long previousWeek = 0,
        long dailyDeadline = 1_800) => new()
    {
        ContinuousDrivingMinutes = continuous,
        CurrentContinuousBreakMinutes = currentBreak,
        DailyDrivingMinutes = daily,
        WeeklyDrivingMinutes = weekly,
        PreviousWeekDrivingMinutes = previousWeek,
        MinutesUntilDailyRestDeadline = dailyDeadline,
        MinutesUntilWeeklyRestDeadline = 8_640
    };
}

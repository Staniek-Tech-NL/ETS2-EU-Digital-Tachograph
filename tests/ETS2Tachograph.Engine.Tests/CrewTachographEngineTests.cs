using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Telemetry;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Engine.Tests;

public sealed class CrewTachographEngineTests
{
    private static readonly DateTimeOffset Epoch = new(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Slot_one_drives_while_slot_two_records_availability()
    {
        var crew = Crew();

        crew.ProcessFrame(Frame(0, 30));
        crew.ProcessFrame(Frame(1, 30));
        var snapshot = crew.ProcessFrame(Frame(2, 30));

        Assert.True(snapshot.MultiManning);
        Assert.Equal(DriverActivity.Driving, snapshot.Driver!.LastClosedRecord!.Activity);
        Assert.Equal(DriverActivity.Availability, snapshot.CoDriver!.LastClosedRecord!.Activity);
        Assert.Equal(1_800, snapshot.Driver.DailyRestWindowMinutes);
        Assert.Equal(1_800, snapshot.CoDriver.DailyRestWindowMinutes);
    }

    [Fact]
    public void Moving_co_driver_break_is_exactly_45_minutes_then_returns_to_availability()
    {
        var crew = Crew();
        crew.ProcessFrame(Frame(0, 30));
        crew.StartCoDriverMovingBreak();

        for (var minute = 1; minute <= 47; minute++)
            crew.ProcessFrame(Frame(minute, 30));

        Assert.False(crew.Current.CoDriverMovingBreakActive);
        Assert.Equal(DriverActivity.Availability, crew.Current.CoDriver!.ProvisionalActivity);
        Assert.Equal(
            45,
            crew.GetEngine("CARD-B")!.History.CurrentTimeline.Records
                .Where(x => x.Activity == DriverActivity.BreakOrRest)
                .Sum(x => x.DurationMinutes));
    }

    [Fact]
    public void Co_driver_break_selected_while_stopped_continues_when_slot_one_starts_driving()
    {
        var crew = Crew();
        crew.ProcessFrame(Frame(0, 0));
        crew.SetManualActivity(TachographSlot.CoDriver, DriverActivity.BreakOrRest);
        crew.ProcessFrame(Frame(1, 0));

        var moving = crew.ProcessFrame(Frame(2, 30));

        Assert.True(moving.CoDriverMovingBreakActive);
        Assert.Equal(DriverActivity.BreakOrRest, moving.CoDriver!.ProvisionalActivity);
        Assert.Equal(2, moving.CoDriverMovingBreakElapsedMinutes);
    }

    [Fact]
    public void Automatically_continued_co_driver_break_ends_after_45_minutes()
    {
        var crew = Crew();
        crew.ProcessFrame(Frame(0, 0));
        crew.SetManualActivity(TachographSlot.CoDriver, DriverActivity.BreakOrRest);

        for (var minute = 1; minute <= 47; minute++)
            crew.ProcessFrame(Frame(minute, 30));

        Assert.False(crew.Current.CoDriverMovingBreakActive);
        Assert.True(crew.Current.CoDriverMovingBreakCompleted);
        Assert.Equal(DriverActivity.Availability, crew.Current.CoDriver!.ProvisionalActivity);
        Assert.Equal(
            45,
            crew.GetEngine("CARD-B")!.History.CurrentTimeline.Records
                .Where(x => x.Activity == DriverActivity.BreakOrRest)
                .Sum(x => x.DurationMinutes));
    }

    [Fact]
    public void Full_moving_break_resets_co_driver_continuous_driving_and_cannot_be_restarted()
    {
        var crew = new CrewTachographEngine();
        crew.RegisterCard("CARD-A");
        crew.RegisterCard("CARD-B");
        crew.InsertCard(TachographSlot.Driver, "CARD-B");
        crew.InsertCard(TachographSlot.CoDriver, "CARD-A");
        for (var minute = 0; minute <= 61; minute++)
            crew.ProcessFrame(Frame(minute, 30));

        crew.ProcessFrame(Frame(62, 0));
        crew.EjectCard(TachographSlot.Driver, Epoch.AddMinutes(62));
        crew.EjectCard(TachographSlot.CoDriver, Epoch.AddMinutes(62));
        crew.InsertCard(TachographSlot.Driver, "CARD-A");
        crew.InsertCard(TachographSlot.CoDriver, "CARD-B");
        crew.ProcessFrame(Frame(63, 30));
        crew.StartCoDriverMovingBreak();

        for (var minute = 64; minute <= 73; minute++)
            crew.ProcessFrame(Frame(minute, 30));
        crew.StartCoDriverMovingBreak();
        for (var minute = 74; minute <= 110; minute++)
            crew.ProcessFrame(Frame(minute, 30));

        Assert.False(crew.Current.CoDriverMovingBreakActive);
        Assert.True(crew.Current.CoDriverMovingBreakCompleted);
        Assert.Equal(0, crew.Current.CoDriver!.Regulation!.State.ContinuousDrivingMinutes);
        Assert.Equal(270, crew.Current.CoDriver.Regulation.State.MinutesUntilBreak);
        Assert.Equal(
            45,
            crew.GetEngine("CARD-B")!.History.CurrentTimeline.Records
                .Where(x => x.Activity == DriverActivity.BreakOrRest)
                .Sum(x => x.DurationMinutes));
    }

    [Fact]
    public void Game_time_jump_does_not_credit_full_break_before_45_minutes()
    {
        var engine = new TachographEngine("CARD-A");
        for (var minute = 0; minute <= 60; minute++)
            engine.ProcessFrame(Frame(minute, 30));
        engine.SetManualActivity(DriverActivity.BreakOrRest);
        engine.ProcessFrame(Frame(61, 0));

        var afterFortyOne = engine.ProcessFrame(Frame(102, 0));

        Assert.True(afterFortyOne.GameTimeJumpDetected);
        Assert.True(afterFortyOne.Regulation!.State.ContinuousDrivingMinutes > 0);
        var projectedBreak = afterFortyOne.CurrentSessionRecords
            .Where(x => x.Activity == DriverActivity.BreakOrRest)
            .Sum(x => x.DurationMinutes) +
            (engine.History.ProvisionalRecord?.Activity == DriverActivity.BreakOrRest ? 1 : 0);
        Assert.Equal(41, projectedBreak);

        var afterFortyFive = engine.ProcessFrame(Frame(106, 0));
        Assert.Equal(0, afterFortyFive.Regulation!.State.ContinuousDrivingMinutes);
    }

    [Fact]
    public void Large_jump_during_moving_co_driver_break_creates_a_gap_in_slot_two()
    {
        var crew = Crew();
        crew.ProcessFrame(Frame(0, 30));
        crew.StartCoDriverMovingBreak();
        crew.ProcessFrame(Frame(1, 30));

        var jumped = crew.ProcessFrame(Frame(20, 30));

        Assert.True(jumped.CoDriver!.GameTimeJumpDetected);
        Assert.True(jumped.CoDriverMovingBreakActive);
        var gap = Assert.Single(jumped.CoDriver.CreatedGaps);
        Assert.Equal(2, gap.Slot);
        Assert.Equal(ActivityGapReason.ForwardTimeJump, gap.Reason);
        Assert.DoesNotContain(
            crew.GetEngine("CARD-B")!.History.CurrentTimeline.Records,
            record => record.Source == ActivitySource.Reconstructed);
        Assert.DoesNotContain(
            jumped.CoDriver.CompletedRecords,
            record => record.Source == ActivitySource.Reconstructed);
    }

    [Fact]
    public void Cargo_operation_jump_preserves_each_slots_selected_activity()
    {
        var crew = Crew();
        crew.ProcessFrame(Frame(100, 0, cargoOperationGeneration: 7));

        var snapshot = crew.ProcessFrame(Frame(120, 0, cargoOperationGeneration: 8));
        crew.ProcessFrame(Frame(121, 0, cargoOperationGeneration: 8));

        Assert.Empty(snapshot.Driver!.CreatedGaps);
        Assert.Empty(snapshot.CoDriver!.CreatedGaps);
        Assert.Equal(
            19,
            crew.GetEngine("CARD-A")!.History.CurrentTimeline.Records.Count(record =>
                record.Start >= new GameTime(101) &&
                record.EndExclusive <= new GameTime(120) &&
                record.Activity == DriverActivity.OtherWork));
        Assert.Equal(
            19,
            crew.GetEngine("CARD-B")!.History.CurrentTimeline.Records.Count(record =>
                record.Start >= new GameTime(101) &&
                record.EndExclusive <= new GameTime(120) &&
                record.Activity == DriverActivity.Availability));
    }

    [Fact]
    public void Cards_are_reassigned_by_ejecting_and_inserting_into_opposite_slots()
    {
        var crew = Crew();
        crew.ProcessFrame(Frame(0, 30));
        crew.ProcessFrame(Frame(1, 30));
        crew.ProcessFrame(Frame(2, 0));

        crew.EjectCard(TachographSlot.Driver, Epoch.AddMinutes(2));
        crew.EjectCard(TachographSlot.CoDriver, Epoch.AddMinutes(2));
        crew.InsertCard(TachographSlot.Driver, "CARD-B");
        crew.InsertCard(TachographSlot.CoDriver, "CARD-A");
        crew.ProcessFrame(Frame(3, 30));
        crew.ProcessFrame(Frame(4, 30));
        var snapshot = crew.ProcessFrame(Frame(5, 30));

        Assert.Equal("CARD-B", snapshot.DriverCardId);
        Assert.Equal("CARD-A", snapshot.CoDriverCardId);
        Assert.Equal(DriverActivity.Driving, snapshot.Driver!.LastClosedRecord!.Activity);
        Assert.Equal(DriverActivity.Availability, snapshot.CoDriver!.LastClosedRecord!.Activity);
        Assert.All(crew.GetEngine("CARD-A")!.History.Sessions.SelectMany(x => x.Records),
            record => Assert.Equal("CARD-A", record.DriverCardId));
        Assert.All(crew.GetEngine("CARD-B")!.History.Sessions.SelectMany(x => x.Records),
            record => Assert.Equal("CARD-B", record.DriverCardId));
    }

    [Fact]
    public void Cards_cannot_be_removed_while_vehicle_is_moving()
    {
        var crew = Crew();
        crew.ProcessFrame(Frame(0, 30));

        Assert.Throws<InvalidOperationException>(() =>
            crew.EjectCard(TachographSlot.Driver, Epoch));
    }

    [Fact]
    public void Same_card_cannot_be_inserted_into_both_slots()
    {
        var crew = new CrewTachographEngine();
        crew.RegisterCard("CARD-A");
        crew.InsertCard(TachographSlot.Driver, "CARD-A");

        Assert.Throws<InvalidOperationException>(() =>
            crew.InsertCard(TachographSlot.CoDriver, "CARD-A"));
    }

    private static CrewTachographEngine Crew()
    {
        var crew = new CrewTachographEngine();
        crew.RegisterCard("CARD-A");
        crew.RegisterCard("CARD-B");
        crew.InsertCard(TachographSlot.Driver, "CARD-A");
        crew.InsertCard(TachographSlot.CoDriver, "CARD-B");
        return crew;
    }

    private static TelemetryFrame Frame(
        long minute,
        double speed,
        uint cargoOperationGeneration = 0) =>
        new(
            new GameTime(minute),
            Epoch.AddMinutes(minute),
            speed,
            GamePaused: false,
            CargoOperationGeneration: cargoOperationGeneration);
}

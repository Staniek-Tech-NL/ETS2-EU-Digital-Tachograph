using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Telemetry;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Engine.Tests;

public sealed class TachographEngineTests
{
    private static readonly DateTimeOffset Epoch = new(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Frames_flow_through_history_and_rule_engine()
    {
        var engine = new TachographEngine("PL-TEST");

        engine.ProcessFrame(Frame(0, 0, 20));
        engine.ProcessFrame(Frame(1, 1, 20));
        var snapshot = engine.ProcessFrame(Frame(2, 2, 20));

        Assert.NotNull(snapshot.LastClosedRecord);
        Assert.Equal(DriverActivity.Driving, snapshot.LastClosedRecord.Activity);
        Assert.Equal(2, snapshot.Regulation!.State.ContinuousDrivingMinutes);
    }

    [Fact]
    public void Out_mode_overrides_automatic_driving_and_does_not_increment_driving()
    {
        var engine = new TachographEngine("PL-TEST");
        engine.SetOutMode(true);

        engine.ProcessFrame(Frame(0, 0, 80));
        engine.ProcessFrame(Frame(1, 1, 80));
        var snapshot = engine.ProcessFrame(Frame(2, 2, 80));

        Assert.Equal(DriverActivity.OutOfScope, snapshot.LastClosedRecord!.Activity);
        Assert.Equal(0, snapshot.Regulation!.State.ContinuousDrivingMinutes);
    }

    [Fact]
    public void Ferry_mode_marks_created_activity_records()
    {
        var engine = new TachographEngine("PL-TEST");
        engine.SetManualActivity(DriverActivity.BreakOrRest);
        engine.SetFerryMode(true);

        engine.ProcessFrame(Frame(0, 0, 0));
        engine.ProcessFrame(Frame(1, 1, 0));
        var snapshot = engine.ProcessFrame(Frame(2, 2, 0));

        Assert.Equal(SpecialCondition.FerryCrossing, snapshot.LastClosedRecord!.Condition);
    }

    [Fact]
    public void Out_and_ferry_modes_are_mutually_exclusive()
    {
        var engine = new TachographEngine("PL-TEST");

        engine.SetOutMode(true);
        engine.SetFerryMode(true);

        Assert.False(engine.Current.OutModeEnabled);
        Assert.True(engine.Current.FerryModeEnabled);
    }

    [Fact]
    public void Paused_frames_do_not_advance_activity_history()
    {
        var engine = new TachographEngine("PL-TEST");

        engine.ProcessFrame(Frame(0, 0, 20));
        var paused = engine.ProcessFrame(new TelemetryFrame(
            new GameTime(0),
            Epoch.AddSeconds(30),
            20,
            GamePaused: true));

        Assert.Null(paused.LastClosedRecord);
        Assert.Null(paused.ProvisionalActivity);
        Assert.Empty(engine.History.CurrentTimeline.Records);
    }

    [Fact]
    public void Small_forward_jump_is_reconstructed_and_backward_clock_starts_new_session()
    {
        var engine = new TachographEngine("PL-TEST");

        engine.ProcessFrame(Frame(0, 0, 0));
        var jumped = engine.ProcessFrame(Frame(2, 1, 0));
        var movedBack = engine.ProcessFrame(Frame(1, 2, 0));

        Assert.True(jumped.GameTimeJumpDetected);
        Assert.Contains(
            engine.History.Sessions[0].Records,
            record => record.Source == ActivitySource.Reconstructed);
        Assert.True(movedBack.ClockMovedBackward);
        Assert.Equal(1, movedBack.SessionIndex);
        Assert.Equal(2, engine.History.Sessions.Count);
    }

    [Fact]
    public void Snapshot_contains_complete_update_and_multi_manning_30_hour_window()
    {
        var engine = new TachographEngine("PL-TEST");
        engine.SetMultiManning(true);

        engine.ProcessFrame(Frame(0, 0, 0));
        engine.ProcessFrame(Frame(1, 1, 0));
        var snapshot = engine.ProcessFrame(Frame(2, 2, 0));

        Assert.True(snapshot.MultiManningEnabled);
        Assert.Equal(1_800, snapshot.DailyRestWindowMinutes);
        Assert.NotEmpty(snapshot.CompletedRecords);
        Assert.NotEmpty(snapshot.CurrentSessionRecords);
        Assert.Equal(new GameTime(2), snapshot.GameTime);
        Assert.False(snapshot.GamePaused);
    }

    [Fact]
    public void Restored_clock_branches_preserve_earlier_daily_driving_without_counting_overlap()
    {
        var engine = new TachographEngine("PL-TEST");
        engine.RestoreSessions(
        [
            [
                Record(0, DriverActivity.Driving),
                Record(1, DriverActivity.Driving),
                Record(2, DriverActivity.Driving)
            ],
            [
                Record(2, DriverActivity.OtherWork),
                Record(3, DriverActivity.Driving)
            ]
        ]);
        Assert.Equal(3, engine.Current.Regulation!.State.DailyDrivingMinutes);
        engine.SetManualActivity(DriverActivity.OtherWork);

        var snapshot = engine.ProcessFrame(Frame(4, 4, 0));

        Assert.Equal(3, snapshot.Regulation!.State.DailyDrivingMinutes);
        Assert.Equal(4, engine.History.RegulationRecords().Count);
        Assert.Single(engine.History.RegulationRecords(), record => record.Start == new GameTime(2));
    }

    [Fact]
    public void Single_card_daily_driving_resets_after_nine_hour_rest()
    {
        var engine = new TachographEngine("PL-TEST");
        engine.RestoreSessions(
        [[
            Record(0, 233, DriverActivity.Driving),
            Record(233, 773, DriverActivity.BreakOrRest),
            Record(773, 867, DriverActivity.Driving)
        ]]);

        Assert.Equal(94, engine.Current.Regulation!.State.DailyDrivingMinutes);
        Assert.Equal(94, engine.Current.Regulation.State.ContinuousDrivingMinutes);
    }

    [Fact]
    public void Live_backward_jump_keeps_history_before_the_new_branch()
    {
        var engine = new TachographEngine("PL-TEST");
        engine.ProcessFrame(Frame(0, 0, 30));
        engine.ProcessFrame(Frame(1, 1, 30));
        engine.ProcessFrame(Frame(2, 2, 30));
        engine.SetManualActivity(DriverActivity.OtherWork);

        var movedBack = engine.ProcessFrame(Frame(1, 3, 0));
        engine.ProcessFrame(Frame(2, 4, 30));
        var continued = engine.ProcessFrame(Frame(3, 5, 30));

        Assert.True(movedBack.ClockMovedBackward);
        Assert.Equal(1, movedBack.Regulation!.State.DailyDrivingMinutes);
        Assert.Equal(2, continued.Regulation!.State.DailyDrivingMinutes);
    }

    [Fact]
    public void Empty_hot_session_keeps_its_archived_branch_anchor_after_restore()
    {
        var engine = new TachographEngine("PL-TEST");

        engine.RestoreSessions(
        [
            new RestoredActivitySession(0, new GameTime(1_000),
                [Record(1_000, DriverActivity.Driving)]),
            new RestoredActivitySession(1, new GameTime(900), [])
        ]);

        Assert.Empty(engine.History.RegulationRecords());
        Assert.Null(engine.Current.Regulation);
    }

    private static TelemetryFrame Frame(long minute, int second, double speed) =>
        new(new GameTime(minute), Epoch.AddSeconds(second), speed, GamePaused: false);

    private static ActivityRecord Record(long minute, DriverActivity activity) => new()
    {
        Id = Guid.NewGuid(),
        DriverCardId = "PL-TEST",
        Activity = activity,
        Start = new GameTime(minute),
        EndExclusive = new GameTime(minute + 1),
        RecordedAtUtc = Epoch.AddMinutes(minute)
    };

    private static ActivityRecord Record(
        long start,
        long endExclusive,
        DriverActivity activity) => new()
    {
        Id = Guid.NewGuid(),
        DriverCardId = "PL-TEST",
        Activity = activity,
        Start = new GameTime(start),
        EndExclusive = new GameTime(endExclusive),
        RecordedAtUtc = Epoch.AddMinutes(start)
    };
}

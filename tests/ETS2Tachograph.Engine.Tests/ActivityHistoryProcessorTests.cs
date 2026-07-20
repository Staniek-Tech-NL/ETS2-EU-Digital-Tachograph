using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Telemetry;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Engine.Tests;

public sealed class ActivityHistoryProcessorTests
{
    private static readonly DateTimeOffset Epoch = new(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Small_forward_jump_reconstructs_last_activity_even_when_driving()
    {
        var processor = new ActivityHistoryProcessor("PL-TEST");

        processor.Process(Frame(10, 0, 30));
        var update = processor.Process(Frame(12, 1, 30));

        Assert.True(update.GameTimeJumpDetected);
        Assert.Equal(ActivitySource.Reconstructed, processor.ProvisionalRecord!.Source);
        Assert.Equal(DriverActivity.Driving, processor.ProvisionalRecord.Activity);
        Assert.Equal(new GameTime(11), processor.ProvisionalRecord.Start);
    }

    [Fact]
    public void Large_forward_jump_after_driving_creates_a_gap_instead_of_synthetic_driving()
    {
        var processor = new ActivityHistoryProcessor("PL-TEST");

        processor.Process(Frame(10, 0, 30));
        var update = processor.Process(Frame(490, 1, 30));
        processor.Flush(Epoch.AddSeconds(2));

        Assert.True(update.GameTimeJumpDetected);
        var gap = Assert.Single(update.CreatedGaps);
        Assert.Equal("PL-TEST", gap.DriverCardId);
        Assert.Equal(1, gap.Slot);
        Assert.Equal(0, gap.SessionIndex);
        Assert.Equal(new GameTime(11), gap.Start);
        Assert.Equal(new GameTime(490), gap.EndExclusive);
        Assert.Equal(ActivityGapReason.ForwardTimeJump, gap.Reason);
        Assert.Equal(ActivityGapState.Unresolved, gap.State);
        Assert.DoesNotContain(
            processor.CurrentTimeline.Records,
            record => record.Source == ActivitySource.Reconstructed);
        Assert.DoesNotContain(
            processor.CurrentTimeline.Records,
            record => record.Start > new GameTime(10) && record.Start < new GameTime(490));

        var records = processor.CurrentTimeline.Records;
        var gaps = processor.CanonicalGaps();
        var clockSpan = records.Max(record => record.EndExclusive.TotalMinutes) -
                        records.Min(record => record.Start.TotalMinutes);
        Assert.Equal(
            clockSpan,
            records.Sum(record => record.DurationMinutes) +
            gaps.Sum(item => item.DurationMinutes ?? 0));
    }

    [Fact]
    public void Large_forward_jump_reconstructs_rest_only_when_vehicle_was_and_is_stopped()
    {
        var processor = new ActivityHistoryProcessor("PL-TEST");
        processor.ManualActivity = DriverActivity.BreakOrRest;

        processor.Process(Frame(10, 0, 0));
        var update = processor.Process(Frame(490, 1, 0));
        processor.Flush(Epoch.AddSeconds(2));

        Assert.True(update.GameTimeJumpDetected);
        Assert.Equal(
            479,
            processor.CurrentTimeline.Records.Count(record =>
                record.Source == ActivitySource.Reconstructed &&
                record.Activity == DriverActivity.BreakOrRest));
    }

    [Theory]
    [InlineData(DriverActivity.OtherWork)]
    [InlineData(DriverActivity.Availability)]
    [InlineData(DriverActivity.OutOfScope)]
    public void Large_forward_jump_in_non_rest_state_creates_a_gap(DriverActivity activity)
    {
        var processor = new ActivityHistoryProcessor("PL-TEST");
        if (activity == DriverActivity.OutOfScope)
            processor.SetOutMode(true);
        else
            processor.ManualActivity = activity;

        processor.Process(Frame(10, 0, 0));
        var update = processor.Process(Frame(490, 1, 0));
        processor.Flush(Epoch.AddSeconds(2));

        Assert.True(update.GameTimeJumpDetected);
        Assert.DoesNotContain(
            processor.CurrentTimeline.Records,
            record => record.Source == ActivitySource.Reconstructed);
        Assert.DoesNotContain(
            processor.CurrentTimeline.Records,
            record => record.Start > new GameTime(10) && record.Start < new GameTime(490));
    }

    [Theory]
    [InlineData(DriverActivity.OtherWork)]
    [InlineData(DriverActivity.Availability)]
    [InlineData(DriverActivity.BreakOrRest)]
    public void Cargo_loading_or_unloading_jump_preserves_selected_activity(
        DriverActivity selectedActivity)
    {
        var processor = new ActivityHistoryProcessor("PL-TEST");
        processor.ManualActivity = selectedActivity;
        processor.Process(Frame(100, 0, 0, cargoOperationGeneration: 4));

        var update = processor.Process(Frame(120, 1, 0, cargoOperationGeneration: 5));
        processor.Flush(Epoch.AddSeconds(2));

        Assert.True(update.GameTimeJumpDetected);
        Assert.Empty(update.CreatedGaps);
        var operationMinutes = processor.CurrentTimeline.Records.Where(record =>
            record.Start >= new GameTime(101) &&
            record.EndExclusive <= new GameTime(120)).ToList();
        Assert.Equal(19, operationMinutes.Count);
        Assert.All(operationMinutes, record =>
        {
            Assert.Equal(selectedActivity, record.Activity);
            Assert.Equal(ActivitySource.Reconstructed, record.Source);
        });
    }

    [Fact]
    public void Cargo_operation_marker_does_not_reclassify_an_unrelated_later_jump()
    {
        var processor = new ActivityHistoryProcessor("PL-TEST");
        processor.ManualActivity = DriverActivity.OtherWork;
        processor.Process(Frame(100, 0, 0, cargoOperationGeneration: 5));
        processor.Process(Frame(101, 1, 0, cargoOperationGeneration: 6));

        var update = processor.Process(Frame(120, 2, 0, cargoOperationGeneration: 6));

        Assert.Single(update.CreatedGaps);
    }

    [Fact]
    public void Late_cargo_marker_withdraws_fresh_gap_and_restores_selected_activity()
    {
        var processor = new ActivityHistoryProcessor("PL-TEST");
        processor.ManualActivity = DriverActivity.OtherWork;
        processor.Process(Frame(100, 0, 0, cargoOperationGeneration: 3));

        var jumped = processor.Process(Frame(120, 1, 0, cargoOperationGeneration: 3));
        var gap = Assert.Single(jumped.CreatedGaps);

        var confirmed = processor.Process(Frame(120, 2, 0, cargoOperationGeneration: 4));
        processor.Process(Frame(121, 3, 0, cargoOperationGeneration: 4));
        processor.Flush(Epoch.AddSeconds(4));

        Assert.Empty(processor.CanonicalGaps());
        var removal = Assert.Single(confirmed.CreatedGapBatches);
        Assert.Empty(removal.Gaps);
        Assert.NotNull(removal.RemovedGapIds);
        Assert.Equal(gap.Id, Assert.Single(removal.RemovedGapIds!));
        var operationMinutes = processor.CurrentTimeline.Records.Where(record =>
            record.Start >= new GameTime(101) &&
            record.EndExclusive <= new GameTime(120)).ToList();
        Assert.Equal(19, operationMinutes.Count);
        Assert.All(operationMinutes, record =>
        {
            Assert.Equal(DriverActivity.OtherWork, record.Activity);
            Assert.Equal(ActivitySource.Reconstructed, record.Source);
        });
    }

    [Theory]
    [InlineData(DriverActivity.OtherWork)]
    [InlineData(DriverActivity.Availability)]
    [InlineData(DriverActivity.BreakOrRest)]
    public void Cargo_marker_received_while_paused_preserves_pre_pause_activity_for_jump(
        DriverActivity selectedActivity)
    {
        var processor = new ActivityHistoryProcessor("PL-TEST");
        processor.ManualActivity = selectedActivity;
        processor.Process(Frame(100, 0, 0, cargoOperationGeneration: 0));
        processor.Process(new TelemetryFrame(
            new GameTime(100),
            Epoch.AddSeconds(1),
            SpeedKph: 0,
            GamePaused: true,
            CargoOperationGeneration: 1));

        var resumed = processor.Process(Frame(121, 2, 0, cargoOperationGeneration: 1));
        processor.Flush(Epoch.AddSeconds(3));

        Assert.Empty(resumed.CreatedGaps);
        Assert.Empty(processor.CanonicalGaps());
        var operationMinutes = processor.CurrentTimeline.Records.Where(record =>
            record.Start >= new GameTime(101) &&
            record.EndExclusive <= new GameTime(121)).ToList();
        Assert.Equal(20, operationMinutes.Count);
        Assert.All(operationMinutes, record =>
            Assert.Equal(selectedActivity, record.Activity));
    }

    [Fact]
    public void Large_forward_jump_does_not_reconstruct_rest_when_vehicle_moves_after_jump()
    {
        var processor = new ActivityHistoryProcessor("PL-TEST");
        processor.ManualActivity = DriverActivity.BreakOrRest;

        processor.Process(Frame(10, 0, 0));
        var update = processor.Process(Frame(490, 1, 30));
        processor.Flush(Epoch.AddSeconds(2));

        Assert.True(update.GameTimeJumpDetected);
        Assert.DoesNotContain(
            processor.CurrentTimeline.Records,
            record => record.Source == ActivitySource.Reconstructed);
    }

    [Fact]
    public void Backward_branch_truncates_an_existing_gap_at_the_branch_anchor()
    {
        var processor = new ActivityHistoryProcessor("PL-TEST");

        processor.Process(Frame(10, 0, 30));
        processor.Process(Frame(100, 1, 30));
        var movedBack = processor.Process(Frame(50, 2, 0));

        Assert.True(movedBack.ClockMovedBackward);
        var gap = Assert.Single(processor.CanonicalGaps());
        Assert.Equal(new GameTime(11), gap.Start);
        Assert.Equal(new GameTime(50), gap.EndExclusive);
        Assert.Equal(0, gap.SessionIndex);
    }

    [Fact]
    public void Backward_clock_starts_an_independent_session()
    {
        var processor = new ActivityHistoryProcessor("PL-TEST");

        processor.Process(Frame(20, 0, 0));
        var update = processor.Process(Frame(5, 1, 0));

        Assert.True(update.ClockMovedBackward);
        Assert.Equal(1, update.SessionIndex);
        Assert.Equal(2, processor.Sessions.Count);
        var oldSessionBatch = Assert.Single(update.CompletedBatches);
        Assert.Equal(0, oldSessionBatch.SessionIndex);
        Assert.Equal(new GameTime(20), oldSessionBatch.SessionStartedAt);
        Assert.NotEmpty(oldSessionBatch.Records);
        var newSession = Assert.Single(update.OpenedSessions);
        Assert.Equal(1, newSession.SessionIndex);
        Assert.Equal(new GameTime(5), newSession.StartedAt);
    }

    [Fact]
    public void Out_overrides_driving_classification()
    {
        var processor = new ActivityHistoryProcessor("PL-TEST");
        processor.SetOutMode(true);

        processor.Process(Frame(0, 0, 80));
        processor.Process(Frame(1, 1, 80));
        var update = processor.Process(Frame(2, 2, 80));

        Assert.Equal(DriverActivity.OutOfScope, update.CompletedRecords[^1].Activity);
    }

    [Fact]
    public void Ferry_condition_is_preserved_on_closed_record()
    {
        var processor = new ActivityHistoryProcessor("PL-TEST");
        processor.SetFerryMode(true);

        processor.Process(Frame(0, 0, 0));
        processor.Process(Frame(1, 1, 0));
        var update = processor.Process(Frame(2, 2, 0));

        Assert.Equal(SpecialCondition.FerryCrossing, update.CompletedRecords[^1].Condition);
    }

    [Fact]
    public void First_frame_only_establishes_world_generation_baseline()
    {
        var processor = new ActivityHistoryProcessor("PL-TEST");

        var update = processor.Process(Frame(100, 0, 0, worldGeneration: 9));

        Assert.False(update.WorldGenerationChanged);
        Assert.Empty(update.OpenedSessions);
        Assert.Equal(0, update.SessionIndex);
        Assert.Single(processor.Sessions);
    }

    [Fact]
    public void World_generation_change_starts_branch_even_at_identical_game_time()
    {
        var processor = new ActivityHistoryProcessor("PL-TEST");
        processor.Process(Frame(100, 0, 0, worldGeneration: 5));
        processor.Process(Frame(101, 1, 0, worldGeneration: 5));

        var update = processor.Process(Frame(101, 2, 0, worldGeneration: 8));

        Assert.True(update.WorldGenerationChanged);
        Assert.False(update.ClockMovedBackward);
        Assert.Equal(1, update.SessionIndex);
        Assert.Equal(0, Assert.Single(update.CompletedBatches).SessionIndex);
        var opened = Assert.Single(update.OpenedSessions);
        Assert.Equal(1, opened.SessionIndex);
        Assert.Equal(new GameTime(101), opened.StartedAt);
        Assert.Equal(2, processor.Sessions.Count);
    }

    [Fact]
    public void World_generation_change_starts_branch_when_loaded_time_moves_forward()
    {
        var processor = new ActivityHistoryProcessor("PL-TEST");
        processor.Process(Frame(100, 0, 0, worldGeneration: 1));

        var update = processor.Process(Frame(110, 1, 0, worldGeneration: 2));

        Assert.True(update.WorldGenerationChanged);
        Assert.False(update.GameTimeJumpDetected);
        Assert.DoesNotContain(update.CompletedRecords, record => record.Source == ActivitySource.Reconstructed);
        Assert.Equal(new GameTime(110), Assert.Single(update.OpenedSessions).StartedAt);
    }

    [Fact]
    public void Generation_change_during_pause_waits_for_first_active_frame()
    {
        var processor = new ActivityHistoryProcessor("PL-TEST");
        processor.Process(Frame(100, 0, 0, worldGeneration: 3));

        var paused = processor.Process(new TelemetryFrame(
            new GameTime(100), Epoch.AddSeconds(1), 0, GamePaused: true, WorldGeneration: 4));
        var resumed = processor.Process(Frame(99, 2, 0, worldGeneration: 4));

        Assert.False(paused.WorldGenerationChanged);
        Assert.Empty(paused.OpenedSessions);
        Assert.True(resumed.WorldGenerationChanged);
        Assert.True(resumed.ClockMovedBackward);
        Assert.Equal(1, resumed.SessionIndex);
    }

    [Fact]
    public void Multiple_generation_increments_between_frames_create_one_branch()
    {
        var processor = new ActivityHistoryProcessor("PL-TEST");
        processor.Process(Frame(100, 0, 0, worldGeneration: 2));

        var update = processor.Process(Frame(101, 1, 0, worldGeneration: 7));

        Assert.True(update.WorldGenerationChanged);
        Assert.Single(update.OpenedSessions);
        Assert.Equal(2, processor.Sessions.Count);
    }

    private static TelemetryFrame Frame(
        long minute,
        int second,
        double speed,
        uint worldGeneration = 0,
        uint cargoOperationGeneration = 0) =>
        new(
            new GameTime(minute),
            Epoch.AddSeconds(second),
            speed,
            GamePaused: false,
            WorldGeneration: worldGeneration,
            CargoOperationGeneration: cargoOperationGeneration);
}

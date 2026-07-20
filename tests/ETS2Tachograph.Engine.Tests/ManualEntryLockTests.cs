using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Telemetry;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Engine.Tests;

public sealed class ManualEntryLockTests
{
    private static readonly DateTimeOffset Epoch =
        new(2026, 7, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Reinserting_card_with_unresolved_card_removed_gap_locks_tachograph_state()
    {
        var crew = RemovedCardGap(100, 700);

        var inserted = crew.InsertCard(TachographSlot.Driver, "CARD-LOCK");
        var moving = crew.ProcessFrame(Frame(701, 30));

        Assert.NotNull(inserted.Snapshot.RequiredManualEntryGap);
        Assert.Equal(ActivityGapReason.CardRemoved, inserted.Snapshot.RequiredManualEntryGap!.Reason);
        Assert.True(moving.ManualEntryRequired);
        Assert.True(moving.DrivingLockedByManualEntry);
        Assert.Equal(DriverActivity.Driving, moving.Driver!.ProvisionalActivity);
        Assert.Throws<InvalidOperationException>(() =>
            crew.SetManualActivity(TachographSlot.Driver, DriverActivity.OtherWork));
        Assert.Throws<InvalidOperationException>(() =>
            crew.EjectCard(TachographSlot.Driver, Epoch.AddMinutes(701)));
    }

    [Fact]
    public void Resolving_required_gap_unlocks_tachograph()
    {
        var crew = RemovedCardGap(100, 700);
        var inserted = crew.InsertCard(TachographSlot.Driver, "CARD-LOCK");
        var gap = inserted.Snapshot.RequiredManualEntryGap!;
        var segment = new ActivityRecord
        {
            Id = Guid.NewGuid(),
            DriverCardId = "CARD-LOCK",
            Activity = DriverActivity.BreakOrRest,
            Start = gap.Start,
            EndExclusive = gap.EndExclusive!.Value,
            RecordedAtUtc = Epoch.AddMinutes(700),
            Source = ActivitySource.ManualEntry,
            SourceGapId = gap.Id
        };

        crew.ApplyManualEntryResolution(
            "CARD-LOCK",
            gap with
            {
                State = ActivityGapState.Resolved,
                ResolvedAt = new GameTime(700)
            },
            [segment]);

        Assert.False(crew.Current.ManualEntryRequired);
        Assert.False(crew.Current.DrivingLockedByManualEntry);
        crew.SetManualActivity(TachographSlot.Driver, DriverActivity.OtherWork);
        Assert.Equal(DriverActivity.OtherWork, crew.Current.Driver!.ManualActivity);
    }

    [Fact]
    public void Forward_time_jump_gap_is_optional_and_does_not_lock_driving()
    {
        var gap = new ActivityGap
        {
            Id = Guid.NewGuid(),
            DriverCardId = "CARD-JUMP",
            Slot = 1,
            SessionIndex = 0,
            Start = new GameTime(100),
            EndExclusive = new GameTime(700),
            Reason = ActivityGapReason.ForwardTimeJump,
            State = ActivityGapState.Unresolved
        };
        var crew = new CrewTachographEngine();
        crew.RegisterCard(
            "CARD-JUMP",
            [new RestoredActivitySession(0, new GameTime(0), [], [gap])]);

        var inserted = crew.InsertCard(TachographSlot.Driver, "CARD-JUMP");

        Assert.Null(inserted.Snapshot.RequiredManualEntryGap);
        Assert.Equal(gap.Id, inserted.Snapshot.OptionalManualEntryGap?.Id);
        Assert.False(crew.Current.ManualEntryRequired);
        Assert.False(crew.Current.DrivingLockedByManualEntry);
    }

    [Fact]
    public void Materialized_projection_resolution_replaces_clipped_source_gap_after_restart()
    {
        var sourceGap = new ActivityGap
        {
            Id = Guid.NewGuid(),
            DriverCardId = "CARD-PROJECTION-RESTART",
            Slot = 1,
            SessionIndex = 0,
            Start = new GameTime(100),
            EndExclusive = null,
            Reason = ActivityGapReason.CardRemoved,
            State = ActivityGapState.Unresolved
        };
        var resolvedGap = sourceGap with
        {
            Id = Guid.NewGuid(),
            SessionIndex = 1,
            EndExclusive = new GameTime(160),
            State = ActivityGapState.Resolved,
            ResolvedAt = new GameTime(161),
            ProjectionSourceGapId = sourceGap.Id
        };
        var segment = new ActivityRecord
        {
            Id = Guid.NewGuid(),
            DriverCardId = sourceGap.DriverCardId,
            Activity = DriverActivity.BreakOrRest,
            Start = sourceGap.Start,
            EndExclusive = resolvedGap.EndExclusive.Value,
            RecordedAtUtc = Epoch,
            Source = ActivitySource.ManualEntry,
            SourceGapId = resolvedGap.Id
        };
        var crew = new CrewTachographEngine();
        crew.RegisterCard(sourceGap.DriverCardId,
        [
            new RestoredActivitySession(0, new GameTime(90), [], [sourceGap]),
            new RestoredActivitySession(1, new GameTime(160), [segment], [resolvedGap])
        ]);

        var inserted = crew.InsertCard(TachographSlot.Driver, sourceGap.DriverCardId);

        Assert.Null(inserted.Snapshot.RequiredManualEntryGap);
        Assert.False(crew.Current.ManualEntryRequired);
        Assert.False(crew.Current.DrivingLockedByManualEntry);
    }

    [Fact]
    public void Resolving_removed_card_gap_as_rest_joins_rest_before_ejection_and_resets_day()
    {
        var gap = new ActivityGap
        {
            Id = Guid.NewGuid(),
            DriverCardId = "CARD-CONTINUOUS-REST",
            Slot = 1,
            SessionIndex = 0,
            Start = new GameTime(180),
            EndExclusive = null,
            Reason = ActivityGapReason.CardRemoved,
            State = ActivityGapState.Unresolved
        };
        var restored = new RestoredActivitySession(
            0,
            new GameTime(0),
            [
                Record("CARD-CONTINUOUS-REST", 0, 60, DriverActivity.Driving),
                Record("CARD-CONTINUOUS-REST", 60, 180, DriverActivity.BreakOrRest)
            ],
            [gap]);
        var crew = new CrewTachographEngine();
        crew.RegisterCard(gap.DriverCardId, [restored]);
        crew.ProcessFrame(Frame(600, 0));

        var inserted = crew.InsertCard(TachographSlot.Driver, gap.DriverCardId);
        var closedGap = inserted.Snapshot.RequiredManualEntryGap!;
        var manualRest = Record(
            gap.DriverCardId,
            closedGap.Start.TotalMinutes,
            closedGap.EndExclusive!.Value.TotalMinutes,
            DriverActivity.BreakOrRest) with
        {
            Source = ActivitySource.ManualEntry,
            SourceGapId = closedGap.Id
        };

        crew.ApplyManualEntryResolution(
            gap.DriverCardId,
            closedGap with
            {
                State = ActivityGapState.Resolved,
                ResolvedAt = new GameTime(600)
            },
            [manualRest]);

        Assert.False(crew.Current.ManualEntryRequired);
        Assert.Equal(
            new GameTime(600),
            crew.Current.Driver!.Regulation!.State.LastDailyRestResetAt);
        var rest = Assert.Single(crew.Current.Driver.Regulation.QualifiedRests);
        Assert.Equal(540, rest.DurationMinutes);
        Assert.Equal(closedGap.Id, rest.SourceGapId);
    }

    private static CrewTachographEngine RemovedCardGap(long removedAt, long reinsertedAt)
    {
        var crew = new CrewTachographEngine();
        crew.RegisterCard("CARD-LOCK");
        crew.InsertCard(TachographSlot.Driver, "CARD-LOCK");
        crew.ProcessFrame(Frame(removedAt, 0));
        crew.EjectCard(TachographSlot.Driver, Epoch.AddMinutes(removedAt));
        crew.ProcessFrame(Frame(reinsertedAt, 0));
        return crew;
    }

    private static TelemetryFrame Frame(long minute, double speed) =>
        new(new GameTime(minute), Epoch.AddMinutes(minute), speed, false);

    private static ActivityRecord Record(
        string cardId,
        long start,
        long end,
        DriverActivity activity) => new()
    {
        Id = Guid.NewGuid(),
        DriverCardId = cardId,
        Activity = activity,
        Start = new GameTime(start),
        EndExclusive = new GameTime(end),
        RecordedAtUtc = Epoch.AddMinutes(end),
        Source = ActivitySource.Telemetry
    };
}

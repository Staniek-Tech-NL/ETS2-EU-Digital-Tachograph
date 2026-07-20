using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Telemetry;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Engine.Tests;

public sealed class CardRemovedGapTests
{
    private static readonly DateTimeOffset Epoch =
        new(2026, 7, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Eject_jump_insert_creates_one_card_removed_gap_and_no_forward_jump_for_that_card()
    {
        var crew = Crew();
        crew.ProcessFrame(Frame(100));
        crew.EjectCard(TachographSlot.Driver, Epoch.AddMinutes(100));

        crew.ProcessFrame(Frame(500));
        crew.InsertCard(TachographSlot.Driver, "CARD-A");

        var gap = Assert.Single(crew.GetEngine("CARD-A")!.History.CanonicalGaps());
        Assert.Equal(ActivityGapReason.CardRemoved, gap.Reason);
        Assert.Equal(new GameTime(100), gap.Start);
        Assert.Equal(new GameTime(500), gap.EndExclusive);
        Assert.DoesNotContain(
            crew.GetEngine("CARD-A")!.History.CanonicalGaps(),
            candidate => candidate.Reason == ActivityGapReason.ForwardTimeJump);
    }

    [Fact]
    public void Gap_reason_priority_is_independent_per_card()
    {
        var crew = Crew();
        crew.ProcessFrame(Frame(100));
        crew.EjectCard(TachographSlot.Driver, Epoch.AddMinutes(100));

        crew.ProcessFrame(Frame(500));

        var removedCardGap = Assert.Single(crew.GetEngine("CARD-A")!.History.CanonicalGaps());
        Assert.Equal(ActivityGapReason.CardRemoved, removedCardGap.Reason);
        var insertedCardGap = Assert.Single(crew.GetEngine("CARD-B")!.History.CanonicalGaps());
        Assert.Equal(ActivityGapReason.ForwardTimeJump, insertedCardGap.Reason);
        Assert.Equal(2, insertedCardGap.Slot);
    }

    [Fact]
    public void Rollback_before_open_gap_removes_old_gap_from_canonical_projection()
    {
        var crew = Crew();
        crew.ProcessFrame(Frame(100));
        crew.EjectCard(TachographSlot.Driver, Epoch.AddMinutes(100));

        crew.ProcessFrame(Frame(90));

        Assert.DoesNotContain(
            crew.GetEngine("CARD-A")!.History.CanonicalGaps(),
            gap => gap.Start == new GameTime(100));
    }

    [Fact]
    public void Rollback_while_card_remains_removed_opens_new_gap_at_branch_anchor()
    {
        var crew = Crew();
        crew.ProcessFrame(Frame(100));
        crew.EjectCard(TachographSlot.Driver, Epoch.AddMinutes(100));

        crew.ProcessFrame(Frame(90));

        var gap = Assert.Single(crew.GetEngine("CARD-A")!.History.CanonicalGaps());
        Assert.Equal(new GameTime(90), gap.Start);
        Assert.Null(gap.EndExclusive);
        Assert.Equal(1, gap.SessionIndex);
    }

    [Fact]
    public void Rollback_before_open_gap_does_not_create_phantom_gap_when_card_is_inserted()
    {
        var crew = new CrewTachographEngine();
        crew.RegisterCard("CARD-A", [SessionWithOpenGap("CARD-A", 100)]);
        crew.InsertCard(TachographSlot.Driver, "CARD-A");

        crew.ProcessFrame(Frame(90));

        Assert.Empty(crew.GetEngine("CARD-A")!.History.CanonicalGaps());
    }

    [Fact]
    public void Abandoned_source_branch_keeps_its_open_gap_for_audit()
    {
        var crew = Crew();
        crew.ProcessFrame(Frame(100));
        crew.EjectCard(TachographSlot.Driver, Epoch.AddMinutes(100));

        crew.ProcessFrame(Frame(90));

        var rawGaps = crew.GetEngine("CARD-A")!.History.GapSessions.SelectMany(x => x).ToList();
        Assert.Equal(2, rawGaps.Count);
        var abandoned = Assert.Single(rawGaps, gap => gap.SessionIndex == 0);
        Assert.Equal(new GameTime(100), abandoned.Start);
        Assert.Null(abandoned.EndExclusive);
    }

    [Fact]
    public void Repeated_rollbacks_keep_only_one_canonical_open_gap_per_card()
    {
        var crew = Crew();
        crew.ProcessFrame(Frame(100));
        crew.EjectCard(TachographSlot.Driver, Epoch.AddMinutes(100));

        crew.ProcessFrame(Frame(90));
        crew.ProcessFrame(Frame(80));

        var gap = Assert.Single(crew.GetEngine("CARD-A")!.History.CanonicalGaps());
        Assert.Equal(new GameTime(80), gap.Start);
        Assert.True(gap.IsOpen);
        Assert.Equal(3, crew.GetEngine("CARD-A")!.History.GapSessions.SelectMany(x => x).Count());
    }

    [Fact]
    public void Eject_and_insert_in_same_game_minute_discards_zero_length_gap()
    {
        var crew = Crew();
        crew.ProcessFrame(Frame(100));
        var ejected = crew.EjectCard(TachographSlot.Driver, Epoch.AddMinutes(100));
        var openedId = Assert.Single(ejected.Snapshot.CreatedGaps).Id;

        var inserted = crew.InsertCard(TachographSlot.Driver, "CARD-A");

        Assert.Empty(crew.GetEngine("CARD-A")!.History.CanonicalGaps());
        var removal = Assert.Single(inserted.Snapshot.CreatedGapBatches);
        Assert.Empty(removal.Gaps);
        Assert.Contains(openedId, removal.RemovedGapIds!);
    }

    [Fact]
    public void Canonical_card_removed_gaps_are_positive_and_never_overlap()
    {
        var crew = Crew();
        crew.ProcessFrame(Frame(100));
        crew.EjectCard(TachographSlot.Driver, Epoch.AddMinutes(100));
        crew.ProcessFrame(Frame(90));
        crew.ProcessFrame(Frame(80));
        crew.ProcessFrame(Frame(120));
        crew.InsertCard(TachographSlot.Driver, "CARD-A");

        var gaps = crew.GetEngine("CARD-A")!.History.CanonicalGaps();
        Assert.All(gaps, gap => Assert.True(gap.EndExclusive > gap.Start));
        Assert.All(gaps.Zip(gaps.Skip(1)), pair =>
            Assert.True(pair.First.EndExclusive <= pair.Second.Start));
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

    private static RestoredActivitySession SessionWithOpenGap(string cardId, long start) =>
        new(
            0,
            new GameTime(start),
            [],
            [new ActivityGap
            {
                Id = Guid.NewGuid(),
                DriverCardId = cardId,
                Slot = 1,
                SessionIndex = 0,
                Start = new GameTime(start),
                EndExclusive = null,
                Reason = ActivityGapReason.CardRemoved,
                State = ActivityGapState.Unresolved
            }]);

    private static TelemetryFrame Frame(long minute) =>
        new(new GameTime(minute), Epoch.AddMinutes(minute), 0, GamePaused: false);
}

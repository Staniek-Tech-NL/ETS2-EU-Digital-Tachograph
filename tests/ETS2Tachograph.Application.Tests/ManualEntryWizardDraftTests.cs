using ETS2Tachograph.Application.Services;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Application.Tests;

public sealed class ManualEntryWizardDraftTests
{
    [Fact]
    public void Empty_work_selection_builds_one_click_full_rest_entry()
    {
        var segments = ManualEntryWizardDraft.Build(Gap(), []);

        var rest = Assert.Single(segments);
        Assert.Equal(100, rest.FromGameMinute);
        Assert.Equal(700, rest.ToGameMinuteExclusive);
        Assert.Equal(DriverActivity.BreakOrRest, rest.Activity);
    }

    [Fact]
    public void Work_blocks_are_subtracted_and_every_other_minute_remains_rest()
    {
        var segments = ManualEntryWizardDraft.Build(
            Gap(),
            [new ManualEntryWorkBlock(220, 280), new ManualEntryWorkBlock(400, 460)]);

        Assert.Collection(
            segments,
            item => AssertSegment(item, 100, 220, DriverActivity.BreakOrRest),
            item => AssertSegment(item, 220, 280, DriverActivity.OtherWork),
            item => AssertSegment(item, 280, 400, DriverActivity.BreakOrRest),
            item => AssertSegment(item, 400, 460, DriverActivity.OtherWork),
            item => AssertSegment(item, 460, 700, DriverActivity.BreakOrRest));
        Assert.Equal(600, segments.Sum(item => item.ToGameMinuteExclusive - item.FromGameMinute));
    }

    [Theory]
    [InlineData(90, 120)]
    [InlineData(690, 710)]
    [InlineData(300, 300)]
    public void Invalid_work_block_is_rejected_before_resolve(long from, long to)
    {
        Assert.Throws<ManualEntryDraftException>(() =>
            ManualEntryWizardDraft.Build(Gap(), [new ManualEntryWorkBlock(from, to)]));
    }

    [Fact]
    public void Overlapping_work_blocks_are_rejected_before_resolve()
    {
        Assert.Throws<ManualEntryDraftException>(() => ManualEntryWizardDraft.Build(
            Gap(),
            [new ManualEntryWorkBlock(200, 300), new ManualEntryWorkBlock(250, 350)]));
    }

    private static ActivityGap Gap() => new()
    {
        Id = Guid.NewGuid(),
        DriverCardId = "CARD-WIZARD",
        Slot = 1,
        SessionIndex = 0,
        Start = new GameTime(100),
        EndExclusive = new GameTime(700),
        Reason = ActivityGapReason.CardRemoved,
        State = ActivityGapState.Unresolved
    };

    private static void AssertSegment(
        Application.Dtos.ManualEntrySegment segment,
        long from,
        long to,
        DriverActivity activity)
    {
        Assert.Equal(from, segment.FromGameMinute);
        Assert.Equal(to, segment.ToGameMinuteExclusive);
        Assert.Equal(activity, segment.Activity);
    }
}

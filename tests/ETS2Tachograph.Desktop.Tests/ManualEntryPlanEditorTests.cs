using ETS2Tachograph.Core.Enums;

namespace ETS2Tachograph.Desktop.Tests;

public sealed class ManualEntryPlanEditorTests
{
    [Fact]
    public void New_plan_covers_the_whole_gap_with_rest()
    {
        var editor = new ManualEntryPlanEditor(0, 30);

        var segment = Assert.Single(editor.Segments);
        Assert.Equal((0, 30, DriverActivity.BreakOrRest),
            (segment.FromGameMinute, segment.ToGameMinuteExclusive, segment.Activity));
        Assert.True(editor.IsComplete);
        Assert.Equal(30, editor.RestMinutes);
    }

    [Fact]
    public void Replacing_middle_of_rest_splits_it_into_three_segments()
    {
        var editor = new ManualEntryPlanEditor(0, 30);

        editor.Replace(10, 20, DriverActivity.OtherWork);

        Assert.Collection(
            editor.Segments,
            segment => AssertSegment(segment, 0, 10, DriverActivity.BreakOrRest),
            segment => AssertSegment(segment, 10, 20, DriverActivity.OtherWork),
            segment => AssertSegment(segment, 20, 30, DriverActivity.BreakOrRest));
        Assert.Equal(20, editor.RestMinutes);
        Assert.Equal(10, editor.OtherWorkMinutes);
        Assert.True(editor.IsComplete);
    }

    [Fact]
    public void Replacement_can_cross_existing_segments_without_overlap()
    {
        var editor = new ManualEntryPlanEditor(0, 60);
        editor.Replace(10, 20, DriverActivity.OtherWork);
        editor.Replace(30, 40, DriverActivity.Availability);

        editor.Replace(15, 35, DriverActivity.Availability);

        Assert.Collection(
            editor.Segments,
            segment => AssertSegment(segment, 0, 10, DriverActivity.BreakOrRest),
            segment => AssertSegment(segment, 10, 15, DriverActivity.OtherWork),
            segment => AssertSegment(segment, 15, 40, DriverActivity.Availability),
            segment => AssertSegment(segment, 40, 60, DriverActivity.BreakOrRest));
        Assert.True(editor.IsComplete);
    }

    [Fact]
    public void Adjacent_segments_with_the_same_activity_are_merged()
    {
        var editor = new ManualEntryPlanEditor(0, 30);

        editor.Replace(0, 10, DriverActivity.OtherWork);
        editor.Replace(10, 20, DriverActivity.OtherWork);

        Assert.Collection(
            editor.Segments,
            segment => AssertSegment(segment, 0, 20, DriverActivity.OtherWork),
            segment => AssertSegment(segment, 20, 30, DriverActivity.BreakOrRest));
    }

    [Fact]
    public void Removing_non_rest_segment_restores_and_merges_rest()
    {
        var editor = new ManualEntryPlanEditor(0, 30);
        editor.Replace(10, 20, DriverActivity.OtherWork);

        editor.Remove(editor.Segments[1]);

        var segment = Assert.Single(editor.Segments);
        AssertSegment(segment, 0, 30, DriverActivity.BreakOrRest);
    }

    [Fact]
    public void Rest_segment_cannot_be_removed()
    {
        var editor = new ManualEntryPlanEditor(0, 30);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            editor.Remove(editor.Segments[0]));

        Assert.Contains("domyślnym", exception.Message);
    }

    [Fact]
    public void Editing_shorter_non_rest_segment_returns_released_minutes_to_rest()
    {
        var editor = new ManualEntryPlanEditor(0, 30);
        editor.Replace(5, 25, DriverActivity.OtherWork);
        var original = editor.Segments[1];

        editor.Edit(original, 10, 20, DriverActivity.Availability);

        Assert.Collection(
            editor.Segments,
            segment => AssertSegment(segment, 0, 10, DriverActivity.BreakOrRest),
            segment => AssertSegment(segment, 10, 20, DriverActivity.Availability),
            segment => AssertSegment(segment, 20, 30, DriverActivity.BreakOrRest));
    }

    [Fact]
    public void Rejected_edit_does_not_modify_the_existing_plan()
    {
        var editor = new ManualEntryPlanEditor(0, 30);
        editor.Replace(5, 25, DriverActivity.OtherWork);
        var original = editor.Segments[1];
        var before = editor.Segments.ToList();

        Assert.Throws<InvalidOperationException>(() =>
            editor.Edit(original, -1, 20, DriverActivity.Availability));

        Assert.Equal(before, editor.Segments);
    }

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(0, 31)]
    [InlineData(10, 10)]
    [InlineData(20, 10)]
    public void Invalid_range_is_rejected(long from, long to)
    {
        var editor = new ManualEntryPlanEditor(0, 30);

        Assert.Throws<InvalidOperationException>(() =>
            editor.Replace(from, to, DriverActivity.OtherWork));
    }

    [Fact]
    public void Driving_cannot_be_added_as_manual_activity()
    {
        var editor = new ManualEntryPlanEditor(0, 30);

        Assert.Throws<InvalidOperationException>(() =>
            editor.Replace(0, 30, DriverActivity.Driving));
    }

    [Fact]
    public void Output_segments_preserve_all_three_supported_activities()
    {
        var editor = new ManualEntryPlanEditor(1_430, 1_470);
        editor.Replace(1_440, 1_450, DriverActivity.OtherWork);
        editor.Replace(1_450, 1_460, DriverActivity.Availability);

        var output = editor.ToSegments();

        Assert.Equal(4, output.Count);
        Assert.Equal(
            [
                DriverActivity.BreakOrRest,
                DriverActivity.OtherWork,
                DriverActivity.Availability,
                DriverActivity.BreakOrRest
            ],
            output.Select(segment => segment.Activity));
        Assert.Equal(40, output.Sum(segment =>
            segment.ToGameMinuteExclusive - segment.FromGameMinute));
    }

    private static void AssertSegment(
        ManualEntrySegmentRow segment,
        long from,
        long to,
        DriverActivity activity)
    {
        Assert.Equal((from, to, activity),
            (segment.FromGameMinute, segment.ToGameMinuteExclusive, segment.Activity));
    }
}

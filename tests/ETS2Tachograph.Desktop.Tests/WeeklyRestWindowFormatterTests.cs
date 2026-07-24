using ETS2Tachograph.Desktop;

namespace ETS2Tachograph.Desktop.Tests;

public sealed class WeeklyRestWindowFormatterTests
{
    private const long ReferenceDeadline = 202_975;

    [Fact] // WRF-01
    public void Formats_reference_deadline()
    {
        Assert.Equal(
            "4/6 (D141 22:55)",
            WeeklyRestWindowFormatter.Format(5_379, ReferenceDeadline));
    }

    [Theory] // WRF-02
    [InlineData(0)]
    [InlineData(1_439)]
    public void Fresh_window_stays_in_first_period(long elapsedMinutes)
    {
        Assert.Equal(
            "1/6 (D141 22:55)",
            WeeklyRestWindowFormatter.Format(elapsedMinutes, ReferenceDeadline));
    }

    [Fact] // WRF-03
    public void First_full_day_starts_second_period()
    {
        Assert.Equal(
            "2/6 (D141 22:55)",
            WeeklyRestWindowFormatter.Format(1_440, ReferenceDeadline));
    }

    [Theory] // WRF-04
    [InlineData(5_759, "4/6 (D141 22:55)")]
    [InlineData(5_760, "5/6 (D141 22:55)")]
    public void Ninety_six_hour_boundary_does_not_round_up(
        long elapsedMinutes,
        string expected)
    {
        Assert.Equal(
            expected,
            WeeklyRestWindowFormatter.Format(elapsedMinutes, ReferenceDeadline));
    }

    [Fact] // WRF-05
    public void Last_minute_before_limit_stays_in_sixth_period()
    {
        Assert.Equal(
            "6/6 (D141 22:55)",
            WeeklyRestWindowFormatter.Format(8_639, ReferenceDeadline));
    }

    [Fact] // WRF-06
    public void Exact_limit_does_not_show_overrun()
    {
        Assert.Equal(
            "6/6 (D141 22:55)",
            WeeklyRestWindowFormatter.Format(8_640, ReferenceDeadline));
    }

    [Fact] // WRF-07
    public void First_minute_after_limit_shows_overrun()
    {
        Assert.Equal(
            "6/6+ (D141 22:55)",
            WeeklyRestWindowFormatter.Format(8_641, ReferenceDeadline));
    }

    [Fact] // WRF-08
    public void Game_minute_zero_is_displayed_as_day_one()
    {
        Assert.Equal(
            "1/6 (D1 00:00)",
            WeeklyRestWindowFormatter.Format(0, 0));
    }

    [Fact] // WRF-09
    public void Formats_deadline_after_midnight_on_next_day()
    {
        Assert.Equal(
            "1/6 (D2 00:05)",
            WeeklyRestWindowFormatter.Format(0, 1_445));
    }

    [Fact] // WRF-10
    public void Deadline_does_not_move_while_same_window_progresses()
    {
        var first = WeeklyRestWindowFormatter.Format(5_379, ReferenceDeadline);
        var later = WeeklyRestWindowFormatter.Format(5_384, ReferenceDeadline);

        Assert.EndsWith("(D141 22:55)", first);
        Assert.EndsWith("(D141 22:55)", later);
    }

    [Theory] // WRF-11
    [InlineData(5_379L, "4/6 (—)")]
    [InlineData(null, "—/6 (—)")]
    public void Uses_safe_fallback_without_deadline(
        long? elapsedMinutes,
        string expected)
    {
        Assert.Equal(
            expected,
            WeeklyRestWindowFormatter.Format(elapsedMinutes, null));
    }

    [Fact] // WRF-12
    public void Formats_independent_values_for_both_slots()
    {
        var slot1 = WeeklyRestWindowFormatter.Format(5_379, ReferenceDeadline);
        var slot2 = WeeklyRestWindowFormatter.Format(1_440, ReferenceDeadline + 1_440);

        Assert.Equal("4/6 (D141 22:55)", slot1);
        Assert.Equal("2/6 (D142 22:55)", slot2);
    }

    [Fact] // WRF-13
    public void Same_projection_after_restart_produces_identical_text()
    {
        var beforeRestart = WeeklyRestWindowFormatter.Format(5_379, ReferenceDeadline);
        var afterRestart = WeeklyRestWindowFormatter.Format(5_379, ReferenceDeadline);

        Assert.Equal(beforeRestart, afterRestart);
    }

    [Fact] // WRF-14
    public void New_canonical_branch_does_not_reuse_old_deadline()
    {
        var oldBranch = WeeklyRestWindowFormatter.Format(5_379, ReferenceDeadline);
        var newBranch = WeeklyRestWindowFormatter.Format(60, ReferenceDeadline + 2_880);

        Assert.Equal("4/6 (D141 22:55)", oldBranch);
        Assert.Equal("1/6 (D143 22:55)", newBranch);
        Assert.NotEqual(oldBranch, newBranch);
    }

    [Fact] // WRF-15
    public void Same_game_time_projection_is_independent_of_wall_clock()
    {
        var first = WeeklyRestWindowFormatter.Format(5_379, ReferenceDeadline);
        var second = WeeklyRestWindowFormatter.Format(5_379, ReferenceDeadline);

        Assert.Equal(first, second);
    }
}

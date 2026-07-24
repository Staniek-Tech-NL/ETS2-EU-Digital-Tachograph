using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Core.Tests.Time;

public sealed class GameTimeTests
{
    [Theory]
    [InlineData(0, "Dzień 1, 00:00")]
    [InlineData(870, "Dzień 1, 14:30")]
    [InlineData(2310, "Dzień 2, 14:30")]
    public void Game_clock_is_formatted_as_day_and_time(long minute, string expected)
    {
        Assert.Equal(expected, GameClockFormatter.Format(new GameTime(minute)));
    }

    [Theory]
    [InlineData(0, "00:00")]
    [InlineData(870, "14:30")]
    [InlineData(2_310, "14:30")]
    [InlineData(2_879, "23:59")]
    public void Compact_game_clock_uses_time_of_day_from_game_time(long minute, string expected)
    {
        Assert.Equal(expected, GameClockFormatter.FormatTimeOfDay(new GameTime(minute)));
    }

    [Theory]
    [InlineData("Dzień 12, 14:30", 16710)]
    [InlineData("D12 14:30", 16710)]
    [InlineData("16710", 16710)]
    public void Game_clock_parser_accepts_display_and_legacy_formats(string value, long expectedMinute)
    {
        Assert.True(GameClockFormatter.TryParse(value, out var parsed));
        Assert.Equal(expectedMinute, parsed.TotalMinutes);
    }

    [Fact]
    public void Negative_game_time_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameTime(-1));
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(10_079, 0, 0)]
    [InlineData(10_080, 0, 1)]
    [InlineData(0, -1, -1)]
    [InlineData(8_640, 1, 1)]
    public void Game_week_uses_monday_epoch_and_offset(long minute, int offsetDays, long expectedWeek)
    {
        Assert.Equal(new GameWeek(expectedWeek), GameWeek.From(new GameTime(minute), offsetDays));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(6)]
    public void Public_week_bounds_equal_the_existing_regulatory_formula(int offsetDays)
    {
        var week = new GameWeek(17);
        var expectedStart =
            (week.Index * GameWeek.MinutesPerWeek) -
            ((long)offsetDays * GameWeek.MinutesPerDay);

        var bounds = week.GetBounds(offsetDays);

        Assert.Equal(expectedStart, bounds.StartGameMinute);
        Assert.Equal(
            expectedStart + GameWeek.MinutesPerWeek,
            bounds.EndGameMinuteExclusive);
    }

    [Theory]
    [InlineData(-1, 1_440)]
    [InlineData(0, 0)]
    [InlineData(1, 8_640)]
    [InlineData(6, 1_440)]
    public void Canonical_week_boundaries_cover_exact_edges(
        int offsetDays,
        long boundary)
    {
        var atBoundary = GameWeek.From(new GameTime(boundary), offsetDays);
        var bounds = atBoundary.GetBounds(offsetDays);

        Assert.Equal(boundary, bounds.StartGameMinute);
        Assert.True(bounds.Contains(new GameTime(boundary)));
        Assert.True(bounds.Contains(new GameTime(bounds.EndGameMinuteExclusive - 1)));
        Assert.False(bounds.Contains(new GameTime(bounds.EndGameMinuteExclusive)));

        if (boundary > 0)
        {
            Assert.NotEqual(
                atBoundary,
                GameWeek.From(new GameTime(boundary - 1), offsetDays));
        }
    }

    [Fact]
    public void Raw_equivalent_offsets_keep_distinct_week_indices()
    {
        var time = new GameTime(1_440);

        var negative = GameWeek.From(time, -1);
        var positive = GameWeek.From(time, 6);

        Assert.NotEqual(negative, positive);
        Assert.Equal(
            negative.GetBounds(-1),
            positive.GetBounds(6));
    }

    [Theory]
    [InlineData(0, 0, GameWeekday.Monday)]
    [InlineData(1_439, 0, GameWeekday.Monday)]
    [InlineData(1_440, 0, GameWeekday.Tuesday)]
    [InlineData(8_640, 0, GameWeekday.Sunday)]
    [InlineData(0, 1, GameWeekday.Tuesday)]
    [InlineData(0, -1, GameWeekday.Sunday)]
    public void Calendar_resolver_uses_the_canonical_week_start(
        long minute,
        int offsetDays,
        GameWeekday expectedWeekday)
    {
        var resolver = new GameCalendarResolver(new GameCalendarContext(offsetDays));

        var moment = resolver.Resolve(new GameTime(minute));

        Assert.Equal(expectedWeekday, moment.Weekday);
        Assert.Equal((minute / GameWeek.MinutesPerDay) + 1, moment.DisplayedGameDay);
        Assert.True(moment.WeekBounds.Contains(moment.GameTime));
    }

    [Fact]
    public void Displayed_game_day_does_not_change_with_week_offset()
    {
        var time = new GameTime((146 * GameWeek.MinutesPerDay) + 870);

        var first = new GameCalendarResolver(new GameCalendarContext(-1)).Resolve(time);
        var second = new GameCalendarResolver(new GameCalendarContext(6)).Resolve(time);

        Assert.Equal(147, first.DisplayedGameDay);
        Assert.Equal(first.DisplayedGameDay, second.DisplayedGameDay);
        Assert.Equal(first.Hour, second.Hour);
        Assert.Equal(first.Minute, second.Minute);
    }

    [Theory]
    [InlineData(-7)]
    [InlineData(7)]
    public void Calendar_context_rejects_offsets_outside_the_persisted_contract(
        int offsetDays)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new GameCalendarContext(offsetDays));
    }
}

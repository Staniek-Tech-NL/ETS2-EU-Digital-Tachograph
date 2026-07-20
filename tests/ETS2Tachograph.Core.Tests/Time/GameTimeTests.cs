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
}

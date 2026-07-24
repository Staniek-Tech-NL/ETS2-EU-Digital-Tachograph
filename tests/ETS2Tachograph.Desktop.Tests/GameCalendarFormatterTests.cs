using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.Desktop;

namespace ETS2Tachograph.Desktop.Tests;

public sealed class GameCalendarFormatterTests
{
    private static readonly GameCalendarResolver Calendar =
        new(new GameCalendarContext(0));

    [Fact]
    public void Formats_full_calendar_moment()
    {
        var moment = Calendar.Resolve(new GameTime((28 * 1_440) + 7 * 60 + 29));

        Assert.Equal(
            "Pon · Dzień 29 · 07:29",
            GameCalendarFormatter.FormatFull(moment));
    }

    [Fact]
    public void Formats_compact_calendar_moment()
    {
        var moment = Calendar.Resolve(new GameTime((33 * 1_440) + 22 * 60 + 55));

        Assert.Equal(
            "Sob · Dzień 34 · 22:55",
            GameCalendarFormatter.FormatFull(moment));
        Assert.Equal(
            "SOB · D34 · 22:55",
            GameCalendarFormatter.FormatCompact(moment));
    }

    [Theory]
    [InlineData(GameDeadlineSemantic.CompleteBy, "Ukończ do: Pon · Dzień 29 · 00:00")]
    [InlineData(GameDeadlineSemantic.StartNoLaterThan, "Rozpocznij najpóźniej: Pon · Dzień 29 · 00:00")]
    [InlineData(GameDeadlineSemantic.CompleteBefore, "Ukończ przed: Pon · Dzień 29 · 00:00")]
    [InlineData(GameDeadlineSemantic.AvailableFrom, "Jazda dostępna od: Pon · Dzień 29 · 00:00")]
    public void Formats_exact_full_prefix_for_each_deadline_semantic(
        GameDeadlineSemantic semantic,
        string expected)
    {
        var deadline = new DeadlinePresentation(
            semantic,
            Calendar.Resolve(new GameTime(28 * 1_440)));

        Assert.Equal(expected, GameDeadlineFormatter.FormatFull(deadline));
    }

    [Theory]
    [InlineData(GameDeadlineSemantic.CompleteBy, "Ukończ do: PON · D29 · 00:00")]
    [InlineData(GameDeadlineSemantic.StartNoLaterThan, "Rozpocznij najpóźniej: PON · D29 · 00:00")]
    [InlineData(GameDeadlineSemantic.CompleteBefore, "Ukończ przed: PON · D29 · 00:00")]
    public void M3A_UI_01_to_03_assert_exact_compact_prefixes(
        GameDeadlineSemantic semantic,
        string expected)
    {
        var deadline = new DeadlinePresentation(
            semantic,
            Calendar.Resolve(new GameTime(28 * 1_440)));

        Assert.Equal(expected, GameDeadlineFormatter.FormatCompact(deadline));
    }

    [Theory]
    [InlineData(GameDeadlineSemantic.CompleteBy, "KONIEC≤ PON · D29 · 00:00")]
    [InlineData(GameDeadlineSemantic.StartNoLaterThan, "START≤ PON · D29 · 00:00")]
    [InlineData(GameDeadlineSemantic.CompleteBefore, "PRZED PON · D29 · 00:00")]
    public void Device_format_preserves_boundary_semantics_without_overflowing_labels(
        GameDeadlineSemantic semantic,
        string expected)
    {
        var deadline = new DeadlinePresentation(
            semantic,
            Calendar.Resolve(new GameTime(28 * 1_440)));

        Assert.Equal(expected, GameDeadlineFormatter.FormatDevice(deadline));
    }

    [Fact]
    public void Weekly_rest_device_format_keeps_period_and_short_start_boundary()
    {
        Assert.Equal(
            "4/6 · START≤ PON · D141 · 22:55",
            WeeklyRestWindowFormatter.FormatDevice(5_379, 202_975, Calendar));
    }

    [Fact]
    public void Weekday_mapping_has_one_full_and_compact_value_for_every_day()
    {
        foreach (var weekday in Enum.GetValues<GameWeekday>())
        {
            Assert.False(string.IsNullOrWhiteSpace(GameWeekdayNames.Full(weekday)));
            Assert.False(string.IsNullOrWhiteSpace(GameWeekdayNames.Abbreviated(weekday)));
        }
    }

    [Fact]
    public void Compensation_overview_uses_complete_before_semantic()
    {
        var obligation = Obligation(28 * 1_440);

        var overview = CompensationOverview.From([obligation], Calendar);

        Assert.Equal(
            "Ukończ przed: Pon · Dzień 29 · 00:00",
            overview.NearestDueText);
        Assert.Equal("PON · D29 · 00:00", overview.NearestDueCompactText);
    }

    [Fact]
    public void Compensation_detail_uses_full_exclusive_deadline()
    {
        var obligation = Obligation(28 * 1_440);

        var detail = CompensationDetailRow.From("KARTA", obligation, Calendar);

        Assert.Equal(
            "Ukończ przed: Pon · Dzień 29 · 00:00",
            detail.DueAtText);
    }

    private static WeeklyRestCompensationDto Obligation(long dueAt) => new(
        IdentitySchemeVersion: 1,
        ObligationId: "obligation",
        DriverCardId: "card",
        SourceRestBlockId: "rest",
        SourceRestEndGameMinuteExclusive: 1_440,
        OriginalOwedMinutes: 300,
        RemainingMinutes: 300,
        ReductionWeek: 0,
        DueAtGameMinuteExclusive: dueAt,
        PaymentRestBlockId: null,
        PaymentRange: null,
        SettledAtGameMinute: null,
        Status: WeeklyRestCompensationStatusDto.OpenOnTime);
}

using System.Globalization;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Desktop;

internal static class GameWeekdayNames
{
    public static string Full(GameWeekday weekday) => weekday switch
    {
        GameWeekday.Monday => "Pon",
        GameWeekday.Tuesday => "Wt",
        GameWeekday.Wednesday => "Śr",
        GameWeekday.Thursday => "Czw",
        GameWeekday.Friday => "Pt",
        GameWeekday.Saturday => "Sob",
        GameWeekday.Sunday => "Ndz",
        _ => throw new ArgumentOutOfRangeException(nameof(weekday))
    };

    public static string Abbreviated(GameWeekday weekday) => weekday switch
    {
        GameWeekday.Monday => "PON",
        GameWeekday.Tuesday => "WT",
        GameWeekday.Wednesday => "ŚR",
        GameWeekday.Thursday => "CZW",
        GameWeekday.Friday => "PT",
        GameWeekday.Saturday => "SOB",
        GameWeekday.Sunday => "NDZ",
        _ => throw new ArgumentOutOfRangeException(nameof(weekday))
    };
}

internal static class GameCalendarFormatter
{
    public static string FormatFull(GameCalendarMoment moment) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{GameWeekdayNames.Full(moment.Weekday)} · Dzień {moment.DisplayedGameDay} · {moment.Hour:00}:{moment.Minute:00}");

    public static string FormatCompact(GameCalendarMoment moment) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{GameWeekdayNames.Abbreviated(moment.Weekday)} · D{moment.DisplayedGameDay} · {moment.Hour:00}:{moment.Minute:00}");
}

internal readonly record struct DeadlinePresentation(
    GameDeadlineSemantic Semantic,
    GameCalendarMoment Moment);

internal static class GameDeadlineFormatter
{
    public static string FormatFull(DeadlinePresentation deadline) =>
        $"{Prefix(deadline.Semantic)}: {GameCalendarFormatter.FormatFull(deadline.Moment)}";

    public static string FormatCompact(DeadlinePresentation deadline) =>
        $"{Prefix(deadline.Semantic)}: {GameCalendarFormatter.FormatCompact(deadline.Moment)}";

    public static string FormatDevice(DeadlinePresentation deadline) =>
        $"{DevicePrefix(deadline.Semantic)} {GameWeekdayNames.Abbreviated(deadline.Moment.Weekday)} · " +
        $"D{deadline.Moment.DisplayedGameDay} · {deadline.Moment.Hour:00}:{deadline.Moment.Minute:00}";

    private static string Prefix(GameDeadlineSemantic semantic) => semantic switch
    {
        GameDeadlineSemantic.CompleteBy => "Ukończ do",
        GameDeadlineSemantic.StartNoLaterThan => "Rozpocznij najpóźniej",
        GameDeadlineSemantic.CompleteBefore => "Ukończ przed",
        GameDeadlineSemantic.AvailableFrom => "Jazda dostępna od",
        _ => throw new ArgumentOutOfRangeException(nameof(semantic))
    };

    private static string DevicePrefix(GameDeadlineSemantic semantic) => semantic switch
    {
        GameDeadlineSemantic.CompleteBy => "KONIEC≤",
        GameDeadlineSemantic.StartNoLaterThan => "START≤",
        GameDeadlineSemantic.CompleteBefore => "PRZED",
        GameDeadlineSemantic.AvailableFrom => "OD",
        _ => throw new ArgumentOutOfRangeException(nameof(semantic))
    };
}

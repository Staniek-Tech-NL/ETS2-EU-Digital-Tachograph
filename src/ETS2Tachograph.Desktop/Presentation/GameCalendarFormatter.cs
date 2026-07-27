using System.Globalization;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Desktop;

internal static class GameWeekdayNames
{
    public static string Full(GameWeekday weekday) => weekday switch
    {
        GameWeekday.Monday => Localization.UiStrings.Get("Weekday_Display_Monday"),
        GameWeekday.Tuesday => Localization.UiStrings.Get("Weekday_Display_Tuesday"),
        GameWeekday.Wednesday => Localization.UiStrings.Get("Weekday_Display_Wednesday"),
        GameWeekday.Thursday => Localization.UiStrings.Get("Weekday_Display_Thursday"),
        GameWeekday.Friday => Localization.UiStrings.Get("Weekday_Display_Friday"),
        GameWeekday.Saturday => Localization.UiStrings.Get("Weekday_Display_Saturday"),
        GameWeekday.Sunday => Localization.UiStrings.Get("Weekday_Display_Sunday"),
        _ => throw new ArgumentOutOfRangeException(nameof(weekday))
    };

    public static string Abbreviated(GameWeekday weekday) => weekday switch
    {
        GameWeekday.Monday => Localization.UiStrings.Get("Weekday_Short_Monday"),
        GameWeekday.Tuesday => Localization.UiStrings.Get("Weekday_Short_Tuesday"),
        GameWeekday.Wednesday => Localization.UiStrings.Get("Weekday_Short_Wednesday"),
        GameWeekday.Thursday => Localization.UiStrings.Get("Weekday_Short_Thursday"),
        GameWeekday.Friday => Localization.UiStrings.Get("Weekday_Short_Friday"),
        GameWeekday.Saturday => Localization.UiStrings.Get("Weekday_Short_Saturday"),
        GameWeekday.Sunday => Localization.UiStrings.Get("Weekday_Short_Sunday"),
        _ => throw new ArgumentOutOfRangeException(nameof(weekday))
    };
}

internal static class GameCalendarFormatter
{
    public static string FormatFull(GameCalendarMoment moment) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{GameWeekdayNames.Full(moment.Weekday)} · " +
            $"{Localization.UiStrings.Format("GameCalendar_DayFormat", moment.DisplayedGameDay)} · " +
            $"{moment.Hour:00}:{moment.Minute:00}");

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
        GameDeadlineSemantic.CompleteBy => Localization.UiStrings.Get("Deadline_CompleteByPrefix"),
        GameDeadlineSemantic.StartNoLaterThan => Localization.UiStrings.Get("Deadline_StartNoLaterThanPrefix"),
        GameDeadlineSemantic.CompleteBefore =>
            Localization.UiStrings.Get("Deadline_CompleteBeforePrefix"),
        GameDeadlineSemantic.AvailableFrom => Localization.UiStrings.Get("Deadline_AvailableFromPrefix"),
        _ => throw new ArgumentOutOfRangeException(nameof(semantic))
    };

    private static string DevicePrefix(GameDeadlineSemantic semantic) => semantic switch
    {
        GameDeadlineSemantic.CompleteBy => Localization.UiStrings.Get("DeviceDeadline_CompleteByPrefix"),
        GameDeadlineSemantic.StartNoLaterThan => Localization.UiStrings.Get("DeviceDeadline_StartNoLaterThanPrefix"),
        GameDeadlineSemantic.CompleteBefore => Localization.UiStrings.Get("DeviceDeadline_CompleteBeforePrefix"),
        GameDeadlineSemantic.AvailableFrom =>
            Localization.UiStrings.Get("DeviceDeadline_AvailableFromPrefix"),
        _ => throw new ArgumentOutOfRangeException(nameof(semantic))
    };
}

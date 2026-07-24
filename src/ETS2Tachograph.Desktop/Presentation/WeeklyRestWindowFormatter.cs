using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Desktop;

internal static class WeeklyRestWindowFormatter
{
    private const long MinutesPerPeriod = 24 * 60;
    private const long WindowMinutes = 6 * MinutesPerPeriod;

    public static string Format(
        long? elapsedMinutes,
        long? deadlineGameMinute,
        GameCalendarResolver calendar)
    {
        var periodText = FormatPeriod(elapsedMinutes);
        if (deadlineGameMinute is null or < 0)
            return $"{periodText} (—)";

        var deadline = new DeadlinePresentation(
            GameDeadlineSemantic.StartNoLaterThan,
            calendar.Resolve(new GameTime(deadlineGameMinute.Value)));
        return $"{periodText} · {GameDeadlineFormatter.FormatCompact(deadline)}";
    }

    public static string FormatDevice(
        long? elapsedMinutes,
        long? deadlineGameMinute,
        GameCalendarResolver calendar)
    {
        var periodText = FormatPeriod(elapsedMinutes);
        if (deadlineGameMinute is null or < 0)
            return $"{periodText} (—)";

        var deadline = new DeadlinePresentation(
            GameDeadlineSemantic.StartNoLaterThan,
            calendar.Resolve(new GameTime(deadlineGameMinute.Value)));
        return $"{periodText} · {GameDeadlineFormatter.FormatDevice(deadline)}";
    }

    private static string FormatPeriod(long? elapsedMinutes)
    {
        if (elapsedMinutes is null)
            return "—/6";

        var elapsed = Math.Max(0, elapsedMinutes.Value);
        if (elapsed > WindowMinutes)
            return "6/6+";

        var currentPeriod = Math.Min(elapsed / MinutesPerPeriod + 1, 6);
        return $"{currentPeriod}/6";
    }
}

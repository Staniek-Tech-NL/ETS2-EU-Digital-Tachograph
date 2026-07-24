using System.Globalization;

namespace ETS2Tachograph.Desktop;

internal static class WeeklyRestWindowFormatter
{
    private const long MinutesPerPeriod = 24 * 60;
    private const long WindowMinutes = 6 * MinutesPerPeriod;

    public static string Format(
        long? elapsedMinutes,
        long? deadlineGameMinute)
    {
        var periodText = FormatPeriod(elapsedMinutes);
        if (deadlineGameMinute is null or < 0)
            return $"{periodText} (—)";

        var displayedDay = deadlineGameMinute.Value / MinutesPerPeriod + 1;
        var minuteOfDay = deadlineGameMinute.Value % MinutesPerPeriod;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{periodText} (D{displayedDay} {minuteOfDay / 60:00}:{minuteOfDay % 60:00})");
    }

    private static string FormatPeriod(long? elapsedMinutes)
    {
        if (elapsedMinutes is null)
            return "—/6";

        var elapsed = Math.Max(0, elapsedMinutes.Value);
        if (elapsed > WindowMinutes)
            return "6/6+";

        var currentPeriod = Math.Min(elapsed / MinutesPerPeriod + 1, 6);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{currentPeriod}/6");
    }
}

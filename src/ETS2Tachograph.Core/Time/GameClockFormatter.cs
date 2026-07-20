using System.Globalization;
using System.Text.RegularExpressions;

namespace ETS2Tachograph.Core.Time;

/// <summary>Formats the absolute ETS2 game clock without exposing raw telemetry minutes.</summary>
public static partial class GameClockFormatter
{
    public const int MinutesPerDay = 24 * 60;

    public static string Format(GameTime time)
    {
        var day = (time.TotalMinutes / MinutesPerDay) + 1;
        var minuteOfDay = time.TotalMinutes % MinutesPerDay;
        return $"Dzień {day}, {minuteOfDay / 60:00}:{minuteOfDay % 60:00}";
    }

    /// <summary>Formats only the in-game clock portion for compact tachograph displays.</summary>
    public static string FormatTimeOfDay(GameTime time)
    {
        var minuteOfDay = time.TotalMinutes % MinutesPerDay;
        return $"{minuteOfDay / 60:00}:{minuteOfDay % 60:00}";
    }

    public static bool TryParse(string? text, out GameTime time)
    {
        time = default;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var value = text.Trim();
        if (long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var rawMinutes) && rawMinutes >= 0)
        {
            time = new GameTime(rawMinutes);
            return true;
        }

        var match = GameClockPattern().Match(value);
        if (!match.Success ||
            !long.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var day) ||
            !int.TryParse(match.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var hour) ||
            !int.TryParse(match.Groups[3].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var minute) ||
            day < 1 || hour is < 0 or > 23 || minute is < 0 or > 59)
            return false;

        time = new GameTime(checked(((day - 1) * MinutesPerDay) + (hour * 60) + minute));
        return true;
    }

    [GeneratedRegex(@"^(?:D(?:ZIEŃ)?\s*)?(\d+)\s*[,.-]?\s+(\d{1,2}):(\d{2})$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex GameClockPattern();
}

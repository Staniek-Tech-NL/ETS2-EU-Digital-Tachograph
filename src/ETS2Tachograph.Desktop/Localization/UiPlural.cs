namespace ETS2Tachograph.Desktop.Localization;

internal static class UiPlural
{
    public static string Select(long count, string one, string few, string many)
    {
        var absolute = Math.Abs(count);
        if (absolute == 1)
            return one;

        var lastDigit = absolute % 10;
        var lastTwoDigits = absolute % 100;
        return lastDigit is >= 2 and <= 4 &&
               lastTwoDigits is not (>= 12 and <= 14)
            ? few
            : many;
    }
}

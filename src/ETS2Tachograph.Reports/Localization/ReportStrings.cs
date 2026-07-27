using System.Globalization;
using System.Resources;

namespace ETS2Tachograph.Reports.Localization;

public static class ReportStrings
{
    private static readonly ResourceManager ResourceManager = new(
        "ETS2Tachograph.Reports.Resources.ReportStrings",
        typeof(ReportStrings).Assembly);

    public static string Get(string key, CultureInfo? culture = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var selectedCulture = culture ?? CultureInfo.CurrentUICulture;
        return ResourceManager.GetString(key, selectedCulture)
               ?? throw new MissingManifestResourceException(
                   $"The report resource '{key}' is missing for culture '{selectedCulture.Name}'.");
    }

    public static string Format(
        string key,
        CultureInfo culture,
        params object?[] arguments) =>
        string.Format(culture, Get(key, culture), arguments);
}

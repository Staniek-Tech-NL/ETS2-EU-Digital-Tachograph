using System.Globalization;
using System.Resources;

namespace ETS2Tachograph.Desktop.Localization;

public static class UiStrings
{
    private static readonly ResourceManager ResourceManager = new(
        "ETS2Tachograph.Desktop.Resources.UiStrings",
        typeof(UiStrings).Assembly);

    public static string Get(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return ResourceManager.GetString(key, UiCulture.Current)
               ?? throw new MissingManifestResourceException(
                   $"The UI resource '{key}' is missing for culture '{UiCulture.Current.Name}'.");
    }

    public static string Format(string key, params object?[] arguments) =>
        string.Format(UiCulture.Current, Get(key), arguments);
}

using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace ETS2Tachograph.Desktop.Localization;

public static class UiCulture
{
    public const string Polish = "pl-PL";
    public const string EnglishUnitedKingdom = "en-GB";

    private static readonly object WpfMetadataSync = new();
    private static bool _wpfMetadataApplied;

    public static IReadOnlyList<string> SupportedNames { get; } =
        [Polish, EnglishUnitedKingdom];

    public static bool TryNormalize(string? cultureName, out string normalized)
    {
        normalized = SupportedNames.FirstOrDefault(candidate =>
            string.Equals(candidate, cultureName, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
        return normalized.Length > 0;
    }

    public static string Normalize(string? cultureName)
    {
        if (TryNormalize(cultureName, out var normalized))
            return normalized;

        throw new ArgumentOutOfRangeException(
            nameof(cultureName),
            cultureName,
            "Only pl-PL and en-GB UI cultures are supported.");
    }

    public static void Apply(string cultureName, bool applyWpfLanguage = true)
    {
        var culture = CultureInfo.GetCultureInfo(Normalize(cultureName));
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        if (!applyWpfLanguage)
            return;

        lock (WpfMetadataSync)
        {
            if (_wpfMetadataApplied)
                return;

            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));
            _wpfMetadataApplied = true;
        }
    }
}

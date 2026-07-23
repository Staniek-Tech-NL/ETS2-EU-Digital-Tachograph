using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace ETS2Tachograph.Desktop;

public sealed record CountryOption(
    string IsoAlpha2,
    string DisplayName,
    string TachographCode,
    int TachographNumericCode,
    string RegionFallback)
{
    public string DisplayText => $"{IsoAlpha2} — {DisplayName}";
}

public static class CountryCatalog
{
    private const int ExpectedIsoCountryCount = 249;
    private static readonly IReadOnlyList<CountryOption> LoadedOptions = Load();
    private static readonly IReadOnlyDictionary<string, CountryOption> ByIso =
        LoadedOptions.ToDictionary(country => country.IsoAlpha2, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<CountryOption> Options => LoadedOptions;

    public static CountryOption? FindByIso(string? isoAlpha2) =>
        string.IsNullOrWhiteSpace(isoAlpha2)
            ? null
            : ByIso.GetValueOrDefault(isoAlpha2.Trim());

    public static CountryOption? ResolveLegacyCode(string? storedCode)
    {
        if (string.IsNullOrWhiteSpace(storedCode)) return null;

        var normalized = storedCode.Trim();
        if (FindByIso(normalized) is { } isoMatch) return isoMatch;

        var tachographMatches = LoadedOptions
            .Where(country => string.Equals(
                country.TachographCode,
                normalized,
                StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToList();
        return tachographMatches.Count == 1 ? tachographMatches[0] : null;
    }

    private static IReadOnlyList<CountryOption> Load()
    {
        using var catalog = LoadJson("Data.Countries.iso3166-1.json");
        using var names = LoadJson("Resources.CountryNames.pl.json");

        var localizedNames = names.RootElement.GetProperty("names")
            .EnumerateObject()
            .ToDictionary(
                property => property.Name,
                property => property.Value.GetString()
                            ?? throw new InvalidDataException($"Brak nazwy dla {property.Name}."),
                StringComparer.Ordinal);

        var options = new List<CountryOption>();
        foreach (var row in catalog.RootElement.GetProperty("countries").EnumerateArray())
        {
            if (row.GetArrayLength() != 5)
                throw new InvalidDataException("Niepoprawny rekord katalogu krajów.");

            var isoAlpha2 = RequiredString(row[0], "isoAlpha2");
            var nameKey = RequiredString(row[1], "nameKey");
            var tachographCode = RequiredString(row[2], "tachographCode");
            var tachographNumericCode = row[3].GetInt32();
            var regionFallback = RequiredString(row[4], "regionFallback");

            if (isoAlpha2.Length != 2 || isoAlpha2.Any(character => character is < 'A' or > 'Z'))
                throw new InvalidDataException($"Niepoprawny kod ISO: {isoAlpha2}.");
            if (tachographNumericCode is < 0 or > 255)
                throw new InvalidDataException($"Niepoprawny kod numeryczny tachografu: {isoAlpha2}.");
            if (!localizedNames.TryGetValue(nameKey, out var displayName))
                throw new InvalidDataException($"Brak zasobu {nameKey}.");

            options.Add(new CountryOption(
                isoAlpha2,
                displayName,
                tachographCode,
                tachographNumericCode,
                regionFallback));
        }

        if (options.Count != ExpectedIsoCountryCount ||
            options.Select(country => country.IsoAlpha2)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() != ExpectedIsoCountryCount)
        {
            throw new InvalidDataException(
                $"Katalog ISO musi zawierać {ExpectedIsoCountryCount} unikalnych wpisów.");
        }

        var polish = CultureInfo.GetCultureInfo("pl-PL").CompareInfo;
        options.Sort((left, right) =>
        {
            var byName = polish.Compare(
                left.DisplayName,
                right.DisplayName,
                CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace);
            return byName != 0
                ? byName
                : StringComparer.Ordinal.Compare(left.IsoAlpha2, right.IsoAlpha2);
        });
        return options;
    }

    private static JsonDocument LoadJson(string resourceSuffix)
    {
        var assembly = typeof(CountryCatalog).Assembly;
        var resourceName = assembly.GetManifestResourceNames().SingleOrDefault(name =>
            name.EndsWith(resourceSuffix, StringComparison.Ordinal));
        if (resourceName is null)
            throw new InvalidDataException($"Nie znaleziono zasobu {resourceSuffix}.");

        using var stream = assembly.GetManifestResourceStream(resourceName)
                           ?? throw new InvalidDataException($"Nie można odczytać zasobu {resourceName}.");
        return JsonDocument.Parse(stream);
    }

    private static string RequiredString(JsonElement element, string fieldName) =>
        element.GetString() is { Length: > 0 } value
            ? value
            : throw new InvalidDataException($"Brak wartości {fieldName}.");
}

using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Text.Json;
using System.Text.RegularExpressions;
using ETS2Tachograph.Desktop.Localization;
using ETS2Tachograph.Reports.Localization;

namespace ETS2Tachograph.Desktop.Tests;

public sealed partial class LocalizationFoundationTests
{
    private static readonly CultureInfo Polish = CultureInfo.GetCultureInfo(UiCulture.Polish);
    private static readonly CultureInfo English =
        CultureInfo.GetCultureInfo(UiCulture.EnglishUnitedKingdom);

    [Fact]
    public void Ui_resources_have_complete_matching_non_empty_contract()
    {
        var polish = LoadResources(
            typeof(UiStrings).Assembly,
            "ETS2Tachograph.Desktop.Resources.UiStrings",
            Polish);
        var english = LoadResources(
            typeof(UiStrings).Assembly,
            "ETS2Tachograph.Desktop.Resources.UiStrings",
            English);

        Assert.Equal(626, polish.Count);
        Assert.Equal(polish.Keys.Order(), english.Keys.Order());
        Assert.All(polish.Values, value => Assert.False(string.IsNullOrWhiteSpace(value)));
        Assert.All(english.Values, value => Assert.False(string.IsNullOrWhiteSpace(value)));
        AssertMatchingPlaceholders(polish, english);
    }

    [Fact]
    public void Report_resources_have_complete_matching_non_empty_contract()
    {
        var polish = LoadResources(
            typeof(ReportStrings).Assembly,
            "ETS2Tachograph.Reports.Resources.ReportStrings",
            Polish);
        var english = LoadResources(
            typeof(ReportStrings).Assembly,
            "ETS2Tachograph.Reports.Resources.ReportStrings",
            English);

        Assert.Equal(99, polish.Count);
        Assert.Equal(polish.Keys.Order(), english.Keys.Order());
        Assert.All(polish.Values, value => Assert.False(string.IsNullOrWhiteSpace(value)));
        Assert.All(english.Values, value => Assert.False(string.IsNullOrWhiteSpace(value)));
        AssertMatchingPlaceholders(polish, english);
    }

    [Fact]
    public void Report_bridge_matches_ui_resources_in_both_languages()
    {
        string[] bridgeKeys =
        [
            "Compensation_Title",
            "Activity_Rest",
            "Activity_Driving",
            "Activity_OtherWork",
            "Activity_BreakOrRest",
            "Activity_Unknown",
            "ReportCompensationStatus_OpenOnTime",
            "ReportCompensationStatus_Overdue",
            "ReportCompensationStatus_PaidOnTime",
            "ReportCompensationStatus_PaidLate",
            "RestAllocationPurpose_DailyRestWithCompensation",
            "RestAllocationPurpose_ReducedWeeklyRestOnly",
            "RestAllocationPurpose_ReducedWeeklyRestWithCompensation",
            "RestAllocationPurpose_RegularWeeklyRestOnly",
            "RestAllocationPurpose_RegularWeeklyRestWithCompensation",
            "ActivitySource_Telemetry",
            "ActivitySource_Mixed",
            "ActivitySource_ManualEntry",
            "PlannerTime_FromPrefix",
            "PlannerTime_ToPrefix",
            "ReportActivity_SourceFormat",
            "Common_NoData"
        ];

        foreach (var culture in new[] { Polish, English })
        {
            var ui = LoadResources(
                typeof(UiStrings).Assembly,
                "ETS2Tachograph.Desktop.Resources.UiStrings",
                culture);
            var reports = LoadResources(
                typeof(ReportStrings).Assembly,
                "ETS2Tachograph.Reports.Resources.ReportStrings",
                culture);

            Assert.Equal(22, bridgeKeys.Length);
            Assert.All(bridgeKeys, key => Assert.Equal(ui[key], reports[key]));
        }
    }

    [Fact]
    public void Country_name_stores_match_the_iso_catalog()
    {
        var assembly = typeof(UiStrings).Assembly;
        using var iso = LoadJson(assembly, "Data.Countries.iso3166-1.json");
        using var polish = LoadJson(assembly, "Resources.CountryNames.pl.json");
        using var english = LoadJson(assembly, "Resources.CountryNames.en-GB.json");

        var expectedKeys = iso.RootElement.GetProperty("countries")
            .EnumerateArray()
            .Select(row => row[1].GetString())
            .Order(StringComparer.Ordinal)
            .ToArray();
        var polishKeys = polish.RootElement.GetProperty("names")
            .EnumerateObject()
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var englishKeys = english.RootElement.GetProperty("names")
            .EnumerateObject()
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(249, expectedKeys.Length);
        Assert.Equal(expectedKeys, polishKeys);
        Assert.Equal(expectedKeys, englishKeys);
        Assert.All(
            english.RootElement.GetProperty("names").EnumerateObject(),
            property => Assert.False(string.IsNullOrWhiteSpace(property.Value.GetString())));
    }

    [Theory]
    [InlineData("pl-PL", "pl-PL")]
    [InlineData("PL-pl", "pl-PL")]
    [InlineData("en-GB", "en-GB")]
    [InlineData("EN-gb", "en-GB")]
    public void Supported_culture_names_are_normalized(string input, string expected)
    {
        Assert.Equal(expected, UiCulture.Normalize(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    public void Unsupported_culture_names_are_rejected(string input)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => UiCulture.Normalize(input));
    }

    private static Dictionary<string, string> LoadResources(
        Assembly assembly,
        string baseName,
        CultureInfo culture)
    {
        var manager = new ResourceManager(baseName, assembly);
        var resourceSet = manager.GetResourceSet(culture, true, true)
                          ?? throw new MissingManifestResourceException(baseName);
        return resourceSet.Cast<DictionaryEntry>().ToDictionary(
            entry => Assert.IsType<string>(entry.Key),
            entry => Assert.IsType<string>(entry.Value),
            StringComparer.Ordinal);
    }

    private static void AssertMatchingPlaceholders(
        IReadOnlyDictionary<string, string> polish,
        IReadOnlyDictionary<string, string> english)
    {
        foreach (var key in polish.Keys)
        {
            var polishPlaceholders = PlaceholderPattern().Matches(polish[key])
                .Select(match => match.Value)
                .ToArray();
            var englishPlaceholders = PlaceholderPattern().Matches(english[key])
                .Select(match => match.Value)
                .ToArray();
            Assert.Equal(polishPlaceholders, englishPlaceholders);
        }
    }

    private static JsonDocument LoadJson(Assembly assembly, string suffix)
    {
        var resourceName = assembly.GetManifestResourceNames().Single(name =>
            name.EndsWith(suffix, StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resourceName)
                           ?? throw new MissingManifestResourceException(resourceName);
        return JsonDocument.Parse(stream);
    }

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex PlaceholderPattern();
}

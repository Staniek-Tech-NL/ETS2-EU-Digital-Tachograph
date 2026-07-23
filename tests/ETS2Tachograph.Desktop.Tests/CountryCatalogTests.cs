using ETS2Tachograph.Desktop;

namespace ETS2Tachograph.Desktop.Tests;

public sealed class CountryCatalogTests
{
    [Fact]
    public void Catalog_contains_full_unique_iso_alpha2_set()
    {
        Assert.Equal(249, CountryCatalog.Options.Count);
        Assert.Equal(
            249,
            CountryCatalog.Options
                .Select(country => country.IsoAlpha2)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count());
        Assert.All(CountryCatalog.Options, country =>
        {
            Assert.Matches("^[A-Z]{2}$", country.IsoAlpha2);
            Assert.False(string.IsNullOrWhiteSpace(country.DisplayName));
            Assert.InRange(country.TachographNumericCode, 0, 255);
        });
    }

    [Theory]
    [InlineData("DE", "Niemcy", "D", 13)]
    [InlineData("ES", "Hiszpania", "E", 15)]
    [InlineData("PL", "Polska", "PL", 40)]
    [InlineData("EG", "Egipt", "WLD", 255)]
    [InlineData("AX", "Wyspy Alandzkie", "EUR", 254)]
    public void Catalog_separates_iso_and_tachograph_codes(
        string isoAlpha2,
        string displayName,
        string tachographCode,
        int tachographNumericCode)
    {
        var country = Assert.IsType<CountryOption>(CountryCatalog.FindByIso(isoAlpha2));

        Assert.Equal(displayName, country.DisplayName);
        Assert.Equal(tachographCode, country.TachographCode);
        Assert.Equal(tachographNumericCode, country.TachographNumericCode);
    }

    [Theory]
    [InlineData("PL", "PL")]
    [InlineData("D", "DE")]
    [InlineData("F", "FR")]
    [InlineData("E", "ES")]
    [InlineData("UK", "GB")]
    public void Legacy_reader_maps_unambiguous_old_codes(string storedCode, string expectedIso)
    {
        Assert.Equal(expectedIso, CountryCatalog.ResolveLegacyCode(storedCode)?.IsoAlpha2);
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("WLD")]
    [InlineData("dowolny tekst")]
    public void Legacy_reader_does_not_invent_country_for_ambiguous_or_invalid_value(string storedCode)
    {
        Assert.Null(CountryCatalog.ResolveLegacyCode(storedCode));
    }
}

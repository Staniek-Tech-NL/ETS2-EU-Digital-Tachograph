using System.Text.Json;
using ETS2Tachograph.Desktop.Localization;

namespace ETS2Tachograph.Desktop.Tests;

public sealed class UiCulturePreferenceStoreTests
{
    [Fact]
    public void Missing_preference_preserves_polish_for_existing_installations()
    {
        using var folder = new TemporaryFolder();
        var result = new JsonUiCulturePreferenceStore(folder.File("ui-culture.json")).Load();

        Assert.Equal(UiCulture.Polish, result.CultureName);
        Assert.False(result.UsedFallback);
        Assert.Null(result.DiagnosticReason);
    }

    [Theory]
    [InlineData("pl-PL")]
    [InlineData("en-GB")]
    public void Preference_round_trips_using_the_technical_culture_name(string cultureName)
    {
        using var folder = new TemporaryFolder();
        var path = folder.File("ui-culture.json");
        var store = new JsonUiCulturePreferenceStore(path);

        store.Save(cultureName == UiCulture.Polish
            ? UiCulture.EnglishUnitedKingdom
            : UiCulture.Polish);
        store.Save(cultureName);
        var result = store.Load();

        Assert.Equal(cultureName, result.CultureName);
        Assert.False(result.UsedFallback);
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Equal(1, document.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(cultureName, document.RootElement.GetProperty("cultureName").GetString());
        Assert.Empty(Directory.GetFiles(folder.Path, "*.tmp"));
    }

    [Theory]
    [InlineData("""{"schemaVersion":1,"cultureName":"en-US"}""")]
    [InlineData("""{"schemaVersion":2,"cultureName":"pl-PL"}""")]
    [InlineData("""{"schemaVersion":1,"cultureName":""}""")]
    [InlineData("""not-json""")]
    public void Invalid_or_damaged_preference_falls_back_to_english(string content)
    {
        using var folder = new TemporaryFolder();
        var path = folder.File("ui-culture.json");
        File.WriteAllText(path, content);

        var result = new JsonUiCulturePreferenceStore(path).Load();

        Assert.Equal(UiCulture.EnglishUnitedKingdom, result.CultureName);
        Assert.True(result.UsedFallback);
        Assert.False(string.IsNullOrWhiteSpace(result.DiagnosticReason));
    }

    [Fact]
    public void Unsupported_culture_is_not_written()
    {
        using var folder = new TemporaryFolder();
        var path = folder.File("ui-culture.json");
        var store = new JsonUiCulturePreferenceStore(path);

        Assert.Throws<ArgumentOutOfRangeException>(() => store.Save("en-US"));
        Assert.False(File.Exists(path));
    }

    private sealed class TemporaryFolder : IDisposable
    {
        public TemporaryFolder()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"ets2-tachograph-culture-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string File(string name) => System.IO.Path.Combine(Path, name);

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}

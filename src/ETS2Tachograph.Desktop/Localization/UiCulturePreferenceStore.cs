using System.IO;
using System.Text;
using System.Text.Json;

namespace ETS2Tachograph.Desktop.Localization;

public sealed record UiCulturePreferenceLoadResult(
    string CultureName,
    bool UsedFallback,
    string? DiagnosticReason);

public interface IUiCulturePreferenceStore
{
    UiCulturePreferenceLoadResult Load();
    void Save(string cultureName);
}

public sealed class JsonUiCulturePreferenceStore(string path) : IUiCulturePreferenceStore
{
    private const int SchemaVersion = 1;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static JsonUiCulturePreferenceStore CreateDefault() => new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ETS2Tachograph",
        "ui-culture.json"));

    public UiCulturePreferenceLoadResult Load()
    {
        if (!File.Exists(path))
            return new UiCulturePreferenceLoadResult(UiCulture.Polish, false, null);

        try
        {
            var payload = JsonSerializer.Deserialize<UiCulturePreferencePayload>(
                File.ReadAllText(path, Encoding.UTF8),
                SerializerOptions);
            if (payload is not null &&
                payload.SchemaVersion == SchemaVersion &&
                UiCulture.TryNormalize(payload.CultureName, out var cultureName))
            {
                return new UiCulturePreferenceLoadResult(cultureName, false, null);
            }

            return Fallback("Unsupported schema or UI culture.");
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return Fallback($"{exception.GetType().Name}: {exception.Message}");
        }
    }

    public void Save(string cultureName)
    {
        var normalizedCultureName = UiCulture.Normalize(cultureName);
        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directory))
            throw new InvalidOperationException("The UI culture preference path has no directory.");

        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            var json = JsonSerializer.Serialize(
                new UiCulturePreferencePayload(SchemaVersion, normalizedCultureName),
                SerializerOptions);
            File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));

            if (File.Exists(path))
                File.Replace(temporaryPath, path, null);
            else
                File.Move(temporaryPath, path);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private static UiCulturePreferenceLoadResult Fallback(string reason) =>
        new(UiCulture.EnglishUnitedKingdom, true, reason);

    private sealed record UiCulturePreferencePayload(int SchemaVersion, string CultureName);
}

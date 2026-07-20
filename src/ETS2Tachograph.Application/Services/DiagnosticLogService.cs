using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Core.Entities;

namespace ETS2Tachograph.Application.Services;

public sealed record DiagnosticSnapshot(
    string TelemetryStatus,
    string GameTime,
    string Speed,
    string Slot1,
    string Slot1Activity,
    string Slot1Counters,
    string Slot2,
    string Slot2Activity,
    string Slot2Counters,
    string Modes,
    int HistoryRecords,
    int ActiveViolations);

public sealed class DiagnosticLogService : IActivityPersistenceDiagnostics, IManualEntryDiagnostics
{
    private const int RetentionDays = 14;
    private readonly object _sync = new();
    private readonly string _logsFolder;

    public DiagnosticLogService(string? logsFolder = null)
    {
        _logsFolder = logsFolder ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ETS2Tachograph", "Logs");
        Directory.CreateDirectory(_logsFolder);
        DeleteExpiredLogs();
    }

    public string CurrentLogPath => Path.Combine(_logsFolder, $"tachograph-{DateTime.Now:yyyy-MM-dd}.log");

    public void Info(string eventName, string message) => Write("INFO", eventName, message);
    public void Warning(string eventName, string message) => Write("WARN", eventName, message);
    public void Error(string eventName, Exception exception) =>
        Write("ERROR", eventName, $"{exception.GetType().Name}: {exception.Message}{Environment.NewLine}{exception.StackTrace}");

    public void RecordConflict(
        string driverCardId,
        int sessionIndex,
        ActivityRecord existing,
        ActivityRecord incoming) => Warning(
            "ACTIVITY_RECORD_CONFLICT",
            $"Karta={driverCardId}, sesja={sessionIndex}, minuta={incoming.Start.TotalMinutes}; " +
            $"zachowano: {Describe(existing)}; odrzucono: {Describe(incoming)}.");

    public void RecordResolutionConflict(
        Guid gapId,
        IReadOnlyList<ActivityRecord> existing,
        IReadOnlyList<ActivityRecord> incoming) => Warning(
        "MANUAL_ENTRY_CONFLICT",
        $"Luka={gapId}; zachowano segmenty: {Describe(existing)}; " +
        $"odrzucono segmenty: {Describe(incoming)}.");

    public async Task CreateReportAsync(
        Stream destination,
        DiagnosticSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        Info("DIAGNOSTIC_REPORT", "Rozpoczęto tworzenie raportu diagnostycznego.");
        using var archive = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true);
        var summaryEntry = archive.CreateEntry("diagnostic-summary.txt", CompressionLevel.Optimal);
        await using (var summaryStream = summaryEntry.Open())
        await using (var writer = new StreamWriter(summaryStream, new UTF8Encoding(true)))
        {
            await writer.WriteLineAsync("ETS2 DIGITAL TACHOGRAPH - RAPORT DIAGNOSTYCZNY");
            await writer.WriteLineAsync($"Utworzono: {DateTimeOffset.Now:O}");
            await writer.WriteLineAsync($"Wersja: {Assembly.GetEntryAssembly()?.GetName().Version}");
            await writer.WriteLineAsync($"System: {RuntimeInformation.OSDescription}");
            await writer.WriteLineAsync($"Środowisko: {RuntimeInformation.FrameworkDescription} / {RuntimeInformation.ProcessArchitecture}");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync($"Telemetria: {snapshot.TelemetryStatus}");
            await writer.WriteLineAsync($"Czas gry: {snapshot.GameTime}");
            await writer.WriteLineAsync($"Prędkość: {snapshot.Speed}");
            await writer.WriteLineAsync($"Slot 1: {snapshot.Slot1}");
            await writer.WriteLineAsync($"Aktywność slotu 1: {snapshot.Slot1Activity}");
            await writer.WriteLineAsync($"Liczniki slotu 1: {snapshot.Slot1Counters}");
            await writer.WriteLineAsync($"Slot 2: {snapshot.Slot2}");
            await writer.WriteLineAsync($"Aktywność slotu 2: {snapshot.Slot2Activity}");
            await writer.WriteLineAsync($"Liczniki slotu 2: {snapshot.Slot2Counters}");
            await writer.WriteLineAsync($"Tryby: {snapshot.Modes}");
            await writer.WriteLineAsync($"Rekordy historii: {snapshot.HistoryRecords}");
            await writer.WriteLineAsync($"Aktywne naruszenia: {snapshot.ActiveViolations}");
            await writer.FlushAsync(cancellationToken);
        }

        string[] logFiles;
        lock (_sync)
            logFiles = Directory.GetFiles(_logsFolder, "tachograph-*.log");

        foreach (var logFile in logFiles.OrderBy(file => Path.GetFileName(file)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entry = archive.CreateEntry($"logs/{Path.GetFileName(logFile)}", CompressionLevel.Optimal);
            await using var entryStream = entry.Open();
            await using var source = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            await source.CopyToAsync(entryStream, cancellationToken);
        }
    }

    private void Write(string level, string eventName, string message)
    {
        var cleanMessage = message.Replace("\r\n", " | ").Replace('\r', ' ').Replace('\n', ' ');
        var line = $"{DateTimeOffset.Now:O} | {level,-5} | {eventName} | {cleanMessage}{Environment.NewLine}";
        try
        {
            lock (_sync)
                File.AppendAllText(CurrentLogPath, line, Encoding.UTF8);
        }
        catch
        {
            // Logging must never stop the tachograph.
        }
    }

    private static string Describe(ActivityRecord record) =>
        $"id={record.Id}, aktywność={record.Activity}, koniec={record.EndExclusive.TotalMinutes}, " +
        $"źródło={record.Source}, warunek={record.Condition}";

    private static string Describe(IReadOnlyList<ActivityRecord> records) =>
        string.Join(", ", records.OrderBy(record => record.Start).Select(record =>
            $"[{record.Start.TotalMinutes},{record.EndExclusive.TotalMinutes})={record.Activity}"));

    private void DeleteExpiredLogs()
    {
        try
        {
            var threshold = DateTime.UtcNow.AddDays(-RetentionDays);
            foreach (var file in Directory.GetFiles(_logsFolder, "tachograph-*.log"))
                if (File.GetLastWriteTimeUtc(file) < threshold)
                    File.Delete(file);
        }
        catch
        {
            // Old logs can be cleaned on a later start.
        }
    }
}

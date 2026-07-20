using System.IO.Compression;
using ETS2Tachograph.Application.Services;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Application.Tests;

public sealed class DiagnosticLogServiceTests
{
    [Fact]
    public async Task Diagnostic_report_contains_summary_and_application_log()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"ets2-tacho-log-test-{Guid.NewGuid():N}");
        try
        {
            var service = new DiagnosticLogService(folder);
            service.Info("TEST_EVENT", "Próba raportu");
            var snapshot = new DiagnosticSnapshot(
                "ETS2 · telemetria aktywna", "Dzień 12, 14:30", "80.0 km/h",
                "****1234", "Jazda", "ciągła 01:00", "****5678", "Dyspozycyjność",
                "ciągła 00:00", "podwójna obsada", 42, 0);
            await using var destination = new MemoryStream();

            await service.CreateReportAsync(destination, snapshot);
            destination.Position = 0;
            using var archive = new ZipArchive(destination, ZipArchiveMode.Read);

            var summary = archive.GetEntry("diagnostic-summary.txt");
            Assert.NotNull(summary);
            Assert.Contains(archive.Entries, entry => entry.FullName.StartsWith("logs/tachograph-"));
            using var reader = new StreamReader(summary!.Open());
            var text = await reader.ReadToEndAsync();
            Assert.Contains("Dzień 12, 14:30", text);
            Assert.Contains("****1234", text);
            Assert.DoesNotContain("ETS2-DEFAULT", text);
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void Activity_record_conflict_is_written_as_a_diagnostic_warning()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"ets2-tacho-conflict-test-{Guid.NewGuid():N}");
        try
        {
            var service = new DiagnosticLogService(folder);
            var existing = Record(DriverActivity.Driving);
            var incoming = Record(DriverActivity.OtherWork);

            service.RecordConflict("CARD-TEST", 3, existing, incoming);

            var log = File.ReadAllText(service.CurrentLogPath);
            Assert.Contains("WARN", log);
            Assert.Contains("ACTIVITY_RECORD_CONFLICT", log);
            Assert.Contains("Karta=CARD-TEST, sesja=3, minuta=100", log);
            Assert.Contains("Driving", log);
            Assert.Contains("OtherWork", log);
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    private static ActivityRecord Record(DriverActivity activity) => new()
    {
        Id = Guid.NewGuid(),
        DriverCardId = "CARD-TEST",
        Activity = activity,
        Start = new GameTime(100),
        EndExclusive = new GameTime(101),
        RecordedAtUtc = DateTimeOffset.UtcNow
    };
}

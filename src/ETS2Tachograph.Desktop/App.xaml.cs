using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Application.Services;
using ETS2Tachograph.Engine;
using ETS2Tachograph.Core.Settings;
using ETS2Tachograph.RuleEngine;
using ETS2Tachograph.Infrastructure.Persistence;
using ETS2Tachograph.Reports;
using ETS2Tachograph.Telemetry.Scs;
using Microsoft.EntityFrameworkCore;

namespace ETS2Tachograph.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private const string SingleInstanceMutexName = @"Local\ETS2Tachograph.Desktop.SingleInstance";

    private TachographDbContext? _db;
    private ScsTelemetrySource? _source;
    private MainViewModel? _viewModel;
    private DiagnosticLogService? _diagnostics;
    private Mutex? _singleInstanceMutex;
    private bool _ownsSingleInstanceMutex;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _singleInstanceMutex = new Mutex(
            initiallyOwned: true,
            SingleInstanceMutexName,
            out _ownsSingleInstanceMutex);
        if (!_ownsSingleInstanceMutex)
        {
            MessageBox.Show(
                "ETS2 Digital Tachograph jest już uruchomiony.",
                "Aplikacja już działa",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown(2);
            return;
        }

        _diagnostics = new DiagnosticLogService();
        _diagnostics.Info("APP_START", "Uruchamianie aplikacji.");
        DispatcherUnhandledException += (_, args) =>
            _diagnostics.Error("UNHANDLED_UI_EXCEPTION", args.Exception);
        TaskScheduler.UnobservedTaskException += (_, args) =>
            _diagnostics.Error("UNOBSERVED_TASK_EXCEPTION", args.Exception);
        try
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ETS2Tachograph");
            Directory.CreateDirectory(folder);
            var databasePath = Path.Combine(folder, "tachograph.db");
            var backupPath = DatabaseBackup.CreateBeforeMigration(databasePath);
            if (backupPath is not null)
            {
                _diagnostics.Info("DATABASE_BACKUP_CREATED", $"Utworzono kopię bazy: {backupPath}");
            }

            var options = new DbContextOptionsBuilder<TachographDbContext>()
                .UseSqlite($"Data Source={databasePath}").Options;
            _db = new TachographDbContext(options);
            await _db.Database.MigrateAsync();
            var driverService = new DriverService(new DriverRepository(_db));
            var active = await driverService.GetActiveProfileAsync();
            if (active is null)
            {
                active = await driverService.CreateProfileAsync(new CreateDriverProfileDto(
                    "Kierowca ETS2",
                    new DriverCardDto("ETS2-DEFAULT", "PL", DateOnly.FromDateTime(DateTime.Today),
                        DateOnly.FromDateTime(DateTime.Today.AddYears(5)))));
                await driverService.SetActiveProfileAsync(active.Id);
                active = await driverService.GetActiveProfileAsync();
            }

            var cardId = active!.Cards[0].CardNumber;
            var settingsService = new SettingsService(new SettingsRepository(_db));
            var savedSettings = await settingsService.LoadAsync();
            var crewEngine = new CrewTachographEngine(
                new TachographSettings
                {
                    DrivingSpeedThresholdKph = savedSettings.DrivingSpeedThresholdKph,
                    WeekEpochOffsetDays = savedSettings.WeekEpochOffsetDays
                },
                regulationOptions: new RegulationOptions
                {
                    WeekEpochOffsetDays = savedSettings.WeekEpochOffsetDays
                });
            var activityRepository = new ActivityRepository(_db, _diagnostics);
            var crewService = new CrewTachographService(
                crewEngine,
                activityRepository,
                new ActivityRetentionService(activityRepository));
            var manualEntryService = new ManualEntryService(
                activityRepository,
                _diagnostics);
            var activityGapService = new ActivityGapService(activityRepository);
            _source = new ScsTelemetrySource(new ScsMemoryMappedTelemetryReader());
            _viewModel = new MainViewModel(
                cardId, crewService, manualEntryService, activityGapService, driverService,
                new ExportService(activityRepository), new ImportService(activityRepository),
                new ReportService(
                    activityRepository,
                    new RegulationReportAnalyzer(options: new RegulationOptions
                    {
                        WeekEpochOffsetDays = savedSettings.WeekEpochOffsetDays
                    })),
                new PdfReportExporter(), settingsService, savedSettings, _source, _diagnostics);
            var window = new MainWindow(_viewModel);
            MainWindow = window;
            window.Show();
            await _viewModel.StartAsync();
        }
        catch (Exception exception)
        {
            _diagnostics?.Error("APP_START_FAILED", exception);
            MessageBox.Show(exception.Message, "Nie można uruchomić tachografu", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _diagnostics?.Info("APP_STOP", $"Zamykanie aplikacji. Kod: {e.ApplicationExitCode}.");
        _viewModel?.Dispose();
        _source?.Dispose();
        _db?.Dispose();
        if (_ownsSingleInstanceMutex)
        {
            _singleInstanceMutex?.ReleaseMutex();
            _ownsSingleInstanceMutex = false;
        }

        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}


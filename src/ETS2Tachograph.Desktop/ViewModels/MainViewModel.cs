using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Application.Services;
using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Interfaces;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.Engine;
using ETS2Tachograph.RuleEngine;
using Microsoft.Win32;

namespace ETS2Tachograph.Desktop;

public sealed record RestTargetOption(string Name, string DeviceLabel, int Minutes);

public sealed record ManualEntryWorkBlockRow(long FromGameMinute, long ToGameMinuteExclusive)
{
    public string FromText => GameClockFormatter.Format(new GameTime(FromGameMinute));
    public string ToText => GameClockFormatter.Format(new GameTime(ToGameMinuteExclusive));
    public string DurationText => $"{(ToGameMinuteExclusive - FromGameMinute) / 60:00}:{(ToGameMinuteExclusive - FromGameMinute) % 60:00}";
}

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private static readonly RestTargetOption[] AvailableRestTargets =
    [
        new("Przerwa 15 min · część 1", "15 MIN CZĘŚĆ 1", 15),
        new("Przerwa 30 min · część 2", "30 MIN CZĘŚĆ 2", 30),
        new("Przerwa 45 min · pełna", "45 MIN PEŁNA", 45),
        new("Odpoczynek dzienny · 9 h", "DZIENNY 9H", 9 * 60),
        new("Odpoczynek dzienny · 11 h", "DZIENNY 11H", 11 * 60),
        new("Odpoczynek tygodniowy · 24 h", "TYGODNIOWY 24H", 24 * 60),
        new("Odpoczynek tygodniowy · 45 h", "TYGODNIOWY 45H", 45 * 60)
    ];

    private readonly CrewTachographService _crew;
    private readonly ManualEntryService _manualEntries;
    private readonly ActivityGapService _activityGaps;
    private readonly DriverService _drivers;
    private readonly ExportService _export;
    private readonly ImportService _import;
    private readonly SettingsService _settings;
    private readonly ReportService _reports;
    private readonly IPdfReportExporter _pdfReports;
    private readonly DiagnosticLogService _diagnostics;
    private readonly string _defaultDriverCardId;
    private readonly ITelemetrySource _telemetry;
    private readonly CancellationTokenSource _cancellation = new();
    private string _connectionStatus = "Oczekiwanie na ETS2...";
    private string _gameTimeText = "Czas gry: --";
    private string _activityText = "Brak danych";
    private string _continuousDriving = "00:00";
    private string _untilBreak = "04:30";
    private string _dailyDriving = "00:00";
    private string _dailyDrivingWithLimit = "00:00 / 09:00";
    private string _dailyWorkWithLimit = "00:00 / 13:00";
    private string _dailyExtensionsUsage = "0 / 2";
    private string _reducedDailyRestsUsage = "0 / 3";
    private string _compensationText = "—";
    private string _compensationForeground = "#91A4B7";
    private bool _compensationOverdue;
    private CompensationOverview _compensationOverview = CompensationOverview.Empty;
    private bool _dailyExtensionsExceeded;
    private bool _reducedDailyRestsExceeded;
    private string _weeklyDriving = "00:00";
    private string _modesText = "Tryb zwykły · pojedyncza obsada";
    private DriverProfileDto? _selectedProfile;
    private DriverProfileDto? _reportDriverProfile;
    private DriverProfileDto? _compensationDriverProfile;
    private string _newDriverName = string.Empty;
    private string _newCardNumber = string.Empty;
    private double _drivingThreshold;
    private int _weekOffset;
    private string _operationStatus = string.Empty;
    private ReportDto? _currentReport;
    private string _reportFrom = string.Empty;
    private string _reportTo = string.Empty;
    private string _reportDriving = "00:00";
    private string _reportWork = "00:00";
    private string _reportAvailability = "00:00";
    private string _reportRest = "00:00";
    private string _reportCompensation = "—";
    private string _reportCompensationForeground = "#8D9AAD";
    private int _reportViolationCount;
    private bool _reportHasUnresolvedGaps;
    private string _reportGapWarningText = string.Empty;
    private int _selectedMainTabIndex;
    private bool _isCardInserted;
    private bool _isCardDialogVisible;
    private bool _cardDialogIsInsertion;
    private string _cardDialogTitle = string.Empty;
    private string _cardDialogMessage = string.Empty;
    private string _cardCountry = "PL";
    private string _cardOwner = "---";
    private string _cardNumber = "BRAK KARTY";
    private readonly DispatcherTimer _clockTimer;
    private bool _isCard2Inserted;
    private string _card2Owner = "---";
    private string _card2Number = "BRAK KARTY";
    private DriverActivity _driver2Activity = DriverActivity.Availability;
    private string _driver2ActivityText = "BRAK KARTY";
    private string _driver2ContinuousDriving = "00:00";
    private string _driver2UntilBreak = "04:30";
    private string _driver2DailyDriving = "00:00";
    private string _driver2DailyDrivingWithLimit = "00:00 / 09:00";
    private string _driver2DailyWorkWithLimit = "00:00 / 13:00";
    private string _driver2DailyExtensionsUsage = "0 / 2";
    private string _driver2ReducedDailyRestsUsage = "0 / 3";
    private string _driver2CompensationText = "—";
    private string _driver2CompensationForeground = "#91A4B7";
    private bool _driver2CompensationOverdue;
    private CompensationOverview _driver2CompensationOverview = CompensationOverview.Empty;
    private bool _driver2DailyExtensionsExceeded;
    private bool _driver2ReducedDailyRestsExceeded;
    private string _driver2WeeklyDriving = "00:00";
    private string _driver2FortnightlyDriving = "00:00";
    private string _driver2DailyRestDeadline = "24:00";
    private string _driver2WeeklyRestDeadline = "144:00";
    private int _cardDialogSlot = 1;
    private double _currentSpeed;
    private double _odometer = 123456.7;
    private DateTimeOffset? _lastDistanceSample;
    private string _deviceLine1 = string.Empty;
    private string _deviceLine2 = string.Empty;
    private string _deviceLine3 = string.Empty;
    private string _deviceLine2Foreground = "#111A12";
    private bool _deviceMenuOpen;
    private int _menuIndex;
    private bool _isPrinting;
    private bool _warningBlink;
    private bool _isCardLoading;
    private string _deviceMenuPage = "root";
    private string _startCountry = "PL";
    private string _endCountry = "PL";
    private string _fortnightlyDriving = "00:00";
    private string _dailyRestDeadline = "24:00";
    private string _weeklyRestDeadline = "144:00";
    private string _currentActivityDuration = "00:00";
    private DriverActivity? _lastDisplayedActivity;
    private long? _activityStartedAtMinute;
    private RestTargetOption _selectedRestTarget = AvailableRestTargets[2];
    private long? _restStartedAtGameMinute;
    private string _restElapsed = "00:00";
    private string _restRemaining = "00:45";
    private string _restStatus = "OCZEKUJE";
    private double _restProgressPercent;
    private RestTargetOption _selectedRestTarget2 = AvailableRestTargets[2];
    private long? _restStartedAtGameMinute2;
    private string _restElapsed2 = "00:00";
    private string _restRemaining2 = "00:45";
    private string _restStatus2 = "OCZEKUJE";
    private double _restProgressPercent2;
    private long? _lastLoggedGameTimeJumpMinute;
    private uint? _lastLoggedWorldGeneration;
    private uint? _lastObservedCargoOperationGeneration;
    private ActivityGap? _manualEntryGap;
    private ActivityGap? _optionalManualEntryGap;
    private int _manualEntrySlot = 1;
    private bool _isManualEntryVisible;
    private bool _isManualEntryForced;
    private string _manualEntryRangeText = string.Empty;
    private string _manualEntryDurationText = string.Empty;
    private string _manualEntryWorkFrom = string.Empty;
    private string _manualEntryWorkTo = string.Empty;
    private ManualEntryWorkBlockRow? _selectedManualWorkBlock;
    private string _manualEntryValidationMessage = string.Empty;
    private string _manualEntrySelectionMessage = string.Empty;
    private string _manualEntryQualificationMessage = string.Empty;
    private bool _showResolvedGaps;
    private int _unresolvedGapCount;
    private Guid? _lastOptionalGapWarningId;
    private readonly Dictionary<string, RestCardPersistentState> _cardRestStates =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly string[] Countries = ["PL", "DE", "CZ", "SK", "AT", "FR", "NL", "BE", "ES", "IT", "DK", "SE", "NO", "FI", "LT", "LV", "EE"];

    public MainViewModel(
        string driverCardId,
        CrewTachographService crew,
        ManualEntryService manualEntries,
        ActivityGapService activityGaps,
        DriverService drivers,
        ExportService export,
        ImportService import,
        ReportService reports,
        IPdfReportExporter pdfReports,
        SettingsService settings,
        SettingsDto savedSettings,
        ITelemetrySource telemetry,
        DiagnosticLogService diagnostics)
    {
        _crew = crew;
        _manualEntries = manualEntries;
        _activityGaps = activityGaps;
        _drivers = drivers;
        _export = export;
        _import = import;
        _settings = settings;
        _reports = reports;
        _pdfReports = pdfReports;
        _defaultDriverCardId = driverCardId;
        _drivingThreshold = savedSettings.DrivingSpeedThresholdKph;
        _weekOffset = savedSettings.WeekEpochOffsetDays;
        _telemetry = telemetry;
        _diagnostics = diagnostics;
        OtherWorkCommand = new RelayCommand(() => SetActivity(DriverActivity.OtherWork));
        AvailabilityCommand = new RelayCommand(() => SetActivity(DriverActivity.Availability));
        RestCommand = new RelayCommand(StartSelectedRest);
        OutCommand = new RelayCommand(ToggleOutMode);
        FerryCommand = new RelayCommand(ToggleFerryMode);
        CrewCommand = new RelayCommand(() => OperationStatus = "Tryb załogi włącza się automatycznie po włożeniu dwóch kart.");
        CreateProfileCommand = new RelayCommand(async () => await CreateProfileAsync());
        ActivateProfileCommand = new RelayCommand(async () => await ActivateProfileAsync());
        SaveSettingsCommand = new RelayCommand(async () => await SaveSettingsAsync());
        ExportCommand = new RelayCommand(async () => await ExportAsync());
        ImportCommand = new RelayCommand(async () => await ImportAsync());
        RefreshReportCommand = new RelayCommand(async () => await RefreshReportAsync());
        RefreshCompensationDetailsCommand = new RelayCommand(
            async () => await RefreshCompensationDetailsAsync());
        ShowReportGapsCommand = new RelayCommand(async () => await ShowReportGapsAsync());
        ExportCsvCommand = new RelayCommand(async () => await ExportReportAsync("csv"));
        ExportPdfCommand = new RelayCommand(async () => await ExportReportAsync("pdf"));
        ExportVtcCommand = new RelayCommand(async () => await ExportReportAsync("json"));
        ExportDiagnosticCommand = new RelayCommand(async () => await ExportDiagnosticReportAsync());
        InsertCardCommand = new RelayCommand(OpenCardInsertion);
        InsertCard2Command = new RelayCommand(() => OpenCardInsertion(2));
        EjectCardCommand = new RelayCommand(OpenCardEjection);
        EjectCard2Command = new RelayCommand(() => OpenCardEjection(2));
        ConfirmCardCommand = new RelayCommand(async () => await ConfirmCardOperationAsync());
        CancelCardCommand = new RelayCommand(() => IsCardDialogVisible = false);
        Driver1ActivityCommand = new RelayCommand(() => CycleDriverActivity(1));
        Driver2ActivityCommand = new RelayCommand(() => CycleDriverActivity(2));
        StartSelectedRestCommand = new RelayCommand(StartSelectedRest);
        StartSelectedRest2Command = new RelayCommand(StartSelectedRest2);
        DeviceOkCommand = new RelayCommand(async () => await DeviceOkAsync());
        DeviceUpCommand = new RelayCommand(() => MoveMenu(-1));
        DeviceDownCommand = new RelayCommand(() => MoveMenu(1));
        DeviceCancelCommand = new RelayCommand(DeviceCancel);
        AddManualWorkBlockCommand = new RelayCommand(AddManualWorkBlock);
        RemoveManualWorkBlockCommand = new RelayCommand(RemoveManualWorkBlock);
        ConfirmManualEntryCommand = new RelayCommand(async () => await ConfirmManualEntryAsync());
        CancelManualEntryCommand = new RelayCommand(CancelManualEntry);
        OpenOptionalManualEntryCommand = new RelayCommand(OpenOptionalManualEntry);
        ResolveGapFromHistoryCommand = new RelayCommand<ActivityGapListItemDto>(
            OpenManualEntryFromHistory,
            gap => gap.IsResolvable);
        CopyIdentifierCommand = new RelayCommand<string>(CopyIdentifier, value =>
            !string.IsNullOrWhiteSpace(value));
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) =>
        {
            UpdateDeviceDisplay();
            UpdateOpenGapDurations();
        };
        _clockTimer.Start();
        UpdateDeviceDisplay();
    }

    public ObservableCollection<ActivityRecord> History { get; } = [];
    public ObservableCollection<DriverProfileDto> Profiles { get; } = [];
    public ObservableCollection<DriverProfileDto> AvailableCardProfiles { get; } = [];
    public ObservableCollection<string> Violations { get; } = [];
    public ObservableCollection<ActivityRecord> ReportRows { get; } = [];
    public ObservableCollection<ManualEntryWorkBlockRow> ManualEntryWorkBlocks { get; } = [];
    public ObservableCollection<ActivityGapListItemDto> ActivityGaps { get; } = [];
    public ObservableCollection<CompensationDetailRow> CompensationDetails { get; } = [];
    public string CompensationDetailsHeader => CompensationDetails.Count == 0
        ? "Brak zobowiązań w bieżących projekcjach kart."
        : $"Zobowiązania: {CompensationDetails.Count} · otwarte: {CompensationDetails.Count(item => item.IsOpen)}";
    public ICommand OtherWorkCommand { get; }
    public ICommand AvailabilityCommand { get; }
    public ICommand RestCommand { get; }
    public ICommand OutCommand { get; }
    public ICommand FerryCommand { get; }
    public ICommand CrewCommand { get; }
    public ICommand CreateProfileCommand { get; }
    public ICommand ActivateProfileCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand RefreshReportCommand { get; }
    public ICommand RefreshCompensationDetailsCommand { get; }
    public ICommand ShowReportGapsCommand { get; }
    public ICommand ExportCsvCommand { get; }
    public ICommand ExportPdfCommand { get; }
    public ICommand ExportVtcCommand { get; }
    public ICommand ExportDiagnosticCommand { get; }
    public ICommand InsertCardCommand { get; }
    public ICommand InsertCard2Command { get; }
    public ICommand EjectCardCommand { get; }
    public ICommand EjectCard2Command { get; }
    public ICommand ConfirmCardCommand { get; }
    public ICommand CancelCardCommand { get; }
    public ICommand Driver1ActivityCommand { get; }
    public ICommand Driver2ActivityCommand { get; }
    public ICommand StartSelectedRestCommand { get; }
    public ICommand StartSelectedRest2Command { get; }
    public ICommand DeviceOkCommand { get; }
    public ICommand DeviceUpCommand { get; }
    public ICommand DeviceDownCommand { get; }
    public ICommand DeviceCancelCommand { get; }
    public ICommand AddManualWorkBlockCommand { get; }
    public ICommand RemoveManualWorkBlockCommand { get; }
    public ICommand ConfirmManualEntryCommand { get; }
    public ICommand CancelManualEntryCommand { get; }
    public ICommand OpenOptionalManualEntryCommand { get; }
    public ICommand ResolveGapFromHistoryCommand { get; }
    public ICommand CopyIdentifierCommand { get; }
    public DriverProfileDto? SelectedProfile { get => _selectedProfile; set => Set(ref _selectedProfile, value); }
    public DriverProfileDto? ReportDriverProfile
    {
        get => _reportDriverProfile;
        set { if (Set(ref _reportDriverProfile, value)) InvalidateCurrentReport(); }
    }
    public DriverProfileDto? CompensationDriverProfile
    {
        get => _compensationDriverProfile;
        set
        {
            if (!Set(ref _compensationDriverProfile, value)) return;
            CompensationDetails.Clear();
            OnPropertyChanged(nameof(CompensationDetailsHeader));
        }
    }
    public string NewDriverName { get => _newDriverName; set => Set(ref _newDriverName, value); }
    public string NewCardNumber { get => _newCardNumber; set => Set(ref _newCardNumber, value); }
    public double DrivingThreshold { get => _drivingThreshold; set => Set(ref _drivingThreshold, value); }
    public int WeekOffset { get => _weekOffset; set => Set(ref _weekOffset, value); }
    public string OperationStatus
    {
        get => _operationStatus;
        private set
        {
            if (Set(ref _operationStatus, value) && !string.IsNullOrWhiteSpace(value))
                _diagnostics.Info("STATUS", value);
        }
    }
    public string ReportFrom
    {
        get => _reportFrom;
        set { if (Set(ref _reportFrom, value)) InvalidateCurrentReport(); }
    }
    public string ReportTo
    {
        get => _reportTo;
        set { if (Set(ref _reportTo, value)) InvalidateCurrentReport(); }
    }
    public string ReportDriving { get => _reportDriving; private set => Set(ref _reportDriving, value); }
    public string ReportWork { get => _reportWork; private set => Set(ref _reportWork, value); }
    public string ReportAvailability { get => _reportAvailability; private set => Set(ref _reportAvailability, value); }
    public string ReportRest { get => _reportRest; private set => Set(ref _reportRest, value); }
    public string ReportCompensation { get => _reportCompensation; private set => Set(ref _reportCompensation, value); }
    public string ReportCompensationForeground { get => _reportCompensationForeground; private set => Set(ref _reportCompensationForeground, value); }
    public int ReportViolationCount { get => _reportViolationCount; private set => Set(ref _reportViolationCount, value); }
    public bool ReportHasUnresolvedGaps { get => _reportHasUnresolvedGaps; private set => Set(ref _reportHasUnresolvedGaps, value); }
    public string ReportGapWarningText { get => _reportGapWarningText; private set => Set(ref _reportGapWarningText, value); }
    public int SelectedMainTabIndex { get => _selectedMainTabIndex; set => Set(ref _selectedMainTabIndex, value); }
    public string ConnectionStatus
    {
        get => _connectionStatus;
        private set
        {
            if (Set(ref _connectionStatus, value))
                _diagnostics.Info("TELEMETRY_STATUS", value);
        }
    }
    public string GameTimeText { get => _gameTimeText; private set => Set(ref _gameTimeText, value); }
    public string ActivityText { get => _activityText; private set => Set(ref _activityText, value); }
    public string ContinuousDriving { get => _continuousDriving; private set => Set(ref _continuousDriving, value); }
    public string UntilBreak { get => _untilBreak; private set => Set(ref _untilBreak, value); }
    public string DailyDriving { get => _dailyDriving; private set => Set(ref _dailyDriving, value); }
    public string DailyDrivingWithLimit { get => _dailyDrivingWithLimit; private set => Set(ref _dailyDrivingWithLimit, value); }
    public string DailyWorkWithLimit { get => _dailyWorkWithLimit; private set => Set(ref _dailyWorkWithLimit, value); }
    public string DailyExtensionsUsage { get => _dailyExtensionsUsage; private set => Set(ref _dailyExtensionsUsage, value); }
    public string ReducedDailyRestsUsage { get => _reducedDailyRestsUsage; private set => Set(ref _reducedDailyRestsUsage, value); }
    public string CompensationText { get => _compensationText; private set => Set(ref _compensationText, value); }
    public string CompensationForeground { get => _compensationForeground; private set => Set(ref _compensationForeground, value); }
    public bool CompensationOverdue { get => _compensationOverdue; private set => Set(ref _compensationOverdue, value); }
    public CompensationOverview CompensationOverview { get => _compensationOverview; private set => Set(ref _compensationOverview, value); }
    public bool DailyExtensionsExceeded { get => _dailyExtensionsExceeded; private set => Set(ref _dailyExtensionsExceeded, value); }
    public bool ReducedDailyRestsExceeded { get => _reducedDailyRestsExceeded; private set => Set(ref _reducedDailyRestsExceeded, value); }
    public string WeeklyDriving { get => _weeklyDriving; private set => Set(ref _weeklyDriving, value); }
    public string FortnightlyDriving { get => _fortnightlyDriving; private set => Set(ref _fortnightlyDriving, value); }
    public string DailyRestDeadline { get => _dailyRestDeadline; private set => Set(ref _dailyRestDeadline, value); }
    public string WeeklyRestDeadline { get => _weeklyRestDeadline; private set => Set(ref _weeklyRestDeadline, value); }
    public string CurrentActivityDuration { get => _currentActivityDuration; private set => Set(ref _currentActivityDuration, value); }
    public IReadOnlyList<RestTargetOption> RestTargets => AvailableRestTargets;
    public RestTargetOption SelectedRestTarget
    {
        get => _selectedRestTarget;
        set
        {
            if (value is null || !Set(ref _selectedRestTarget, value)) return;
            OnPropertyChanged(nameof(RestTargetText));
            UpdateRestCounters(_crew.Current.Driver);
            SaveDeviceState();
        }
    }
    public string RestTargetText => SelectedRestTarget.Name;
    public string RestElapsed { get => _restElapsed; private set => Set(ref _restElapsed, value); }
    public string RestRemaining { get => _restRemaining; private set => Set(ref _restRemaining, value); }
    public string RestStatus { get => _restStatus; private set => Set(ref _restStatus, value); }
    public double RestProgressPercent { get => _restProgressPercent; private set => Set(ref _restProgressPercent, value); }
    public RestTargetOption SelectedRestTarget2
    {
        get => _selectedRestTarget2;
        set
        {
            if (value is null || !Set(ref _selectedRestTarget2, value)) return;
            OnPropertyChanged(nameof(RestTargetText2));
            UpdateRestCounters2(_crew.Current);
            SaveDeviceState();
        }
    }
    public string RestTargetText2 => SelectedRestTarget2.Name;
    public string RestElapsed2 { get => _restElapsed2; private set => Set(ref _restElapsed2, value); }
    public string RestRemaining2 { get => _restRemaining2; private set => Set(ref _restRemaining2, value); }
    public string RestStatus2 { get => _restStatus2; private set => Set(ref _restStatus2, value); }
    public double RestProgressPercent2 { get => _restProgressPercent2; private set => Set(ref _restProgressPercent2, value); }
    public string Driver2ActivityText { get => _driver2ActivityText; private set => Set(ref _driver2ActivityText, value); }
    public string Driver2ContinuousDriving { get => _driver2ContinuousDriving; private set => Set(ref _driver2ContinuousDriving, value); }
    public string Driver2UntilBreak { get => _driver2UntilBreak; private set => Set(ref _driver2UntilBreak, value); }
    public string Driver2DailyDriving { get => _driver2DailyDriving; private set => Set(ref _driver2DailyDriving, value); }
    public string Driver2DailyDrivingWithLimit { get => _driver2DailyDrivingWithLimit; private set => Set(ref _driver2DailyDrivingWithLimit, value); }
    public string Driver2DailyWorkWithLimit { get => _driver2DailyWorkWithLimit; private set => Set(ref _driver2DailyWorkWithLimit, value); }
    public string Driver2DailyExtensionsUsage { get => _driver2DailyExtensionsUsage; private set => Set(ref _driver2DailyExtensionsUsage, value); }
    public string Driver2ReducedDailyRestsUsage { get => _driver2ReducedDailyRestsUsage; private set => Set(ref _driver2ReducedDailyRestsUsage, value); }
    public string Driver2CompensationText { get => _driver2CompensationText; private set => Set(ref _driver2CompensationText, value); }
    public string Driver2CompensationForeground { get => _driver2CompensationForeground; private set => Set(ref _driver2CompensationForeground, value); }
    public bool Driver2CompensationOverdue { get => _driver2CompensationOverdue; private set => Set(ref _driver2CompensationOverdue, value); }
    public CompensationOverview Driver2CompensationOverview { get => _driver2CompensationOverview; private set => Set(ref _driver2CompensationOverview, value); }
    public bool Driver2DailyExtensionsExceeded { get => _driver2DailyExtensionsExceeded; private set => Set(ref _driver2DailyExtensionsExceeded, value); }
    public bool Driver2ReducedDailyRestsExceeded { get => _driver2ReducedDailyRestsExceeded; private set => Set(ref _driver2ReducedDailyRestsExceeded, value); }
    public string Driver2WeeklyDriving { get => _driver2WeeklyDriving; private set => Set(ref _driver2WeeklyDriving, value); }
    public string Driver2FortnightlyDriving { get => _driver2FortnightlyDriving; private set => Set(ref _driver2FortnightlyDriving, value); }
    public string Driver2DailyRestDeadline { get => _driver2DailyRestDeadline; private set => Set(ref _driver2DailyRestDeadline, value); }
    public string Driver2WeeklyRestDeadline { get => _driver2WeeklyRestDeadline; private set => Set(ref _driver2WeeklyRestDeadline, value); }
    public bool CanSelectRestTarget2 => !_crew.Engine.VehicleMoving;
    public string StartCountry { get => _startCountry; private set => Set(ref _startCountry, value); }
    public string EndCountry { get => _endCountry; private set => Set(ref _endCountry, value); }
    public string ModesText { get => _modesText; private set => Set(ref _modesText, value); }
    public bool IsCardInserted { get => _isCardInserted; private set { if (Set(ref _isCardInserted, value)) OnPropertyChanged(nameof(CardStatus)); } }
    public bool IsCardDialogVisible { get => _isCardDialogVisible; private set => Set(ref _isCardDialogVisible, value); }
    public string CardDialogTitle { get => _cardDialogTitle; private set => Set(ref _cardDialogTitle, value); }
    public string CardDialogMessage { get => _cardDialogMessage; private set => Set(ref _cardDialogMessage, value); }
    public string CardCountry { get => _cardCountry; set => Set(ref _cardCountry, value.ToUpperInvariant()); }
    public string CardOwner { get => _cardOwner; private set => Set(ref _cardOwner, value); }
    public string CardNumber { get => _cardNumber; private set => Set(ref _cardNumber, value); }
    public string CardStatus => IsCardInserted ? "KARTA 1 GOTOWA" : "BRAK KARTY 1";
    public bool IsCard2Inserted { get => _isCard2Inserted; private set { if (Set(ref _isCard2Inserted, value)) OnPropertyChanged(nameof(Card2Status)); } }
    public string Card2Owner { get => _card2Owner; private set => Set(ref _card2Owner, value); }
    public string Card2Number { get => _card2Number; private set => Set(ref _card2Number, value); }
    public string Card2Status => IsCard2Inserted ? "KARTA 2 GOTOWA" : "BRAK KARTY 2";
    public string CardDialogSlotText => $"{_cardDialogSlot}  KARTA KIEROWCY";
    public string DeviceLine1 { get => _deviceLine1; private set => Set(ref _deviceLine1, value); }
    public string DeviceLine2 { get => _deviceLine2; private set => Set(ref _deviceLine2, value); }
    public string DeviceLine3 { get => _deviceLine3; private set => Set(ref _deviceLine3, value); }
    public string DeviceLine2Foreground { get => _deviceLine2Foreground; private set => Set(ref _deviceLine2Foreground, value); }
    public bool IsPrinting { get => _isPrinting; private set => Set(ref _isPrinting, value); }
    public bool IsManualEntryVisible { get => _isManualEntryVisible; private set => Set(ref _isManualEntryVisible, value); }
    public bool IsManualEntryForced
    {
        get => _isManualEntryForced;
        private set
        {
            if (!Set(ref _isManualEntryForced, value)) return;
            OnPropertyChanged(nameof(CanCancelManualEntry));
        }
    }
    public bool CanCancelManualEntry => !IsManualEntryForced;
    public bool HasOptionalManualEntryGap => _optionalManualEntryGap is not null && !IsManualEntryVisible;
    public string ManualEntryTitle => IsManualEntryForced
        ? $"WYMAGANY WPIS MANUALNY · SLOT {_manualEntrySlot}"
        : $"OPCJONALNY WPIS MANUALNY · SLOT {_manualEntrySlot}";
    public string ManualEntryRangeText { get => _manualEntryRangeText; private set => Set(ref _manualEntryRangeText, value); }
    public string ManualEntryDurationText { get => _manualEntryDurationText; private set => Set(ref _manualEntryDurationText, value); }
    public string ManualEntryWorkFrom { get => _manualEntryWorkFrom; set => Set(ref _manualEntryWorkFrom, value); }
    public string ManualEntryWorkTo { get => _manualEntryWorkTo; set => Set(ref _manualEntryWorkTo, value); }
    public ManualEntryWorkBlockRow? SelectedManualWorkBlock
    {
        get => _selectedManualWorkBlock;
        set => Set(ref _selectedManualWorkBlock, value);
    }
    public string ManualEntryValidationMessage { get => _manualEntryValidationMessage; private set => Set(ref _manualEntryValidationMessage, value); }
    public string ManualEntrySelectionMessage { get => _manualEntrySelectionMessage; private set => Set(ref _manualEntrySelectionMessage, value); }
    public string ManualEntryQualificationMessage { get => _manualEntryQualificationMessage; private set => Set(ref _manualEntryQualificationMessage, value); }
    public bool ShowResolvedGaps
    {
        get => _showResolvedGaps;
        set
        {
            if (!Set(ref _showResolvedGaps, value)) return;
            _ = RefreshActivityGapsSafelyAsync();
        }
    }
    public int UnresolvedGapCount
    {
        get => _unresolvedGapCount;
        private set
        {
            if (!Set(ref _unresolvedGapCount, value)) return;
            OnPropertyChanged(nameof(ActivityGapsHeader));
        }
    }
    public string ActivityGapsHeader => $"LUKI AKTYWNOŚCI · NIEROZLICZONE: {UnresolvedGapCount}";

    public async Task StartAsync()
    {
        _diagnostics.Info("APP_READY", "Inicjalizacja profili, kart i historii.");
        foreach (var profile in await _drivers.GetProfilesAsync(_cancellation.Token)) Profiles.Add(profile);
        SelectedProfile = Profiles.FirstOrDefault(x => x.IsActive) ?? Profiles.FirstOrDefault();
        foreach (var card in Profiles.SelectMany(x => x.Cards))
            await _crew.RegisterCardAsync(card.CardNumber, _cancellation.Token);
        LoadDeviceState();
        ReportDriverProfile = FindProfileByCard(CurrentDriverCardId) ?? SelectedProfile;
        CompensationDriverProfile = ReportDriverProfile;
        await ReloadHistoryAsync(_cancellation.Token);
        await RefreshReportAsync();
        await RefreshCompensationDetailsAsync();
        await RefreshActivityGapsAsync(_cancellation.Token);
        _ = ReadTelemetryAsync(_cancellation.Token);
    }

    private async Task ReadTelemetryAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var frame in _telemetry.ReadFramesAsync(cancellationToken))
            {
                var snapshot = await _crew.ProcessFrameAsync(frame, cancellationToken);
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => Refresh(snapshot));
                if ((snapshot.Driver?.CreatedGaps.Count ?? 0) > 0 ||
                    (snapshot.CoDriver?.CreatedGaps.Count ?? 0) > 0)
                {
                    await await System.Windows.Application.Current.Dispatcher.InvokeAsync(
                        RefreshActivityGapsSafelyAsync);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            _diagnostics.Error("TELEMETRY_ERROR", exception);
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => ConnectionStatus = $"Błąd telemetrii: {exception.Message}");
        }
    }

    private void Refresh(CrewTachographSnapshot snapshot)
    {
        var frame = snapshot.Frame;
        if (frame is not null)
        {
            if (_lastDistanceSample is not null)
                _odometer += Math.Max(0, (frame.RecordedAtUtc - _lastDistanceSample.Value).TotalHours) * Math.Abs(frame.SpeedKph);
            _lastDistanceSample = frame.RecordedAtUtc;
            _currentSpeed = Math.Abs(frame.SpeedKph);
            if ((snapshot.Driver?.GameTimeJumpDetected == true || snapshot.CoDriver?.GameTimeJumpDetected == true) &&
                _lastLoggedGameTimeJumpMinute != frame.GameTime.TotalMinutes)
            {
                _lastLoggedGameTimeJumpMinute = frame.GameTime.TotalMinutes;
                _diagnostics.Warning("GAME_TIME_JUMP", $"Wykryto skok do {GameClockFormatter.Format(frame.GameTime)}.");
            }
            if ((snapshot.Driver?.WorldGenerationChanged == true || snapshot.CoDriver?.WorldGenerationChanged == true) &&
                _lastLoggedWorldGeneration != frame.WorldGeneration)
            {
                _lastLoggedWorldGeneration = frame.WorldGeneration;
                _diagnostics.Warning(
                    "WORLD_GENERATION_CHANGED",
                    $"Nowa generacja świata {frame.WorldGeneration}, granica przy {GameClockFormatter.Format(frame.GameTime)}.");
            }
            if (_lastObservedCargoOperationGeneration is not null &&
                _lastObservedCargoOperationGeneration.Value != frame.CargoOperationGeneration)
            {
                _diagnostics.Info(
                    "CARGO_OPERATION_MARKER",
                    $"Znacznik {frame.CargoOperationGeneration} przy {GameClockFormatter.Format(frame.GameTime)}.");
            }
            _lastObservedCargoOperationGeneration = frame.CargoOperationGeneration;
        }
        ConnectionStatus = frame is null ? "Oczekiwanie na ETS2..." : frame.GamePaused ? "ETS2 · pauza" : "ETS2 · telemetria aktywna";
        GameTimeText = frame is null ? "Czas gry: --" : $"Czas gry: {GameClockFormatter.Format(frame.GameTime)}";

        var driver = snapshot.Driver;
        ActivityText = driver is null ? "BRAK KARTY" : ActivityDescription(driver.ProvisionalActivity ?? driver.ManualActivity);
        if (driver is not null)
        {
            var displayedActivity = driver.ProvisionalActivity ?? driver.ManualActivity;
            if (_lastDisplayedActivity != displayedActivity)
            {
                _lastDisplayedActivity = displayedActivity;
                _activityStartedAtMinute = driver.GameTime?.TotalMinutes;
            }
            if (driver.GameTime is not null && _activityStartedAtMinute is not null)
                CurrentActivityDuration = Format(Math.Max(0, driver.GameTime.Value.TotalMinutes - _activityStartedAtMinute.Value));
        }
        else
        {
            CurrentActivityDuration = "00:00";
            _lastDisplayedActivity = null;
            _activityStartedAtMinute = null;
        }
        UpdateRestCounters(driver);

        if (driver?.Regulation is { } driverRules)
        {
            var state = driverRules.State;
            ContinuousDriving = Format(state.ContinuousDrivingMinutes);
            UntilBreak = Format(state.MinutesUntilBreak);
            DailyDriving = Format(state.DailyDrivingMinutes);
            DailyDrivingWithLimit = FormatDailyDrivingWithLimit(state);
            DailyWorkWithLimit = FormatWithLimit(state.DailyWorkMinutes, 13 * 60);
            DailyExtensionsUsage = FormatUsage(state.DailyExtensionsUsedThisWeek, 2);
            ReducedDailyRestsUsage = FormatUsage(state.ReducedDailyRestsSinceWeeklyRest, 3);
            CompensationText = FormatCompensation(driverRules.CompensationSummary);
            CompensationOverdue = driverRules.CompensationSummary.HasOverdue;
            CompensationForeground = CompensationOverdue ? "#FF6B6B" : "#91A4B7";
            CompensationOverview = global::ETS2Tachograph.Desktop.CompensationOverview.From(
                WeeklyRestCompensationDtoMapper.MapAll(driverRules.CompensationObligations));
            DailyExtensionsExceeded = state.DailyExtensionsUsedThisWeek > 2;
            ReducedDailyRestsExceeded = state.ReducedDailyRestsSinceWeeklyRest > 3;
            WeeklyDriving = Format(state.WeeklyDrivingMinutes);
            FortnightlyDriving = Format(state.FortnightlyDrivingMinutes);
            DailyRestDeadline = Format(state.MinutesUntilDailyRestDeadline);
            WeeklyRestDeadline = Format(state.MinutesUntilWeeklyRestDeadline);
        }
        else ResetDriver1Counters();

        var coDriver = snapshot.CoDriver;
        OnPropertyChanged(nameof(CanSelectRestTarget2));
        if (_crew.Engine.VehicleMoving && IsCard2Inserted && SelectedRestTarget2.Minutes != CrewTachographEngine.MovingBreakMinutes)
        {
            _selectedRestTarget2 = AvailableRestTargets.First(x => x.Minutes == CrewTachographEngine.MovingBreakMinutes);
            OnPropertyChanged(nameof(SelectedRestTarget2));
            OnPropertyChanged(nameof(RestTargetText2));
        }
        Driver2ActivityText = coDriver is null ? "BRAK KARTY" : ActivityDescription(coDriver.ProvisionalActivity ?? coDriver.ManualActivity);
        _driver2Activity = coDriver?.ManualActivity ?? DriverActivity.Availability;
        if (coDriver?.Regulation is { } coRules)
        {
            var state = coRules.State;
            Driver2ContinuousDriving = Format(state.ContinuousDrivingMinutes);
            Driver2UntilBreak = Format(state.MinutesUntilBreak);
            Driver2DailyDriving = Format(state.DailyDrivingMinutes);
            Driver2DailyDrivingWithLimit = FormatDailyDrivingWithLimit(state);
            Driver2DailyWorkWithLimit = FormatWithLimit(state.DailyWorkMinutes, 13 * 60);
            Driver2DailyExtensionsUsage = FormatUsage(state.DailyExtensionsUsedThisWeek, 2);
            Driver2ReducedDailyRestsUsage = FormatUsage(state.ReducedDailyRestsSinceWeeklyRest, 3);
            Driver2CompensationText = FormatCompensation(coRules.CompensationSummary);
            Driver2CompensationOverdue = coRules.CompensationSummary.HasOverdue;
            Driver2CompensationForeground = Driver2CompensationOverdue ? "#FF6B6B" : "#91A4B7";
            Driver2CompensationOverview = CompensationOverview.From(
                WeeklyRestCompensationDtoMapper.MapAll(coRules.CompensationObligations));
            Driver2DailyExtensionsExceeded = state.DailyExtensionsUsedThisWeek > 2;
            Driver2ReducedDailyRestsExceeded = state.ReducedDailyRestsSinceWeeklyRest > 3;
            Driver2WeeklyDriving = Format(state.WeeklyDrivingMinutes);
            Driver2FortnightlyDriving = Format(state.FortnightlyDrivingMinutes);
            Driver2DailyRestDeadline = Format(state.MinutesUntilDailyRestDeadline);
            Driver2WeeklyRestDeadline = Format(state.MinutesUntilWeeklyRestDeadline);
        }
        else ResetDriver2Counters();
        UpdateRestCounters2(snapshot);

        Violations.Clear();
        if (driver?.Regulation is not null)
            foreach (var violation in driver.Regulation.Violations)
                Violations.Add($"K1 · {violation.Article}: {violation.Type}");
        if (coDriver?.Regulation is not null)
            foreach (var violation in coDriver.Regulation.Violations)
                Violations.Add($"K2 · {violation.Article}: {violation.Type}");
        if (snapshot.ManualEntryRequired)
            Violations.Insert(0, "WPIS MANUALNY WYMAGANY · jazda zablokowana do rozliczenia luki po wyjęciu karty.");

        foreach (var record in (driver?.CompletedRecords ?? []).Concat(coDriver?.CompletedRecords ?? [])
                     .Where(x => History.All(old => old.Id != x.Id)))
            History.Add(record);
        var activeModes = driver is null ? "Tryb zwykły" : driver.OutModeEnabled ? "OUT" : driver.FerryModeEnabled ? "Prom" : "Tryb zwykły";
        ModesText = $"{activeModes} · {(snapshot.MultiManning ? "podwójna obsada (30 h)" : "pojedyncza obsada (24 h)")}";
        HandleManualEntryState(snapshot);
        UpdateDeviceDisplay();
    }

    private void ResetDriver1Counters()
    {
        ContinuousDriving = "00:00"; UntilBreak = "04:30"; DailyDriving = "00:00";
        DailyDrivingWithLimit = "00:00 / 09:00"; DailyWorkWithLimit = "00:00 / 13:00";
        DailyExtensionsUsage = "0 / 2"; ReducedDailyRestsUsage = "0 / 3";
        CompensationText = "—"; CompensationForeground = "#91A4B7"; CompensationOverdue = false;
        CompensationOverview = global::ETS2Tachograph.Desktop.CompensationOverview.Empty;
        DailyExtensionsExceeded = false; ReducedDailyRestsExceeded = false;
        WeeklyDriving = "00:00"; FortnightlyDriving = "00:00";
        DailyRestDeadline = "24:00"; WeeklyRestDeadline = "144:00";
    }

    private void ResetDriver2Counters()
    {
        Driver2ContinuousDriving = "00:00"; Driver2UntilBreak = "04:30";
        Driver2DailyDriving = "00:00"; Driver2WeeklyDriving = "00:00";
        Driver2DailyDrivingWithLimit = "00:00 / 09:00"; Driver2DailyWorkWithLimit = "00:00 / 13:00";
        Driver2DailyExtensionsUsage = "0 / 2"; Driver2ReducedDailyRestsUsage = "0 / 3";
        Driver2CompensationText = "—"; Driver2CompensationForeground = "#91A4B7"; Driver2CompensationOverdue = false;
        Driver2CompensationOverview = CompensationOverview.Empty;
        Driver2DailyExtensionsExceeded = false; Driver2ReducedDailyRestsExceeded = false;
        Driver2FortnightlyDriving = "00:00";
        Driver2DailyRestDeadline = "24:00"; Driver2WeeklyRestDeadline = "144:00";
    }

    private void SetActivity(DriverActivity activity)
        => SetActivity(TachographSlot.Driver, activity);

    private void SetActivity(TachographSlot slot, DriverActivity activity)
    {
        if ((slot == TachographSlot.Driver && !IsCardInserted) ||
            (slot == TachographSlot.CoDriver && !IsCard2Inserted))
        {
            OperationStatus = $"Włóż kartę kierowcy do czytnika {(int)slot}.";
            return;
        }
        try
        {
            _crew.Engine.SetManualActivity(slot, activity);
            _diagnostics.Info("MANUAL_ACTIVITY", $"Slot {(int)slot}: {activity}.");
            Refresh(_crew.Current);
            SaveDeviceState();
        }
        catch (InvalidOperationException exception)
        {
            _diagnostics.Error("MANUAL_ACTIVITY_REJECTED", exception);
            OperationStatus = exception.Message;
        }
    }

    private void StartSelectedRest()
    {
        var wasResting = _crew.Current.Driver is { } current &&
                         (current.ProvisionalActivity ?? current.ManualActivity) == DriverActivity.BreakOrRest;
        var startedAt = _crew.Current.Frame?.GameTime.TotalMinutes;
        SetActivity(DriverActivity.BreakOrRest);
        if (!_crew.Engine.VehicleMoving && IsCardInserted)
        {
            if (!wasResting && startedAt is not null)
                _restStartedAtGameMinute = startedAt;
            OperationStatus = $"Rozpoczęto: {SelectedRestTarget.Name}.";
            SaveDeviceState();
        }
    }

    private void StartSelectedRest2()
    {
        if (!IsCard2Inserted) { OperationStatus = "Włóż kartę kierowcy do czytnika 2."; return; }
        try
        {
            if (_crew.Engine.VehicleMoving)
            {
                if (_crew.Current.CoDriverMovingBreakActive)
                {
                    OperationStatus = "Przerwa 45 minut kierowcy 2 jest już w trakcie.";
                    return;
                }
                _selectedRestTarget2 = AvailableRestTargets.First(x => x.Minutes == CrewTachographEngine.MovingBreakMinutes);
                OnPropertyChanged(nameof(SelectedRestTarget2));
                OnPropertyChanged(nameof(RestTargetText2));
                _crew.Engine.StartCoDriverMovingBreak();
                _diagnostics.Info("CODRIVER_MOVING_BREAK", "Slot 2: rozpoczęto przerwę 45 minut podczas jazdy.");
                OperationStatus = "Kierowca 2 rozpoczął ciągłą przerwę 45 minut podczas jazdy.";
            }
            else
            {
                var wasResting = _crew.Current.CoDriver is { } current &&
                                 (current.ProvisionalActivity ?? current.ManualActivity) == DriverActivity.BreakOrRest;
                var startedAt = _crew.Current.Frame?.GameTime.TotalMinutes;
                SetActivity(TachographSlot.CoDriver, DriverActivity.BreakOrRest);
                if (!wasResting && startedAt is not null)
                    _restStartedAtGameMinute2 = startedAt;
                OperationStatus = $"Kierowca 2 rozpoczął: {SelectedRestTarget2.Name}.";
            }
            Refresh(_crew.Current);
            SaveDeviceState();
        }
        catch (InvalidOperationException exception)
        {
            _diagnostics.Error("REST_REJECTED", exception);
            OperationStatus = exception.Message;
        }
    }

    private void UpdateRestCounters(TachographSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            _restStartedAtGameMinute = null;
            RestElapsed = "00:00"; RestRemaining = Format(SelectedRestTarget.Minutes);
            RestProgressPercent = 0; RestStatus = "OCZEKUJE";
            return;
        }
        var displayedActivity = snapshot.ProvisionalActivity ?? snapshot.ManualActivity;
        var isResting = IsCardInserted && displayedActivity == DriverActivity.BreakOrRest;
        var now = snapshot.GameTime?.TotalMinutes;

        if (!isResting)
        {
            _restStartedAtGameMinute = null;
            RestElapsed = "00:00";
            RestRemaining = Format(SelectedRestTarget.Minutes);
            RestProgressPercent = 0;
            RestStatus = "OCZEKUJE";
            return;
        }

        if (now is not null && (_restStartedAtGameMinute is null || now.Value < _restStartedAtGameMinute.Value))
            _restStartedAtGameMinute = now.Value;

        var elapsed = now is not null && _restStartedAtGameMinute is not null
            ? Math.Max(0, now.Value - _restStartedAtGameMinute.Value)
            : 0;
        var remaining = Math.Max(0, SelectedRestTarget.Minutes - elapsed);

        RestElapsed = Format(elapsed);
        RestRemaining = Format(remaining);
        RestProgressPercent = Math.Min(100, elapsed * 100d / SelectedRestTarget.Minutes);
        RestStatus = remaining == 0 ? "ZALICZONA" : "W TRAKCIE";
    }

    private void UpdateRestCounters2(CrewTachographSnapshot crewSnapshot)
    {
        if (crewSnapshot.CoDriverMovingBreakActive)
        {
            var target = AvailableRestTargets.First(x => x.Minutes == CrewTachographEngine.MovingBreakMinutes);
            if (_selectedRestTarget2 != target)
            {
                _selectedRestTarget2 = target;
                OnPropertyChanged(nameof(SelectedRestTarget2));
                OnPropertyChanged(nameof(RestTargetText2));
            }
            RestElapsed2 = Format(crewSnapshot.CoDriverMovingBreakElapsedMinutes);
            RestRemaining2 = Format(crewSnapshot.CoDriverMovingBreakRemainingMinutes);
            RestProgressPercent2 = crewSnapshot.CoDriverMovingBreakElapsedMinutes * 100d / CrewTachographEngine.MovingBreakMinutes;
            RestStatus2 = "W TRAKCIE · W RUCHU";
            return;
        }

        if (crewSnapshot.CoDriverMovingBreakCompleted)
        {
            RestElapsed2 = Format(CrewTachographEngine.MovingBreakMinutes);
            RestRemaining2 = "00:00";
            RestProgressPercent2 = 100;
            RestStatus2 = "ZALICZONA · W RUCHU";
            return;
        }

        var snapshot = crewSnapshot.CoDriver;
        if (snapshot is null)
        {
            _restStartedAtGameMinute2 = null;
            RestElapsed2 = "00:00"; RestRemaining2 = Format(SelectedRestTarget2.Minutes);
            RestProgressPercent2 = 0; RestStatus2 = "OCZEKUJE";
            return;
        }

        var activity = snapshot.ProvisionalActivity ?? snapshot.ManualActivity;
        var isResting = IsCard2Inserted && activity == DriverActivity.BreakOrRest;
        var now = snapshot.GameTime?.TotalMinutes;
        if (!isResting)
        {
            _restStartedAtGameMinute2 = null;
            RestElapsed2 = "00:00"; RestRemaining2 = Format(SelectedRestTarget2.Minutes);
            RestProgressPercent2 = 0; RestStatus2 = "OCZEKUJE";
            return;
        }
        if (now is not null && (_restStartedAtGameMinute2 is null || now.Value < _restStartedAtGameMinute2.Value))
            _restStartedAtGameMinute2 = now.Value;
        var elapsed = now is not null && _restStartedAtGameMinute2 is not null
            ? Math.Max(0, now.Value - _restStartedAtGameMinute2.Value) : 0;
        var remaining = Math.Max(0, SelectedRestTarget2.Minutes - elapsed);
        RestElapsed2 = Format(elapsed); RestRemaining2 = Format(remaining);
        RestProgressPercent2 = Math.Min(100, elapsed * 100d / SelectedRestTarget2.Minutes);
        RestStatus2 = remaining == 0 ? "ZALICZONA" : "W TRAKCIE";
    }

    private void ToggleOutMode()
    {
        var driver = _crew.Current.Driver;
        if (driver is null) { OperationStatus = "Włóż kartę do slotu 1."; return; }
        _crew.Engine.SetOutMode(TachographSlot.Driver, !driver.OutModeEnabled);
        _diagnostics.Info("OUT_MODE", $"Slot 1: {(!driver.OutModeEnabled ? "włączono" : "wyłączono")} OUT.");
        Refresh(_crew.Current); SaveDeviceState();
    }

    private void ToggleFerryMode()
    {
        var driver = _crew.Current.Driver;
        if (driver is null) { OperationStatus = "Włóż kartę do slotu 1."; return; }
        _crew.Engine.SetFerryMode(TachographSlot.Driver, !driver.FerryModeEnabled);
        _diagnostics.Info("FERRY_MODE", $"Slot 1: {(!driver.FerryModeEnabled ? "włączono" : "wyłączono")} tryb promu.");
        Refresh(_crew.Current); SaveDeviceState();
    }

    private static string ActivityDescription(DriverActivity activity) => activity switch
    {
        DriverActivity.Driving => "Jazda",
        DriverActivity.OtherWork => "Inna praca",
        DriverActivity.Availability => "Dyspozycyjność",
        DriverActivity.BreakOrRest => "Przerwa / odpoczynek",
        DriverActivity.OutOfScope => "OUT",
        _ => activity.ToString()
    };

    private void OpenCardInsertion() => OpenCardInsertion(1);
    private void OpenCardInsertion(int slot)
    {
        if ((slot == 1 && IsCardInserted) || (slot == 2 && IsCard2Inserted)) { OperationStatus = $"Czytnik {slot} jest już zajęty."; return; }
        if (_crew.Engine.VehicleMoving) { OperationStatus = "Nie można wkładać karty podczas jazdy. Zatrzymaj pojazd."; return; }
        _cardDialogSlot = slot;
        OnPropertyChanged(nameof(CardDialogSlotText));
        var occupiedCard = slot == 1 ? Card2Number : CardNumber;
        AvailableCardProfiles.Clear();
        foreach (var profile in Profiles.Where(x => x.Cards.All(card =>
                     !string.Equals(card.CardNumber, occupiedCard, StringComparison.OrdinalIgnoreCase))))
            AvailableCardProfiles.Add(profile);
        SelectedProfile = AvailableCardProfiles.FirstOrDefault();
        if (SelectedProfile is null) { OperationStatus = "Najpierw utwórz profil kierowcy."; return; }
        _cardDialogIsInsertion = true;
        CardDialogTitle = $"WŁOŻENIE KARTY - CZYTNIK {slot}";
        CardDialogMessage = "Potwierdź kierowcę i kraj rozpoczęcia zmiany. Karta zostanie przypisana do kierowcy prowadzącego.";
        IsCardDialogVisible = true;
    }

    private void OpenCardEjection() => OpenCardEjection(1);
    private void OpenCardEjection(int slot)
    {
        if ((slot == 1 && !IsCardInserted) || (slot == 2 && !IsCard2Inserted)) { OperationStatus = $"W czytniku {slot} nie ma karty."; return; }
        if (_currentSpeed > 0.5)
        {
            OperationStatus = "Nie można wyjąć karty podczas jazdy. Zatrzymaj pojazd.";
            return;
        }
        _cardDialogSlot = slot;
        OnPropertyChanged(nameof(CardDialogSlotText));
        AvailableCardProfiles.Clear();
        var currentCard = slot == 1 ? CardNumber : Card2Number;
        if (FindProfileByCard(currentCard) is { } currentProfile)
        {
            AvailableCardProfiles.Add(currentProfile);
            SelectedProfile = currentProfile;
        }
        _cardDialogIsInsertion = false;
        CardDialogTitle = $"WYJĘCIE KARTY - CZYTNIK {slot}";
        CardDialogMessage = "Wybierz kraj zakończenia zmiany. Dane zostaną zapisane przed wysunięciem karty.";
        IsCardDialogVisible = true;
    }

    private async Task ConfirmCardOperationAsync()
    {
        if (string.IsNullOrWhiteSpace(CardCountry)) { OperationStatus = "Podaj kod kraju, np. PL."; return; }
        if (_cardDialogIsInsertion)
        {
            var card = SelectedProfile?.Cards.FirstOrDefault();
            if (SelectedProfile is null || card is null) { OperationStatus = "Wybrany profil nie ma karty kierowcy."; return; }
            var otherCard = _cardDialogSlot == 1 ? Card2Number : CardNumber;
            if (string.Equals(card.CardNumber, otherCard, StringComparison.OrdinalIgnoreCase))
            {
                OperationStatus = "Ta karta jest już w drugim slocie.";
                return;
            }
            try
            {
                await _crew.InsertCardAsync(
                    _cardDialogSlot == 1 ? TachographSlot.Driver : TachographSlot.CoDriver,
                    card.CardNumber,
                    _cancellation.Token);
            }
            catch (InvalidOperationException exception)
            {
                _diagnostics.Error("CARD_INSERT_REJECTED", exception);
                OperationStatus = exception.Message;
                return;
            }
            if (_cardDialogSlot == 1)
            {
                IsCardInserted = true; CardOwner = SelectedProfile.DisplayName; CardNumber = card.CardNumber;
                RestoreRestStateForSlot(card.CardNumber, 1);
            }
            else
            {
                IsCard2Inserted = true; Card2Owner = SelectedProfile.DisplayName; Card2Number = card.CardNumber;
                RestoreRestStateForSlot(card.CardNumber, 2);
            }
            OperationStatus = $"Karta odczytana. Kraj rozpoczęcia: {CardCountry}. Można rozpocząć jazdę.";
            _diagnostics.Info("CARD_INSERTED", $"Slot {_cardDialogSlot}: {MaskCard(card.CardNumber)}, kraj {CardCountry}.");
            StartCountry = CardCountry;
            _isCardLoading = true;
            IsCardDialogVisible = false;
            UpdateDeviceDisplay();
            await Task.Delay(2200);
            _isCardLoading = false;
        }
        else
        {
            var slot = _cardDialogSlot == 1 ? TachographSlot.Driver : TachographSlot.CoDriver;
            var ejectedCard = _cardDialogSlot == 1 ? CardNumber : Card2Number;
            CaptureRestStateForCard(ejectedCard, _cardDialogSlot);
            try
            {
                var result = await _crew.EjectCardAsync(
                    slot,
                    _crew.Current.Frame?.RecordedAtUtc ?? DateTimeOffset.UtcNow,
                    _cancellation.Token);
                foreach (var record in result.Snapshot.CompletedRecords.Where(x => History.All(old => old.Id != x.Id)))
                    History.Add(record);
            }
            catch (InvalidOperationException exception)
            {
                _diagnostics.Error("CARD_EJECT_REJECTED", exception);
                OperationStatus = exception.Message;
                return;
            }
            if (_cardDialogSlot == 1) { IsCardInserted = false; CardOwner = "---"; CardNumber = "BRAK KARTY"; }
            else { IsCard2Inserted = false; Card2Owner = "---"; Card2Number = "BRAK KARTY"; }
            OperationStatus = $"Karta wysunięta. Kraj zakończenia: {CardCountry}.";
            _diagnostics.Info("CARD_EJECTED", $"Slot {_cardDialogSlot}: {MaskCard(ejectedCard)}, kraj {CardCountry}.");
            EndCountry = CardCountry;
        }
        IsCardDialogVisible = false;
        Refresh(_crew.Current);
        await RefreshActivityGapsAsync(_cancellation.Token);
        SaveDeviceState();
    }

    private void HandleManualEntryState(CrewTachographSnapshot snapshot)
    {
        var required = snapshot.Driver?.RequiredManualEntryGap is { } driverGap
            ? (Gap: driverGap, Slot: 1)
            : snapshot.CoDriver?.RequiredManualEntryGap is { } coDriverGap
                ? (Gap: coDriverGap, Slot: 2)
                : ((ActivityGap Gap, int Slot)?)null;

        if (required is not null)
        {
            OpenManualEntry(required.Value.Gap, required.Value.Slot, forced: true);
            if (_currentSpeed > _drivingThreshold)
            {
                var message = $"Jazda zablokowana przez tachograf: rozlicz wpis manualny w slocie {required.Value.Slot}.";
                if (!string.Equals(OperationStatus, message, StringComparison.Ordinal))
                    OperationStatus = message;
            }
        }
        else if (IsManualEntryVisible && IsManualEntryForced)
        {
            IsManualEntryVisible = false;
        }

        var optional = snapshot.Driver?.OptionalManualEntryGap is { } driverOptional
            ? (Gap: driverOptional, Slot: 1)
            : snapshot.CoDriver?.OptionalManualEntryGap is { } coDriverOptional
                ? (Gap: coDriverOptional, Slot: 2)
                : ((ActivityGap Gap, int Slot)?)null;
        var previousOptionalGapId = _optionalManualEntryGap?.Id;
        _optionalManualEntryGap = optional?.Gap;
        if (optional is null &&
            previousOptionalGapId is not null &&
            IsManualEntryVisible &&
            !IsManualEntryForced &&
            _manualEntryGap?.Id == previousOptionalGapId)
        {
            IsManualEntryVisible = false;
            _manualEntryGap = null;
            OperationStatus = "Skok czasu rozpoznany jako załadunek lub rozładunek — luka została wycofana.";
        }
        OnPropertyChanged(nameof(HasOptionalManualEntryGap));
        if (optional is not null && optional.Value.Gap.Id != _lastOptionalGapWarningId)
        {
            _lastOptionalGapWarningId = optional.Value.Gap.Id;
            _diagnostics.Warning(
                "OPTIONAL_MANUAL_ENTRY",
                $"Slot {optional.Value.Slot}: nierozliczona luka po skoku czasu {Format(optional.Value.Gap.DurationMinutes ?? 0)}.");
            OperationStatus =
                $"Wykryto lukę po skoku czasu w slocie {optional.Value.Slot}. Wpis jest opcjonalny i nie blokuje jazdy.";
        }
    }

    private void OpenOptionalManualEntry()
    {
        if (_optionalManualEntryGap is null) return;
        var slot = string.Equals(
            _crew.Current.DriverCardId,
            _optionalManualEntryGap.DriverCardId,
            StringComparison.OrdinalIgnoreCase) ? 1 : 2;
        OpenManualEntry(_optionalManualEntryGap, slot, forced: false);
    }

    private void OpenManualEntryFromHistory(ActivityGapListItemDto gap)
    {
        if (!gap.IsResolvable || gap.EndGameMinute is null) return;
        OpenManualEntry(
            new ActivityGap
            {
                Id = gap.GapId,
                DriverCardId = gap.DriverCardId,
                Slot = gap.Slot,
                SessionIndex = 0,
                Start = new GameTime(gap.StartGameMinute),
                EndExclusive = new GameTime(gap.EndGameMinute.Value),
                Reason = gap.Reason,
                State = gap.State,
                ResolvedAt = gap.ResolvedAtGameMinute is { } resolvedAt
                    ? new GameTime(resolvedAt)
                    : null
            },
            gap.Slot,
            forced: false);
    }

    private void OpenManualEntry(ActivityGap gap, int slot, bool forced)
    {
        if (gap.EndExclusive is null) return;
        if (IsManualEntryVisible && _manualEntryGap?.Id == gap.Id)
        {
            if (forced) IsManualEntryForced = true;
            return;
        }

        _manualEntryGap = gap;
        _manualEntrySlot = slot;
        IsManualEntryForced = forced;
        ManualEntryRangeText = $"{GameClockFormatter.Format(gap.Start)}  →  {GameClockFormatter.Format(gap.EndExclusive.Value)}";
        ManualEntryDurationText = $"Długość luki: {Format(gap.DurationMinutes ?? 0)}";
        ManualEntryWorkFrom = string.Empty;
        ManualEntryWorkTo = string.Empty;
        ManualEntryWorkBlocks.Clear();
        SelectedManualWorkBlock = null;
        ManualEntryValidationMessage = string.Empty;
        ManualEntrySelectionMessage = "Wybrany cel: cała luka jako Przerwa / Odpoczynek.";
        ManualEntryQualificationMessage = "Zakwalifikowano: oczekuje na zatwierdzenie wpisu.";
        OnPropertyChanged(nameof(ManualEntryTitle));
        OnPropertyChanged(nameof(HasOptionalManualEntryGap));
        IsManualEntryVisible = true;
        _deviceMenuOpen = false;
    }

    private void AddManualWorkBlock()
    {
        if (_manualEntryGap is null) return;
        try
        {
            var from = ParseManualEntryTime(ManualEntryWorkFrom, "OD");
            var to = ParseManualEntryTime(ManualEntryWorkTo, "DO");
            var candidate = ManualEntryWorkBlocks
                .Select(row => new ManualEntryWorkBlock(row.FromGameMinute, row.ToGameMinuteExclusive))
                .Append(new ManualEntryWorkBlock(from.TotalMinutes, to.TotalMinutes))
                .ToList();
            ManualEntryWizardDraft.Build(_manualEntryGap, candidate);
            var rows = candidate
                .OrderBy(block => block.FromGameMinute)
                .Select(block => new ManualEntryWorkBlockRow(
                    block.FromGameMinute,
                    block.ToGameMinuteExclusive))
                .ToList();
            ManualEntryWorkBlocks.Clear();
            foreach (var row in rows) ManualEntryWorkBlocks.Add(row);
            ManualEntryWorkFrom = string.Empty;
            ManualEntryWorkTo = string.Empty;
            ManualEntryValidationMessage = string.Empty;
            UpdateManualEntrySelectionMessage();
        }
        catch (Exception exception) when (exception is FormatException or ManualEntryDraftException)
        {
            ManualEntryValidationMessage = exception.Message;
        }
    }

    private void RemoveManualWorkBlock()
    {
        if (SelectedManualWorkBlock is null) return;
        ManualEntryWorkBlocks.Remove(SelectedManualWorkBlock);
        SelectedManualWorkBlock = null;
        ManualEntryValidationMessage = string.Empty;
        UpdateManualEntrySelectionMessage();
    }

    private void UpdateManualEntrySelectionMessage()
    {
        if (_manualEntryGap is null) return;
        var work = ManualEntryWorkBlocks.Sum(row =>
            row.ToGameMinuteExclusive - row.FromGameMinute);
        var rest = Math.Max(0, (_manualEntryGap.DurationMinutes ?? 0) - work);
        ManualEntrySelectionMessage = work == 0
            ? "Wybrany cel: cała luka jako Przerwa / Odpoczynek."
            : $"Wybrany cel: odpoczynek {Format(rest)}, Inna praca {Format(work)}.";
    }

    private async Task ConfirmManualEntryAsync()
    {
        if (_manualEntryGap is null || _manualEntryGap.EndExclusive is null) return;
        try
        {
            var work = ManualEntryWorkBlocks.Select(row =>
                new ManualEntryWorkBlock(row.FromGameMinute, row.ToGameMinuteExclusive)).ToList();
            var segments = ManualEntryWizardDraft.Build(_manualEntryGap, work);
            ManualEntryValidator.Validate(_manualEntryGap, segments, []);
            var now = _crew.Current.Frame?.GameTime ?? _manualEntryGap.EndExclusive.Value;
            var result = await _manualEntries.ResolveGapAsync(
                _manualEntryGap.Id,
                segments,
                now,
                _cancellation.Token);
            _crew.Engine.ApplyManualEntryResolution(
                _manualEntryGap.DriverCardId,
                result.Gap,
                result.Segments);

            var qualified = result.Evaluation.QualifiedRests.SingleOrDefault(rest =>
                rest.SourceGapId == result.Gap.Id);
            ManualEntryQualificationMessage = qualified is null
                ? "Zakwalifikowano: brak ciągłego odpoczynku 9 h — bez resetu dobowego."
                : QualificationText(qualified);
            _diagnostics.Info(
                "MANUAL_ENTRY_RESOLVED",
                $"Slot {_manualEntrySlot}: luka {_manualEntryGap.Id}, {ManualEntrySelectionMessage} {ManualEntryQualificationMessage}");
            OperationStatus = $"Wpis manualny zapisany. {ManualEntryQualificationMessage}";
            IsManualEntryVisible = false;
            _manualEntryGap = null;
            await ReloadHistoryAsync(_cancellation.Token);
            Refresh(_crew.Current);
            await RefreshActivityGapsAsync(_cancellation.Token);
        }
        catch (Exception exception) when (exception is
                   FormatException or
                   ManualEntryDraftException or
                   ManualEntryValidationException or
                   InvalidOperationException)
        {
            ManualEntryValidationMessage = exception.Message;
            _diagnostics.Error("MANUAL_ENTRY_REJECTED", exception);
        }
    }

    private void CancelManualEntry()
    {
        if (IsManualEntryForced)
        {
            ManualEntryValidationMessage = "Ta luka powstała po wyjęciu karty i musi zostać rozliczona.";
            return;
        }
        IsManualEntryVisible = false;
        _manualEntryGap = null;
        OnPropertyChanged(nameof(HasOptionalManualEntryGap));
    }

    private static GameTime ParseManualEntryTime(string value, string label)
    {
        if (GameClockFormatter.TryParse(value, out var time)) return time;
        throw new FormatException($"Pole {label}: wpisz np. Dzień 12, 14:30.");
    }

    private static string QualificationText(QualifiedRestPeriod rest)
    {
        var daily = rest.DailyClassification == DailyRestClassification.Regular
            ? "odpoczynek dobowy regularny"
            : "odpoczynek dobowy skrócony";
        var weekly = rest.WeeklyClassification switch
        {
            WeeklyRestClassification.Regular => ", tygodniowy regularny",
            WeeklyRestClassification.Reduced => ", tygodniowy skrócony",
            _ => string.Empty
        };
        return $"Zakwalifikowano: {daily}{weekly}; reset o {GameClockFormatter.Format(rest.EndExclusive)}.";
    }

    private void CycleDriverActivity(int driver)
    {
        if (driver == 1)
        {
            if (_crew.Engine.VehicleMoving) { OperationStatus = "Podczas jazdy aktywność kierowcy 1 jest ustawiana automatycznie."; return; }
            var current = _crew.Current.Driver?.ManualActivity ?? DriverActivity.OtherWork;
            SetActivity(NextActivity(current));
        }
        else
        {
            if (!IsCard2Inserted) { OperationStatus = "Włóż kartę do czytnika 2."; return; }
            if (_crew.Engine.VehicleMoving)
            {
                StartSelectedRest2();
                return;
            }
            var current = _crew.Current.CoDriver?.ManualActivity ?? DriverActivity.Availability;
            SetActivity(TachographSlot.CoDriver, NextActivity(current));
        }
    }

    private static DriverActivity NextActivity(DriverActivity activity) => activity switch
    {
        DriverActivity.BreakOrRest => DriverActivity.OtherWork,
        DriverActivity.OtherWork => DriverActivity.Availability,
        _ => DriverActivity.BreakOrRest
    };

    private async Task DeviceOkAsync()
    {
        if (_crew.Current.ManualEntryRequired)
        {
            HandleManualEntryState(_crew.Current);
            OperationStatus = "Najpierw zatwierdź wymagany wpis manualny.";
            return;
        }
        if (!_deviceMenuOpen)
        {
            _deviceMenuOpen = true; _deviceMenuPage = "root"; _menuIndex = 0;
        }
        else if (_deviceMenuPage == "root")
        {
            _deviceMenuPage = _menuIndex switch
            {
                0 => "print",
                1 => "manual",
                2 => "rest-target",
                3 => "countries",
                4 => "modes",
                5 => "counter-cards",
                _ => "settings"
            };
            _menuIndex = 0;
        }
        else if (_deviceMenuPage == "print")
        {
            IsPrinting = true; UpdateDeviceDisplay(); await Task.Delay(3500);
            await PrintDailyReportAsync();
            IsPrinting = false; CloseDeviceMenu();
        }
        else if (_deviceMenuPage == "manual")
        {
            if (_menuIndex == 2) StartSelectedRest();
            else SetActivity(_menuIndex == 0 ? DriverActivity.OtherWork : DriverActivity.Availability);
            CloseDeviceMenu();
        }
        else if (_deviceMenuPage == "rest-target")
        {
            SelectedRestTarget = AvailableRestTargets[_menuIndex];
            StartSelectedRest();
            CloseDeviceMenu();
        }
        else if (_deviceMenuPage == "countries")
        {
            _deviceMenuPage = _menuIndex == 0 ? "country-start" : "country-end";
            _menuIndex = Math.Max(0, Array.IndexOf(Countries, _menuIndex == 0 ? StartCountry : EndCountry));
        }
        else if (_deviceMenuPage is "country-start" or "country-end")
        {
            if (_deviceMenuPage == "country-start") StartCountry = Countries[_menuIndex];
            else EndCountry = Countries[_menuIndex];
            OperationStatus = $"Zapisano kraj: {Countries[_menuIndex]}.";
            SaveDeviceState(); CloseDeviceMenu();
        }
        else if (_deviceMenuPage == "modes")
        {
            if (_menuIndex == 0) ToggleOutMode();
            else ToggleFerryMode();
        }
        else if (_deviceMenuPage == "counter-cards")
        {
            _deviceMenuPage = _menuIndex == 0 ? "counters-1" : "counters-2";
            _menuIndex = 0;
        }
        else if (_deviceMenuPage == "settings")
        {
            OperationStatus = "Ustawienia szczegółowe są dostępne w lewym panelu.";
            CloseDeviceMenu();
        }
        UpdateDeviceDisplay();
    }

    private void MoveMenu(int delta)
    {
        if (!_deviceMenuOpen) return;
        var count = MenuItems().Length;
        _menuIndex = (_menuIndex + delta + count) % count;
        UpdateDeviceDisplay();
    }

    private void DeviceCancel()
    {
        if (_deviceMenuPage is "counters-1" or "counters-2") { _deviceMenuPage = "counter-cards"; _menuIndex = 0; }
        else if (_deviceMenuPage != "root") { _deviceMenuPage = "root"; _menuIndex = 0; }
        else CloseDeviceMenu();
        UpdateDeviceDisplay();
    }

    private void CloseDeviceMenu()
    {
        _deviceMenuOpen = false; _deviceMenuPage = "root"; _menuIndex = 0;
    }

    private string[] MenuItems() => _deviceMenuPage switch
    {
        "root" => ["WYDRUK", "WPIS MANUALNY", "PAUZA / ODPOCZ.", "KRAJE", "TRYBY", "LICZNIKI KART", "USTAWIENIA"],
        "print" => ["24H KIEROWCY 1", "24H POJAZDU"],
        "manual" => ["INNA PRACA", "DYSPOZYCYJNOŚĆ", "ODPOCZYNEK"],
        "rest-target" => AvailableRestTargets.Select(x => x.DeviceLabel).ToArray(),
        "countries" => [$"START: {StartCountry}", $"KONIEC: {EndCountry}"],
        "country-start" or "country-end" => Countries,
        "modes" => [$"OUT {OnOff(_crew.Current.Driver?.OutModeEnabled == true)}", $"PROM {OnOff(_crew.Current.Driver?.FerryModeEnabled == true)}"],
        "counter-cards" => [$"KARTA 1 {(IsCardInserted ? "GOTOWA" : "BRAK")}", $"KARTA 2 {(IsCard2Inserted ? "GOTOWA" : "BRAK")}"],
        "counters-1" => [$"PAUZA {RestElapsed}", $"CEL {RestRemaining}", $"CIĄGŁA {ContinuousDriving}", $"DO PRZERWY {UntilBreak}", $"DZIENNA {DailyDrivingWithLimit}", $"PRACA {DailyWorkWithLimit}", $"TYDZIEŃ {WeeklyDriving}", $"2 TYG. {FortnightlyDriving}", $"ODP. DZIENNY {DailyRestDeadline}", $"ODP. TYG. {WeeklyRestDeadline}", $"REKOMPENSATA {CompensationText}", $"WYDŁUŻENIA {DailyExtensionsUsage} · TYDZIEŃ", $"SKRÓCONE {ReducedDailyRestsUsage} · OD ODP. TYG."],
        "counters-2" => [$"PAUZA {RestElapsed2}", $"CEL {RestRemaining2}", $"CIĄGŁA {Driver2ContinuousDriving}", $"DO PRZERWY {Driver2UntilBreak}", $"DZIENNA {Driver2DailyDrivingWithLimit}", $"PRACA {Driver2DailyWorkWithLimit}", $"TYDZIEŃ {Driver2WeeklyDriving}", $"2 TYG. {Driver2FortnightlyDriving}", $"ODP. DZIENNY {Driver2DailyRestDeadline}", $"ODP. TYG. {Driver2WeeklyRestDeadline}", $"REKOMPENSATA {Driver2CompensationText}", $"WYDŁUŻENIA {Driver2DailyExtensionsUsage} · TYDZIEŃ", $"SKRÓCONE {Driver2ReducedDailyRestsUsage} · OD ODP. TYG."],
        _ => ["PRÓG PRĘDKOŚCI", "TYDZIEŃ REGULACYJNY"]
    };

    private static string OnOff(bool enabled) => enabled ? "WŁ." : "WYŁ.";

    private async Task PrintDailyReportAsync()
    {
        var report = await _reports.CreateAsync(CurrentDriverCardId);
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ETS2Tachograph", "Printouts");
        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, $"wydruk-24h-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
        await using var stream = File.Create(file);
        await _pdfReports.ExportAsync(report, stream);
        OperationStatus = $"Wydruk zapisany: {file}";
    }

    private void UpdateDeviceDisplay()
    {
        _warningBlink = !_warningBlink;
        DeviceLine2Foreground = "#111A12";
        if (_isCardLoading)
        {
            DeviceLine1 = $"KARTA {_cardDialogSlot} - ODCZYT";
            DeviceLine2 = SelectedProfile?.DisplayName?.ToUpperInvariant() ?? "KIEROWCA";
            DeviceLine3 = "[##########]  OK"; return;
        }
        if (_crew.Current.ManualEntryRequired)
        {
            DeviceLine1 = "! WPIS MANUALNY !";
            DeviceLine2 = $"SLOT {_manualEntrySlot}  WYMAGANY";
            DeviceLine3 = _warningBlink ? "JAZDA ZABLOKOWANA" : "POTWIERDŹ AKTYWNOŚĆ";
            return;
        }
        if (IsPrinting)
        {
            DeviceLine1 = "DRUKOWANIE..."; DeviceLine2 = "24H KIEROWCY 1"; DeviceLine3 = "[########--]"; return;
        }
        if (_deviceMenuOpen)
        {
            var items = MenuItems();
            DeviceLine1 = _deviceMenuPage switch
            {
                "root" => "MENU GŁÓWNE",
                "rest-target" => "WYBIERZ PAUZĘ",
                "counter-cards" => "WYBIERZ KARTĘ",
                "counters-1" => "LICZNIKI KARTY 1",
                "counters-2" => "LICZNIKI KARTY 2",
                "country-start" => "KRAJ START",
                "country-end" => "KRAJ KONIEC",
                _ => _deviceMenuPage.ToUpperInvariant()
            };
            var selectedItem = items[_menuIndex];
            DeviceLine2Foreground = IsExceededCounterItem(selectedItem) ? "#B42318" : "#111A12";
            DeviceLine2 = $"> {selectedItem}";
            DeviceLine3 = "▲/▼   OK   C"; return;
        }
        var driver = _crew.Current.Driver;
        var coDriver = _crew.Current.CoDriver;
        var gameClock = _crew.Current.Frame is { } frame
            ? GameClockFormatter.FormatTimeOfDay(frame.GameTime)
            : "--:--";
        var activity1 = _currentSpeed > 0.5 ? "KIEROWNICA" : driver is null ? "BRAK KARTY" : ActivityLabel(driver.ManualActivity);
        var activity2 = coDriver is null ? "BRAK KARTY" : ActivityLabel(coDriver.ProvisionalActivity ?? coDriver.ManualActivity);
        DeviceLine1 = $"{gameClock}       {_currentSpeed,3:0} km/h";
        DeviceLine2 = $"1 {activity1,-11} 2 {activity2}";
        DeviceLine3 = !IsCardInserted && _currentSpeed > 0.5
            ? (_warningBlink ? "! JAZDA BEZ KARTY !" : "X  BŁĄD KARTY 1  X")
            : IsCardInserted && _currentSpeed <= 0.5 && driver?.ManualActivity == DriverActivity.BreakOrRest
                ? $"P {RestElapsed}  > {RestRemaining}"
                : $"{_odometer:000000.0} km  {(IsCardInserted ? "K1" : "BRAK K1")}";
    }

    private static string ActivityLabel(DriverActivity activity) => activity switch
    {
        DriverActivity.BreakOrRest => "ŁÓŻKO",
        DriverActivity.OtherWork => "MŁOTKI",
        DriverActivity.Availability => "GOTOWOŚĆ",
        _ => activity.ToString().ToUpperInvariant()
    };

    private bool IsExceededCounterItem(string item) =>
        item.StartsWith("WYDŁUŻENIA", StringComparison.Ordinal) &&
        (_deviceMenuPage == "counters-1" ? DailyExtensionsExceeded : Driver2DailyExtensionsExceeded) ||
        item.StartsWith("SKRÓCONE", StringComparison.Ordinal) &&
        (_deviceMenuPage == "counters-1" ? ReducedDailyRestsExceeded : Driver2ReducedDailyRestsExceeded) ||
        item.StartsWith("REKOMPENSATA", StringComparison.Ordinal) &&
        (_deviceMenuPage == "counters-1" ? CompensationOverdue : Driver2CompensationOverdue);

    private string CurrentDriverCardId => _crew.Current.DriverCardId ?? _defaultDriverCardId;
    private string CurrentReportCardId => ReportDriverProfile?.Cards.FirstOrDefault()?.CardNumber ?? CurrentDriverCardId;

    private static string DeviceStatePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ETS2Tachograph", "device-state.json");

    private void LoadDeviceState()
    {
        try
        {
            if (!File.Exists(DeviceStatePath)) return;
            var state = JsonSerializer.Deserialize<DevicePersistentState>(File.ReadAllText(DeviceStatePath));
            if (state is null) return;
            _odometer = state.Odometer;
            StartCountry = state.StartCountry;
            EndCountry = state.EndCountry;
            if (state.CardRestStates is not null)
                foreach (var pair in state.CardRestStates)
                    _cardRestStates[pair.Key] = pair.Value;
            if (state.Card1Inserted && !_cardRestStates.ContainsKey(state.Card1Number))
                _cardRestStates[state.Card1Number] = new RestCardPersistentState(state.RestTargetMinutes, state.RestStartedAtGameMinute);
            if (state.Card2Inserted && !_cardRestStates.ContainsKey(state.Card2Number))
                _cardRestStates[state.Card2Number] = new RestCardPersistentState(state.RestTargetMinutes2, state.RestStartedAtGameMinute2);

            if (state.Card1Inserted && FindProfileByCard(state.Card1Number) is { } profile1)
            {
                _crew.InsertCard(TachographSlot.Driver, state.Card1Number);
                IsCardInserted = true; CardOwner = profile1.DisplayName; CardNumber = state.Card1Number;
                RestoreRestStateForSlot(state.Card1Number, 1);
                if (_crew.Current.Driver?.RequiredManualEntryGap is null)
                {
                    _crew.Engine.SetManualActivity(TachographSlot.Driver, state.Driver1Activity);
                    _crew.Engine.SetOutMode(TachographSlot.Driver, state.OutMode);
                    _crew.Engine.SetFerryMode(TachographSlot.Driver, state.FerryMode);
                }
            }
            if (state.Card2Inserted &&
                !string.Equals(state.Card2Number, CardNumber, StringComparison.OrdinalIgnoreCase) &&
                FindProfileByCard(state.Card2Number) is { } profile2)
            {
                _crew.InsertCard(TachographSlot.CoDriver, state.Card2Number);
                IsCard2Inserted = true; Card2Owner = profile2.DisplayName; Card2Number = state.Card2Number;
                RestoreRestStateForSlot(state.Card2Number, 2);
                if (_crew.Current.CoDriver?.RequiredManualEntryGap is null)
                    _crew.Engine.SetManualActivity(TachographSlot.CoDriver, state.Driver2Activity);
            }
            Refresh(_crew.Current);
            UpdateDeviceDisplay();
        }
        catch (Exception exception)
        {
            OperationStatus = $"Nie udało się odtworzyć stanu urządzenia: {exception.Message}";
        }
    }

    private void SaveDeviceState()
    {
        try
        {
            if (IsCardInserted) CaptureRestStateForCard(CardNumber, 1);
            if (IsCard2Inserted) CaptureRestStateForCard(Card2Number, 2);
            Directory.CreateDirectory(Path.GetDirectoryName(DeviceStatePath)!);
            var driver = _crew.Current.Driver;
            var coDriver = _crew.Current.CoDriver;
            var state = new DevicePersistentState(
                _odometer, StartCountry, EndCountry,
                driver?.ManualActivity ?? DriverActivity.OtherWork,
                coDriver?.ManualActivity ?? DriverActivity.Availability,
                driver?.OutModeEnabled == true, driver?.FerryModeEnabled == true, _crew.Current.MultiManning,
                IsCardInserted, CardOwner, CardNumber, IsCard2Inserted, Card2Owner, Card2Number,
                SelectedRestTarget.Minutes, _restStartedAtGameMinute,
                SelectedRestTarget2.Minutes, _restStartedAtGameMinute2,
                new Dictionary<string, RestCardPersistentState>(_cardRestStates, StringComparer.OrdinalIgnoreCase));
            File.WriteAllText(DeviceStatePath, JsonSerializer.Serialize(state));
        }
        catch (Exception exception)
        {
            OperationStatus = $"Nie udało się zapisać stanu urządzenia: {exception.Message}";
        }
    }

    private sealed record DevicePersistentState(
        double Odometer,
        string StartCountry,
        string EndCountry,
        DriverActivity Driver1Activity,
        DriverActivity Driver2Activity,
        bool OutMode,
        bool FerryMode,
        bool MultiManning,
        bool Card1Inserted,
        string Card1Owner,
        string Card1Number,
        bool Card2Inserted,
        string Card2Owner,
        string Card2Number,
        int RestTargetMinutes = 45,
        long? RestStartedAtGameMinute = null,
        int RestTargetMinutes2 = 45,
        long? RestStartedAtGameMinute2 = null,
        Dictionary<string, RestCardPersistentState>? CardRestStates = null);

    private sealed record RestCardPersistentState(int TargetMinutes, long? StartedAtGameMinute);

    private DriverProfileDto? FindProfileByCard(string cardNumber) => Profiles.FirstOrDefault(profile =>
        profile.Cards.Any(card => string.Equals(card.CardNumber, cardNumber, StringComparison.OrdinalIgnoreCase)));

    private void CaptureRestStateForCard(string cardNumber, int slot)
    {
        if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.StartsWith("BRAK", StringComparison.OrdinalIgnoreCase)) return;
        _cardRestStates[cardNumber] = slot == 1
            ? new RestCardPersistentState(SelectedRestTarget.Minutes, _restStartedAtGameMinute)
            : new RestCardPersistentState(SelectedRestTarget2.Minutes, _restStartedAtGameMinute2);
    }

    private void RestoreRestStateForSlot(string cardNumber, int slot)
    {
        var state = _cardRestStates.GetValueOrDefault(cardNumber) ?? new RestCardPersistentState(45, null);
        var target = AvailableRestTargets.FirstOrDefault(x => x.Minutes == state.TargetMinutes) ?? AvailableRestTargets[2];
        if (slot == 1)
        {
            _selectedRestTarget = target; _restStartedAtGameMinute = state.StartedAtGameMinute;
            OnPropertyChanged(nameof(SelectedRestTarget)); OnPropertyChanged(nameof(RestTargetText));
        }
        else
        {
            _selectedRestTarget2 = target; _restStartedAtGameMinute2 = state.StartedAtGameMinute;
            OnPropertyChanged(nameof(SelectedRestTarget2)); OnPropertyChanged(nameof(RestTargetText2));
        }
    }
    private async Task CreateProfileAsync()
    {
        try
        {
            var profile = await _drivers.CreateProfileAsync(new CreateDriverProfileDto(
                NewDriverName,
                new DriverCardDto(NewCardNumber, "PL", DateOnly.FromDateTime(DateTime.Today),
                    DateOnly.FromDateTime(DateTime.Today.AddYears(5)))));
            Profiles.Add(profile);
            foreach (var card in profile.Cards)
                await _crew.RegisterCardAsync(card.CardNumber, _cancellation.Token);
            NewDriverName = string.Empty; NewCardNumber = string.Empty;
            OperationStatus = "Profil utworzony.";
        }
        catch (Exception ex) { OperationStatus = ex.Message; }
    }

    private async Task ActivateProfileAsync()
    {
        if (SelectedProfile is null) return;
        await _drivers.SetActiveProfileAsync(SelectedProfile.Id);
        OperationStatus = "Profil zapisany. Uruchom aplikację ponownie, aby rozpocząć jego sesję.";
    }

    private async Task SaveSettingsAsync()
    {
        try { await _settings.SaveAsync(new SettingsDto(DrivingThreshold, WeekOffset)); OperationStatus = "Ustawienia zapisane. Zostaną zastosowane przy następnym uruchomieniu."; }
        catch (Exception ex) { OperationStatus = ex.Message; }
    }

    private async Task ExportAsync()
    {
        var cardId = CurrentDriverCardId;
        var dialog = new SaveFileDialog { Filter = "Sesja tachografu (*.tacho)|*.tacho", FileName = $"{cardId}.tacho" };
        if (dialog.ShowDialog() != true) return;
        await using var stream = File.Create(dialog.FileName);
        await _export.ExportSessionAsync(cardId, stream);
        OperationStatus = "Eksport zakończony.";
    }

    private async Task ImportAsync()
    {
        var dialog = new OpenFileDialog { Filter = "Sesja tachografu (*.tacho)|*.tacho" };
        if (dialog.ShowDialog() != true) return;
        try
        {
            await using var stream = File.OpenRead(dialog.FileName);
            var count = await _import.ImportSessionAsync(stream);
            await ReloadHistoryAsync(_cancellation.Token);
            OperationStatus = $"Zaimportowano {count} rekordów.";
            await RefreshReportAsync();
        }
        catch (Exception ex) { OperationStatus = ex.Message; }
    }

    private async Task RefreshReportAsync()
    {
        _currentReport = null;
        ReportHasUnresolvedGaps = false;
        ReportGapWarningText = string.Empty;
        try
        {
            GameTime? from = ParseOptionalReportTime(ReportFrom, "OD");
            GameTime? to = ParseOptionalReportTime(ReportTo, "DO");
            var effectiveTo = to ?? _crew.Current.Frame?.GameTime;
            _currentReport = await _reports.CreateAsync(CurrentReportCardId, from, effectiveTo);
            ReportDriving = Format(_currentReport.DrivingMinutes);
            ReportWork = Format(_currentReport.OtherWorkMinutes);
            ReportAvailability = Format(_currentReport.AvailabilityMinutes);
            ReportRest = Format(_currentReport.RestMinutes);
            ReportCompensation = FormatCompensation(_currentReport.CompensationSummary);
            ReportCompensationForeground = _currentReport.CompensationSummary.HasOverdue
                ? "#FF6B6B"
                : "#8D9AAD";
            ReportViolationCount = _currentReport.Violations.Count;
            ReportHasUnresolvedGaps = _currentReport.UnresolvedGapCount > 0;
            ReportGapWarningText = ReportHasUnresolvedGaps
                ? FormatReportGapWarning(_currentReport.UnresolvedGapCount, _currentReport.GapMinutes)
                : string.Empty;
            ReportRows.Clear();
            foreach (var record in _currentReport.Records) ReportRows.Add(record);
            OperationStatus = ReportHasUnresolvedGaps
                ? ReportGapWarningText
                : $"Raport: {_currentReport.Records.Count} rekordów. Luki: brak — kompletność potwierdzona.";
        }
        catch (Exception ex) { OperationStatus = ex.Message; }
    }

    private async Task ShowReportGapsAsync()
    {
        if (_showResolvedGaps)
        {
            _showResolvedGaps = false;
            OnPropertyChanged(nameof(ShowResolvedGaps));
        }
        await RefreshActivityGapsAsync(_cancellation.Token);
        SelectedMainTabIndex = 1;
    }

    private void InvalidateCurrentReport()
    {
        _currentReport = null;
        ReportHasUnresolvedGaps = false;
        ReportGapWarningText = string.Empty;
    }

    private async Task ExportReportAsync(string format)
    {
        // Every export re-evaluates the selected game-time range so a newly created
        // unresolved gap can never be hidden by an older report preview.
        await RefreshReportAsync();
        if (_currentReport is null) return;
        var dialog = new SaveFileDialog
        {
            Filter = format switch
            {
                "csv" => "Raport CSV (*.csv)|*.csv",
                "pdf" => "Raport PDF (*.pdf)|*.pdf",
                _ => "VTC JSON (*.json)|*.json"
            },
            FileName = $"raport-{CurrentReportCardId}.{format}"
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            await using var stream = File.Create(dialog.FileName);
            if (format == "csv") await _reports.ExportCompensationCsvAsync(_currentReport, stream);
            else if (format == "pdf") await _pdfReports.ExportAsync(_currentReport, stream);
            else await _reports.ExportVtcJsonAsync(_currentReport, stream);
            OperationStatus = $"Zapisano raport {format.ToUpperInvariant()}.";
        }
        catch (Exception ex)
        {
            _diagnostics.Error("REPORT_EXPORT_FAILED", ex);
            OperationStatus = ex.Message;
        }
    }

    private async Task ExportDiagnosticReportAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Raport diagnostyczny ZIP (*.zip)|*.zip",
            FileName = $"ets2-tachograph-diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.zip"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var snapshot = new DiagnosticSnapshot(
                ConnectionStatus,
                _crew.Current.Frame is null ? "--" : GameClockFormatter.Format(_crew.Current.Frame.GameTime),
                $"{_currentSpeed:0.0} km/h",
                MaskCard(CardNumber),
                ActivityText,
                $"ciągła {ContinuousDriving}; do przerwy {UntilBreak}; dzienna {DailyDriving}; tygodniowa {WeeklyDriving}",
                MaskCard(Card2Number),
                Driver2ActivityText,
                $"ciągła {Driver2ContinuousDriving}; do przerwy {Driver2UntilBreak}; dzienna {Driver2DailyDriving}; tygodniowa {Driver2WeeklyDriving}",
                ModesText,
                History.Count,
                Violations.Count);
            await using var stream = File.Create(dialog.FileName);
            await _diagnostics.CreateReportAsync(stream, snapshot, _cancellation.Token);
            OperationStatus = $"Zapisano raport diagnostyczny: {dialog.FileName}";
        }
        catch (Exception exception)
        {
            _diagnostics.Error("DIAGNOSTIC_REPORT_FAILED", exception);
            OperationStatus = $"Nie udało się utworzyć raportu diagnostycznego: {exception.Message}";
        }
    }

    private async Task ReloadHistoryAsync(CancellationToken cancellationToken = default)
    {
        var records = new List<ActivityRecord>();
        foreach (var card in Profiles.SelectMany(x => x.Cards).DistinctBy(x => x.CardNumber))
            records.AddRange(await _crew.LoadDriverHistoryAsync(card.CardNumber, cancellationToken: cancellationToken));
        History.Clear();
        foreach (var record in records.OrderBy(x => x.Start).ThenBy(x => x.DriverCardId))
            History.Add(record);
    }

    private async Task RefreshActivityGapsSafelyAsync()
    {
        try
        {
            await RefreshActivityGapsAsync(_cancellation.Token);
        }
        catch (OperationCanceledException) when (_cancellation.IsCancellationRequested) { }
        catch (Exception exception)
        {
            _diagnostics.Error("ACTIVITY_GAPS_REFRESH_FAILED", exception);
            OperationStatus = $"Nie udało się odświeżyć listy luk: {exception.Message}";
        }
    }

    private async Task RefreshActivityGapsAsync(CancellationToken cancellationToken)
    {
        var list = await _activityGaps.GetListAsync(
            CurrentGameMinuteForGapDisplay(),
            ShowResolvedGaps,
            cancellationToken: cancellationToken);
        ActivityGaps.Clear();
        foreach (var gap in list.Items)
            ActivityGaps.Add(gap);
        UnresolvedGapCount = list.UnresolvedCount;
    }

    private GameTime CurrentGameMinuteForGapDisplay()
    {
        if (_crew.Current.Frame?.GameTime is { } current)
            return current;
        return new GameTime(History.Count == 0
            ? 0
            : History.Max(record => record.EndExclusive.TotalMinutes));
    }

    private void UpdateOpenGapDurations()
    {
        if (_crew.Current.Frame?.GameTime is not { } current) return;
        for (var index = 0; index < ActivityGaps.Count; index++)
        {
            var gap = ActivityGaps[index];
            if (!gap.IsOpen) continue;
            var duration = Math.Max(0, current.TotalMinutes - gap.StartGameMinute);
            if (duration != gap.DurationMinutes)
                ActivityGaps[index] = gap with { DurationMinutes = duration };
        }
    }

    private async Task RefreshCompensationDetailsAsync()
    {
        try
        {
            var cardId = CompensationDriverProfile?.Cards.FirstOrDefault()?.CardNumber
                ?? CurrentDriverCardId;
            var projection = await _reports.CreateAsync(cardId);
            CompensationDetails.Clear();
            foreach (var row in projection.CompensationObligations
                         .Select(item => CompensationDetailRow.From("KARTA", item))
                         .OrderBy(item => item.IsOpen ? 0 : 1)
                         .ThenBy(item => item.DueAtGameMinuteExclusive)
                         .ThenBy(item => item.ObligationId, StringComparer.Ordinal))
                CompensationDetails.Add(row);
            OnPropertyChanged(nameof(CompensationDetailsHeader));
            OperationStatus = CompensationDetails.Count == 0
                ? "Wybrana karta nie ma zobowiązań rekompensaty."
                : $"Wczytano pełny ślad {CompensationDetails.Count} zobowiązań rekompensaty.";
        }
        catch (Exception exception)
        {
            _diagnostics.Error("COMPENSATION_DETAILS_REFRESH_FAILED", exception);
            OperationStatus = $"Nie udało się wczytać szczegółów rekompensaty: {exception.Message}";
        }
    }

    private void CopyIdentifier(string value)
    {
        try
        {
            Clipboard.SetText(value);
            OperationStatus = "Skopiowano pełny identyfikator do schowka.";
        }
        catch (Exception exception)
        {
            _diagnostics.Error("CLIPBOARD_COPY_FAILED", exception);
            OperationStatus = $"Nie udało się skopiować identyfikatora: {exception.Message}";
        }
    }

    private static string Format(long minutes) => minutes < 0 ? $"-{Format(-minutes)}" : $"{minutes / 60:00}:{minutes % 60:00}";
    private static string FormatReportGapWarning(int count, long minutes)
    {
        var noun = count == 1
            ? "nierozliczona luka"
            : count % 10 is >= 2 and <= 4 && count % 100 is not (>= 12 and <= 14)
                ? "nierozliczone luki"
                : "nierozliczonych luk";
        var verb = count == 1 ? "jest" : "są";
        return $"W wybranym zakresie {verb} {count} {noun} (łącznie {Format(minutes)}). Raport będzie niekompletny.";
    }
    private static string FormatWithLimit(long minutes, long limitMinutes) => $"{Format(minutes)} / {Format(limitMinutes)}";
    private static string FormatUsage(int used, int limit) => $"{used} / {limit}";
    private static string FormatCompensation(CompensationSummary summary)
    {
        if (summary.Count == 0)
            return "—";

        var count = summary.Count > 1 ? $" ({summary.Count})" : string.Empty;
        var status = summary.HasOverdue
            ? "PRZETERMINOWANA"
            : $"DO TYG. {summary.NearestDueByEndOfWeek!.Value.Index}";
        return $"{Format(summary.TotalOwedMinutes)}{count} · {status}";
    }

    private static string FormatDailyDrivingWithLimit(RegulationState state)
    {
        var limit = state.DailyDrivingMinutes > 9 * 60 && state.DailyExtensionsUsedThisWeek <= 2
            ? 10 * 60
            : 9 * 60;
        return FormatWithLimit(state.DailyDrivingMinutes, limit);
    }

    private static GameTime? ParseOptionalReportTime(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (GameClockFormatter.TryParse(value, out var time)) return time;
        throw new FormatException($"Pole {fieldName}: wpisz np. Dzień 12, 14:30 albo pozostaw puste.");
    }

    private static string MaskCard(string? cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.StartsWith("BRAK", StringComparison.OrdinalIgnoreCase))
            return "BRAK KARTY";
        return cardNumber.Length <= 4 ? "****" : $"****{cardNumber[^4..]}";
    }
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; OnPropertyChanged(name); return true; }
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    public event PropertyChangedEventHandler? PropertyChanged;
    public void Dispose() { SaveDeviceState(); _clockTimer.Stop(); _cancellation.Cancel(); _cancellation.Dispose(); }

    private sealed class RelayCommand(Action execute) : ICommand
    {
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute();
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }

    private sealed class RelayCommand<T>(Action<T> execute, Func<T, bool>? canExecute = null) : ICommand
        where T : class
    {
        public bool CanExecute(object? parameter) =>
            parameter is T value && (canExecute?.Invoke(value) ?? true);
        public void Execute(object? parameter)
        {
            if (parameter is T value && CanExecute(value)) execute(value);
        }
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}

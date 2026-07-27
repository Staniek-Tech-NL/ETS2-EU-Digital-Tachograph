using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
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
using ETS2Tachograph.Desktop.Localization;
using ETS2Tachograph.Engine;
using ETS2Tachograph.RuleEngine;
using Microsoft.Win32;

namespace ETS2Tachograph.Desktop;

public sealed record RestTargetOption(string Name, string DeviceLabel, int Minutes);
public sealed record UiCultureOption(string CultureName, string DisplayName);
public sealed record HistoryActivityRow(
    Guid Id,
    string DriverCardId,
    DriverActivity Activity,
    GameTime Start,
    GameTime EndExclusive,
    ActivitySource Source,
    SpecialCondition Condition)
{
    public string StartGameTimeText => GameClockFormatter.Format(Start);
    public string EndGameTimeText => GameClockFormatter.Format(EndExclusive);
    public string ActivityText => ActivityDescription(Activity);
    public string SourceText => ActivitySourceDescription(Source);
    public string ConditionText => SpecialConditionDescription(Condition);

    public static HistoryActivityRow From(ActivityRecord record) => new(
        record.Id,
        record.DriverCardId,
        record.Activity,
        record.Start,
        record.EndExclusive,
        record.Source,
        record.Condition);

    private static string ActivityDescription(DriverActivity activity) => activity switch
    {
        DriverActivity.BreakOrRest => Localization.UiStrings.Get("Activity_BreakOrRest"),
        DriverActivity.Availability => Localization.UiStrings.Get("Activity_Availability"),
        DriverActivity.OtherWork => Localization.UiStrings.Get("Activity_OtherWork"),
        DriverActivity.Driving => Localization.UiStrings.Get("Activity_Driving"),
        DriverActivity.OutOfScope => "OUT",
        DriverActivity.Unknown => Localization.UiStrings.Get("Activity_Unknown"),
        _ => throw new ArgumentOutOfRangeException(nameof(activity))
    };

    internal static string GapReasonDescription(ActivityGapReason reason) => reason switch
    {
        ActivityGapReason.CardRemoved => Localization.UiStrings.Get("GapReason_CardRemoved"),
        ActivityGapReason.ForwardTimeJump => Localization.UiStrings.Get("GapReason_ForwardTimeJump"),
        ActivityGapReason.TelemetryUnavailable =>
            Localization.UiStrings.Get("GapReason_TelemetryUnavailable"),
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null)
    };

    internal static string ViolationDescription(ViolationType type) => type switch
    {
        ViolationType.ContinuousDrivingExceeded =>
            Localization.UiStrings.Get("Violation_ContinuousDrivingExceeded"),
        ViolationType.MissingRequiredBreak =>
            Localization.UiStrings.Get("Violation_MissingRequiredBreak"),
        ViolationType.DailyDrivingExceeded =>
            Localization.UiStrings.Get("Violation_DailyDrivingExceeded"),
        ViolationType.WeeklyDrivingExceeded =>
            Localization.UiStrings.Get("Violation_WeeklyDrivingExceeded"),
        ViolationType.FortnightlyDrivingExceeded =>
            Localization.UiStrings.Get("Violation_FortnightlyDrivingExceeded"),
        ViolationType.TooManyDailyExtensions =>
            Localization.UiStrings.Get("Violation_TooManyDailyExtensions"),
        ViolationType.DailyRestMissing =>
            Localization.UiStrings.Get("Violation_DailyRestMissing"),
        ViolationType.TooManyReducedDailyRests =>
            Localization.UiStrings.Get("Violation_TooManyReducedDailyRests"),
        ViolationType.WeeklyRestMissing =>
            Localization.UiStrings.Get("Violation_WeeklyRestMissing"),
        ViolationType.WeeklyRestPatternInvalid =>
            Localization.UiStrings.Get("Violation_WeeklyRestPatternInvalid"),
        ViolationType.WeeklyRestCompensationOverdue =>
            Localization.UiStrings.Get("Violation_WeeklyRestCompensationOverdue"),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    private static string ActivitySourceDescription(ActivitySource source) => source switch
    {
        ActivitySource.Telemetry => Localization.UiStrings.Get("ActivitySource_Telemetry"),
        ActivitySource.Manual => Localization.UiStrings.Get("ActivitySource_Manual"),
        ActivitySource.Reconstructed =>
            Localization.UiStrings.Get("ActivitySource_Reconstructed"),
        ActivitySource.Mixed => Localization.UiStrings.Get("ActivitySource_Mixed"),
        ActivitySource.ManualEntry =>
            Localization.UiStrings.Get("ActivitySource_ManualEntry"),
        ActivitySource.AutomaticCrewReconstruction =>
            Localization.UiStrings.Get("ActivitySource_AutomaticCrewReconstruction"),
        _ => throw new ArgumentOutOfRangeException(nameof(source))
    };

    private static string SpecialConditionDescription(SpecialCondition condition) =>
        condition switch
        {
            SpecialCondition.None => Localization.UiStrings.Get("SpecialCondition_None"),
            SpecialCondition.FerryCrossing =>
                Localization.UiStrings.Get("SpecialCondition_FerryCrossing"),
            SpecialCondition.Mixed => Localization.UiStrings.Get("SpecialCondition_Mixed"),
            SpecialCondition.CrewBreakInMotion =>
                Localization.UiStrings.Get("SpecialCondition_CrewBreakInMotion"),
            _ => throw new ArgumentOutOfRangeException(nameof(condition))
        };
}

public sealed record ActivityGapRow(
    Guid GapId,
    string DriverCardId,
    int Slot,
    ActivityGapReason Reason,
    ActivityGapState State,
    long StartGameMinute,
    long? EndGameMinute,
    long DurationMinutes,
    long? ResolvedAtGameMinute)
{
    public bool IsOpen => EndGameMinute is null;
    public bool IsResolvable => State == ActivityGapState.Unresolved && !IsOpen;
    public string SlotText => $"S{Slot}";
    public string StartGameTimeText =>
        GameClockFormatter.Format(new GameTime(StartGameMinute));
    public string EndGameTimeText => EndGameMinute is { } end
        ? GameClockFormatter.Format(new GameTime(end))
        : Localization.UiStrings.Get("GapState_Ongoing");
    public string DurationText => $"{DurationMinutes / 60:00}:{DurationMinutes % 60:00}";
    public string ReasonText => Reason switch
    {
        ActivityGapReason.ForwardTimeJump =>
            Localization.UiStrings.Get("GapReason_ForwardTimeJump"),
        ActivityGapReason.CardRemoved => Localization.UiStrings.Get("GapReason_CardRemoved"),
        ActivityGapReason.TelemetryUnavailable =>
            Localization.UiStrings.Get("GapReason_TelemetryUnavailable"),
        _ => throw new ArgumentOutOfRangeException(nameof(Reason))
    };
    public string StateText => State == ActivityGapState.Resolved
        ? Localization.UiStrings.Format(
            "GapState_ResolvedFormat",
            ResolvedAtGameMinute is { } resolvedAt
                ? GameClockFormatter.Format(new GameTime(resolvedAt))
                : "—")
        : IsOpen
            ? Localization.UiStrings.Get("GapState_Ongoing")
            : Localization.UiStrings.Get("GapState_Unresolved");
    public string OngoingHelpText => IsOpen && Reason == ActivityGapReason.CardRemoved
        ? Localization.UiStrings.Get("Gap_CardStillRemovedHelp")
        : string.Empty;
    public string ActionText => IsResolvable
        ? Localization.UiStrings.Get("Gap_ResolveAction")
        : "—";

    public static ActivityGapRow From(ActivityGapListItemDto item) => new(
        item.GapId,
        item.DriverCardId,
        item.Slot,
        item.Reason,
        item.State,
        item.StartGameMinute,
        item.EndGameMinute,
        item.DurationMinutes,
        item.ResolvedAtGameMinute);
}

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private const int PlannerTabIndex = 3;
    private static readonly ManualEntryActivityOption[] AvailableManualEntryActivities =
    [
        new(DriverActivity.BreakOrRest,
            Localization.UiStrings.Get("ManualEntryActivity_BreakOrRest")),
        new(DriverActivity.OtherWork, Localization.UiStrings.Get("Activity_OtherWork")),
        new(DriverActivity.Availability, Localization.UiStrings.Get("Activity_Availability"))
    ];

    private static readonly RestTargetOption[] AvailableRestTargets =
    [
        new(Localization.UiStrings.Get("RestTarget_Break15Part1"), Localization.UiStrings.Get("DeviceRestTarget_Break15Part1"), 15),
        new(Localization.UiStrings.Get("RestTarget_Break30Part2"), Localization.UiStrings.Get("DeviceRestTarget_Break30Part2"), 30),
        new(Localization.UiStrings.Get("RestTarget_Break45Full"), Localization.UiStrings.Get("DeviceRestTarget_Break45Full"), 45),
        new(Localization.UiStrings.Get("RestTarget_Daily9Hours"), Localization.UiStrings.Get("DeviceRestTarget_Daily9Hours"), 9 * 60),
        new(Localization.UiStrings.Get("RestTarget_Daily11Hours"), Localization.UiStrings.Get("DeviceRestTarget_Daily11Hours"), 11 * 60),
        new(Localization.UiStrings.Get("RestTarget_Weekly24Hours"), Localization.UiStrings.Get("DeviceRestTarget_Weekly24Hours"), 24 * 60),
        new(Localization.UiStrings.Get("RestTarget_Weekly45Hours"), Localization.UiStrings.Get("DeviceRestTarget_Weekly45Hours"), 45 * 60)
    ];

    public static string ApplicationVersion
    {
        get
        {
            var informationalVersion = typeof(MainViewModel).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;
            return string.IsNullOrWhiteSpace(informationalVersion)
                ? UiStrings.Get("Shell_VersionUnknown")
                : informationalVersion.Split('+', 2)[0];
        }
    }
    public static string ApplicationVersionText =>
        UiStrings.Format("Shell_VersionFormat", ApplicationVersion);

    private readonly CrewTachographService _crew;
    private readonly ManualEntryService _manualEntries;
    private readonly ActivityGapService _activityGaps;
    private readonly DriverService _drivers;
    private readonly ExportService _export;
    private readonly ImportService _import;
    private readonly SettingsService _settings;
    private readonly IUiCulturePreferenceStore _culturePreferences;
    private readonly ReportService _reports;
    private readonly RestAllocationService _restAllocations;
    private readonly IPdfReportExporter _pdfReports;
    private readonly DiagnosticLogService _diagnostics;
    private readonly string _defaultDriverCardId;
    private readonly ITelemetrySource _telemetry;
    private readonly CancellationTokenSource _cancellation = new();
    private string _connectionStatus = UiStrings.Get("Shell_WaitingForEts2");
    private string _gameTimeText = "Czas gry: --";
    private string _activityText = UiStrings.Get("Common_NoData");
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
    private string _modesText = Localization.UiStrings.Get("OverlayModes_NormalSingle");
    private DriverProfileDto? _selectedProfile;
    private DriverProfileDto? _compensationDriverProfile;
    private string _newDriverName = string.Empty;
    private string _newCardNumber = string.Empty;
    private double _drivingThreshold;
    private int _weekOffset;
    private UiCultureOption _selectedUiCulture = null!;
    private readonly GameCalendarResolver _gameCalendar;
    private string _operationStatus = string.Empty;
    private int _selectedMainTabIndex;
    private bool _isCardInserted;
    private bool _isCardDialogVisible;
    private bool _cardDialogIsInsertion;
    private string _cardDialogTitle = string.Empty;
    private string _cardDialogMessage = string.Empty;
    private CountryOption? _selectedCountry;
    private string _cardOwner = "---";
    private string _cardNumber = Localization.UiStrings.Get("Card_NoCard");
    private readonly DispatcherTimer _clockTimer;
    private bool _isCard2Inserted;
    private string _card2Owner = "---";
    private string _card2Number = Localization.UiStrings.Get("Card_NoCard");
    private DriverActivity _driver2Activity = DriverActivity.Availability;
    private string _driver2ActivityText = Localization.UiStrings.Get("Card_NoCard");
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
    private string _driver2DailyRestDeadline = "—";
    private string _driver2WeeklyRestDeadline = "—/6 (—)";
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
    private string _startCountryIso = "PL";
    private string _endCountryIso = "PL";
    private string _fortnightlyDriving = "00:00";
    private string _dailyRestDeadline = "—";
    private string _weeklyRestDeadline = "—/6 (—)";
    private string _currentActivityDuration = "00:00";
    private DriverActivity? _lastDisplayedActivity;
    private long? _activityStartedAtMinute;
    private RestTargetOption _selectedRestTarget = AvailableRestTargets[2];
    private long? _restStartedAtGameMinute;
    private string _restElapsed = "00:00";
    private string _restRemaining = "00:45";
    private string _restStatus = Localization.UiStrings.Get("RestStatus_Waiting");
    private double _restProgressPercent;
    private RestTargetOption _selectedRestTarget2 = AvailableRestTargets[2];
    private long? _restStartedAtGameMinute2;
    private string _restElapsed2 = "00:00";
    private string _restRemaining2 = "00:45";
    private string _restStatus2 = Localization.UiStrings.Get("RestStatus_Waiting");
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
    private string _manualEntryDriverText = string.Empty;
    private string _manualEntryReasonText = string.Empty;
    private ManualEntryPlanEditor? _manualEntryEditor;
    private ManualEntrySegmentRow? _selectedManualEntrySegment;
    private ManualEntrySegmentRow? _editingManualEntrySegment;
    private ManualEntryActivityOption _selectedManualEntryActivity =
        AvailableManualEntryActivities[1];
    private ManualEntryDayOption? _manualEntryFromDay;
    private ManualEntryDayOption? _manualEntryToDay;
    private string _manualEntryFromTime = string.Empty;
    private string _manualEntryToTime = string.Empty;
    private bool _manualEntryIsDirty;
    private string _manualEntryValidationMessage = string.Empty;
    private string _manualEntrySelectionMessage = string.Empty;
    private string _manualEntryQualificationMessage = string.Empty;
    private bool _showResolvedGaps;
    private int _unresolvedGapCount;
    private Guid? _lastOptionalGapWarningId;
    private readonly Dictionary<string, RestCardPersistentState> _cardRestStates =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _lastCountriesByCard =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly CountryOption[] AvailableCountryOptions =
        CountryCatalog.Options.ToArray();
    private static readonly string[] CountryCodes =
        AvailableCountryOptions.Select(country => country.IsoAlpha2).ToArray();

    public MainViewModel(
        string driverCardId,
        CrewTachographService crew,
        ManualEntryService manualEntries,
        ActivityGapService activityGaps,
        DriverService drivers,
        ExportService export,
        ImportService import,
        ReportService reports,
        RestAllocationService restAllocations,
        IPdfReportExporter pdfReports,
        SettingsService settings,
        SettingsDto savedSettings,
        IUiCulturePreferenceStore culturePreferences,
        string activeCultureName,
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
        _culturePreferences = culturePreferences;
        _reports = reports;
        _restAllocations = restAllocations;
        _pdfReports = pdfReports;
        _defaultDriverCardId = driverCardId;
        _drivingThreshold = savedSettings.DrivingSpeedThresholdKph;
        _weekOffset = savedSettings.WeekEpochOffsetDays;
        UiCultureOptions.Add(new UiCultureOption(
            UiCulture.Polish,
            UiStrings.Get("Language_Polish")));
        UiCultureOptions.Add(new UiCultureOption(
            UiCulture.EnglishUnitedKingdom,
            UiStrings.Get("Language_EnglishUnitedKingdom")));
        _selectedUiCulture = UiCultureOptions.Single(option =>
            string.Equals(
                option.CultureName,
                UiCulture.Normalize(activeCultureName),
                StringComparison.Ordinal));
        _gameCalendar = new GameCalendarResolver(
            new GameCalendarContext(crew.Engine.WeekEpochOffsetDays));
        _telemetry = telemetry;
        _diagnostics = diagnostics;
        JourneyPlanner = new JourneyPlannerViewModel(
            new DeliveryPlannerService(crew),
            cardId => FindProfileByCard(cardId)?.DisplayName ?? cardId,
            JsonJourneyPlannerInputStateStore.CreateDefault(),
            (code, exception) => _diagnostics.Error(code, exception));
        ReportsWorkspace = new ReportsWorkspaceViewModel(
            reports,
            () => _crew.Current.Frame?.GameTime.TotalMinutes,
            crew.Engine.WeekEpochOffsetDays,
            ExportWorkspaceReportAsync,
            ShowReportGapsAsync,
            message => OperationStatus = message,
            (code, exception) => _diagnostics.Error(code, exception));
        foreach (var country in AvailableCountryOptions)
            CountryOptions.Add(country);
        OtherWorkCommand = new RelayCommand(() => SetActivity(DriverActivity.OtherWork));
        AvailabilityCommand = new RelayCommand(() => SetActivity(DriverActivity.Availability));
        RestCommand = new RelayCommand(StartSelectedRest);
        OutCommand = new RelayCommand(ToggleOutMode);
        FerryCommand = new RelayCommand(ToggleFerryMode);
        CrewCommand = new RelayCommand(() => OperationStatus = Localization.UiStrings.Get("Operation_CrewModeAutomatic"));
        CreateProfileCommand = new RelayCommand(async () => await CreateProfileAsync());
        ActivateProfileCommand = new RelayCommand(async () => await ActivateProfileAsync());
        SaveSettingsCommand = new RelayCommand(async () => await SaveSettingsAsync());
        ExportCommand = new RelayCommand(async () => await ExportAsync());
        ImportCommand = new RelayCommand(async () => await ImportAsync());
        RefreshReportCommand = ReportsWorkspace.RefreshCommand;
        RefreshCompensationDetailsCommand = new RelayCommand(
            async () => await RefreshCompensationDetailsAsync());
        SelectRestAllocationCommand = new RelayCommand<RestAllocationChoiceRow>(
            async row => await SelectRestAllocationAsync(row),
            row => row is not null);
        ShowReportGapsCommand = new RelayCommand(async () => await ShowReportGapsAsync());
        ExportCsvCommand = ReportsWorkspace.ExportCompensationCsvCommand;
        ExportPdfCommand = ReportsWorkspace.ExportPdfCommand;
        ExportVtcCommand = ReportsWorkspace.ExportVtcJsonCommand;
        ExportRawCsvCommand = ReportsWorkspace.ExportRawCsvCommand;
        ExportDiagnosticCommand = new RelayCommand(async () => await ExportDiagnosticReportAsync());
        InsertCardCommand = new RelayCommand(OpenCardInsertion);
        InsertCard2Command = new RelayCommand(() => OpenCardInsertion(2));
        EjectCardCommand = new RelayCommand(OpenCardEjection);
        EjectCard2Command = new RelayCommand(() => OpenCardEjection(2));
        ConfirmCardCommand = new RelayCommand(
            async () => await ConfirmCardOperationAsync(),
            () => SelectedCountry is not null);
        CancelCardCommand = new RelayCommand(() => IsCardDialogVisible = false);
        Driver1ActivityCommand = new RelayCommand(() => CycleDriverActivity(1));
        Driver2ActivityCommand = new RelayCommand(() => CycleDriverActivity(2));
        StartSelectedRestCommand = new RelayCommand(StartSelectedRest);
        StartSelectedRest2Command = new RelayCommand(StartSelectedRest2);
        DeviceOkCommand = new RelayCommand(async () => await DeviceOkAsync());
        DeviceUpCommand = new RelayCommand(() => MoveMenu(-1));
        DeviceDownCommand = new RelayCommand(() => MoveMenu(1));
        DeviceCancelCommand = new RelayCommand(DeviceCancel);
        ApplyManualEntrySegmentCommand = new RelayCommand(ApplyManualEntrySegment);
        EditManualEntrySegmentCommand = new RelayCommand<ManualEntrySegmentRow>(
            BeginManualEntrySegmentEdit);
        RemoveManualEntrySegmentCommand = new RelayCommand<ManualEntrySegmentRow>(
            RemoveManualEntrySegment,
            segment => segment.CanDelete);
        SetWholeManualEntryRestCommand = new RelayCommand(() =>
            SetWholeManualEntry(DriverActivity.BreakOrRest));
        SetWholeManualEntryWorkCommand = new RelayCommand(() =>
            SetWholeManualEntry(DriverActivity.OtherWork));
        SetWholeManualEntryAvailabilityCommand = new RelayCommand(() =>
            SetWholeManualEntry(DriverActivity.Availability));
        ResetManualEntryCommand = new RelayCommand(() =>
            SetWholeManualEntry(DriverActivity.BreakOrRest));
        ConfirmManualEntryCommand = new RelayCommand(
            async () => await ConfirmManualEntryAsync(),
            () => ManualEntryCanConfirm);
        CancelManualEntryCommand = new RelayCommand(CancelManualEntry);
        OpenOptionalManualEntryCommand = new RelayCommand(OpenOptionalManualEntry);
        ResolveGapFromHistoryCommand = new RelayCommand<ActivityGapRow>(
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

    public ObservableCollection<HistoryActivityRow> History { get; } = [];
    public ObservableCollection<DriverProfileDto> Profiles { get; } = [];
    public ObservableCollection<DriverProfileDto> AvailableCardProfiles { get; } = [];
    public ObservableCollection<string> Violations { get; } = [];
    public ObservableCollection<ManualEntrySegmentRow> ManualEntrySegments { get; } = [];
    public ObservableCollection<ManualEntryDayOption> ManualEntryDayOptions { get; } = [];
    public ObservableCollection<ActivityGapRow> ActivityGaps { get; } = [];
    public ObservableCollection<CompensationDetailRow> CompensationDetails { get; } = [];
    public ObservableCollection<RestAllocationChoiceRow> PendingRestAllocationChoices { get; } = [];
    public ObservableCollection<CountryOption> CountryOptions { get; } = [];
    public ObservableCollection<UiCultureOption> UiCultureOptions { get; } = [];
    public JourneyPlannerViewModel JourneyPlanner { get; }
    public ReportsWorkspaceViewModel ReportsWorkspace { get; }
    public bool HasPendingRestAllocations => PendingRestAllocationChoices.Count > 0;
    public string CompensationDetailsHeader => CompensationDetails.Count == 0
        ? Localization.UiStrings.Get("Compensation_NoObligations")
        : Localization.UiStrings.Format(
            "Compensation_DetailsHeaderFormat",
            CompensationDetails.Count,
            CompensationDetails.Count(item => item.IsOpen));
    public string Driver1ButtonTooltip =>
        Localization.UiStrings.Format("Device_DriverButtonTooltipFormat", 1);
    public string Driver2ButtonTooltip =>
        Localization.UiStrings.Format("Device_DriverButtonTooltipFormat", 2);
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
    public ICommand SelectRestAllocationCommand { get; }
    public ICommand ShowReportGapsCommand { get; }
    public ICommand ExportCsvCommand { get; }
    public ICommand ExportPdfCommand { get; }
    public ICommand ExportVtcCommand { get; }
    public ICommand ExportRawCsvCommand { get; }
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
    public ICommand ApplyManualEntrySegmentCommand { get; }
    public ICommand EditManualEntrySegmentCommand { get; }
    public ICommand RemoveManualEntrySegmentCommand { get; }
    public ICommand SetWholeManualEntryRestCommand { get; }
    public ICommand SetWholeManualEntryWorkCommand { get; }
    public ICommand SetWholeManualEntryAvailabilityCommand { get; }
    public ICommand ResetManualEntryCommand { get; }
    public ICommand ConfirmManualEntryCommand { get; }
    public ICommand CancelManualEntryCommand { get; }
    public ICommand OpenOptionalManualEntryCommand { get; }
    public ICommand ResolveGapFromHistoryCommand { get; }
    public ICommand CopyIdentifierCommand { get; }
    public DriverProfileDto? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (!Set(ref _selectedProfile, value)) return;
            if (IsCardDialogVisible && _cardDialogIsInsertion)
            {
                RefreshCountryOptions(value?.Cards.FirstOrDefault()?.CardNumber);
                RestoreCountrySelection(value?.Cards.FirstOrDefault()?.CardNumber);
            }
        }
    }
    public DriverProfileDto? CompensationDriverProfile
    {
        get => _compensationDriverProfile;
        set
        {
            if (!Set(ref _compensationDriverProfile, value)) return;
            CompensationDetails.Clear();
            PendingRestAllocationChoices.Clear();
            OnPropertyChanged(nameof(CompensationDetailsHeader));
            OnPropertyChanged(nameof(HasPendingRestAllocations));
        }
    }
    public string NewDriverName { get => _newDriverName; set => Set(ref _newDriverName, value); }
    public string NewCardNumber { get => _newCardNumber; set => Set(ref _newCardNumber, value); }
    public double DrivingThreshold { get => _drivingThreshold; set => Set(ref _drivingThreshold, value); }
    public int WeekOffset { get => _weekOffset; set => Set(ref _weekOffset, value); }
    public UiCultureOption SelectedUiCulture
    {
        get => _selectedUiCulture;
        set => Set(ref _selectedUiCulture, value);
    }
    public string OperationStatus
    {
        get => _operationStatus;
        private set
        {
            if (Set(ref _operationStatus, value) && !string.IsNullOrWhiteSpace(value))
                _diagnostics.Info("STATUS", value);
        }
    }
    public int SelectedMainTabIndex
    {
        get => _selectedMainTabIndex;
        set
        {
            if (!Set(ref _selectedMainTabIndex, value))
                return;
            if (value == PlannerTabIndex)
                _ = JourneyPlanner.RefreshReadinessAsync();
        }
    }
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
    private string StartCountryIso { get => _startCountryIso; set => _startCountryIso = value; }
    private string EndCountryIso { get => _endCountryIso; set => _endCountryIso = value; }
    public string ModesText { get => _modesText; private set => Set(ref _modesText, value); }
    public bool IsCardInserted { get => _isCardInserted; private set { if (Set(ref _isCardInserted, value)) OnPropertyChanged(nameof(CardStatus)); } }
    public bool IsCardDialogVisible { get => _isCardDialogVisible; private set => Set(ref _isCardDialogVisible, value); }
    public string CardDialogTitle { get => _cardDialogTitle; private set => Set(ref _cardDialogTitle, value); }
    public string CardDialogMessage { get => _cardDialogMessage; private set => Set(ref _cardDialogMessage, value); }
    public string CardCountryLabel => _cardDialogIsInsertion ? Localization.UiStrings.Get("CardDialog_StartCountryLabel") : Localization.UiStrings.Get("CardDialog_EndCountryLabel");
    public CountryOption? SelectedCountry
    {
        get => _selectedCountry;
        set
        {
            if (!Set(ref _selectedCountry, value)) return;
            CommandManager.InvalidateRequerySuggested();
        }
    }
    public string CardOwner { get => _cardOwner; private set => Set(ref _cardOwner, value); }
    public string CardNumber { get => _cardNumber; private set => Set(ref _cardNumber, value); }
    public string CardStatus => IsCardInserted ? "KARTA 1 GOTOWA" : "BRAK KARTY 1";
    public bool IsCard2Inserted { get => _isCard2Inserted; private set { if (Set(ref _isCard2Inserted, value)) OnPropertyChanged(nameof(Card2Status)); } }
    public string Card2Owner { get => _card2Owner; private set => Set(ref _card2Owner, value); }
    public string Card2Number { get => _card2Number; private set => Set(ref _card2Number, value); }
    public string Card2Status => IsCard2Inserted ? "KARTA 2 GOTOWA" : "BRAK KARTY 2";
    public string CardDialogSlotText =>
        $"{_cardDialogSlot}  {Localization.UiStrings.Get("CardDialog_DriverCardLabel")}";
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
    public string ManualEntryTitle => Localization.UiStrings.Get("ManualEntry_Title");
    public string ManualEntrySlotText => $"S{_manualEntrySlot}";
    public string ManualEntryRangeText { get => _manualEntryRangeText; private set => Set(ref _manualEntryRangeText, value); }
    public string ManualEntryDurationText { get => _manualEntryDurationText; private set => Set(ref _manualEntryDurationText, value); }
    public string ManualEntryDriverText { get => _manualEntryDriverText; private set => Set(ref _manualEntryDriverText, value); }
    public string ManualEntryReasonText { get => _manualEntryReasonText; private set => Set(ref _manualEntryReasonText, value); }
    public IReadOnlyList<ManualEntryActivityOption> ManualEntryActivities =>
        AvailableManualEntryActivities;
    public ManualEntryActivityOption SelectedManualEntryActivity
    {
        get => _selectedManualEntryActivity;
        set => Set(ref _selectedManualEntryActivity, value);
    }
    public ManualEntryDayOption? ManualEntryFromDay
    {
        get => _manualEntryFromDay;
        set { if (Set(ref _manualEntryFromDay, value)) OnPropertyChanged(nameof(ManualEntryFormDurationText)); }
    }
    public ManualEntryDayOption? ManualEntryToDay
    {
        get => _manualEntryToDay;
        set { if (Set(ref _manualEntryToDay, value)) OnPropertyChanged(nameof(ManualEntryFormDurationText)); }
    }
    public string ManualEntryFromTime
    {
        get => _manualEntryFromTime;
        set { if (Set(ref _manualEntryFromTime, value)) OnPropertyChanged(nameof(ManualEntryFormDurationText)); }
    }
    public string ManualEntryToTime
    {
        get => _manualEntryToTime;
        set { if (Set(ref _manualEntryToTime, value)) OnPropertyChanged(nameof(ManualEntryFormDurationText)); }
    }
    public ManualEntrySegmentRow? SelectedManualEntrySegment
    {
        get => _selectedManualEntrySegment;
        set => Set(ref _selectedManualEntrySegment, value);
    }
    public string ManualEntryEditorTitle => _editingManualEntrySegment is null
        ? Localization.UiStrings.Get("ManualEntry_AddOrReplaceTitle")
        : Localization.UiStrings.Get("ManualEntry_EditSegmentTitle");
    public string ManualEntryApplyButtonText => _editingManualEntrySegment is null
        ? Localization.UiStrings.Get("ManualEntry_AddOrReplaceAction")
        : Localization.UiStrings.Get("ManualEntry_SaveChangesAction");
    public string ManualEntryFormDurationText
    {
        get
        {
            if (!TryParseManualEntryDateTime(
                    ManualEntryFromDay,
                    ManualEntryFromTime,
                    out var from) ||
                !TryParseManualEntryDateTime(
                    ManualEntryToDay,
                    ManualEntryToTime,
                    out var to) ||
                to <= from)
            {
                return "—";
            }

            return ManualEntryPlanEditor.FormatDuration(to - from);
        }
    }
    public string ManualEntrySegmentCountText
    {
        get
        {
            var count = ManualEntrySegments.Count;
            return Localization.UiStrings.Format(
                Localization.UiPlural.Select(
                    count,
                    "ManualEntry_SegmentCountOneFormat",
                    "ManualEntry_SegmentCountFewFormat",
                    "ManualEntry_SegmentCountManyFormat"),
                count,
                ManualEntryPlanEditor.FormatDuration(
                    _manualEntryEditor?.GapDuration ?? 0));
        }
    }
    public string ManualEntryCoverageText =>
        $"{ManualEntryPlanEditor.FormatDuration(_manualEntryEditor?.CoveredMinutes ?? 0)} / " +
        ManualEntryPlanEditor.FormatDuration(_manualEntryEditor?.GapDuration ?? 0);
    public string ManualEntryRestTotal => ManualEntryPlanEditor.FormatDuration(_manualEntryEditor?.RestMinutes ?? 0);
    public string ManualEntryWorkTotal => ManualEntryPlanEditor.FormatDuration(_manualEntryEditor?.OtherWorkMinutes ?? 0);
    public string ManualEntryAvailabilityTotal => ManualEntryPlanEditor.FormatDuration(_manualEntryEditor?.AvailabilityMinutes ?? 0);
    public bool ManualEntryCanConfirm => _manualEntryEditor?.IsComplete == true;
    public string ManualEntryCompletionText => ManualEntryCanConfirm
        ? Localization.UiStrings.Get("ManualEntry_Complete")
        : Localization.UiStrings.Format(
            "ManualEntry_MissingDurationFormat",
            ManualEntryPlanEditor.FormatDuration(
                _manualEntryEditor?.UnassignedMinutes ?? 0));
    public string ManualEntryCoverageDetails =>
        Localization.UiStrings.Format(
            "ManualEntry_CoverageDetailsFormat",
            ManualEntryPlanEditor.FormatDuration(
                _manualEntryEditor?.UnassignedMinutes ?? 0),
            "00:00");
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
    public string ActivityGapsHeader =>
        Localization.UiStrings.Format("Gap_HeaderFormat", UnresolvedGapCount);

    public async Task StartAsync()
    {
        _diagnostics.Info("APP_READY", "Inicjalizacja profili, kart i historii.");
        foreach (var profile in await _drivers.GetProfilesAsync(_cancellation.Token)) Profiles.Add(profile);
        SelectedProfile = Profiles.FirstOrDefault(x => x.IsActive) ?? Profiles.FirstOrDefault();
        foreach (var card in Profiles.SelectMany(x => x.Cards))
            await _crew.RegisterCardAsync(card.CardNumber, _cancellation.Token);
        LoadDeviceState();
        CompensationDriverProfile =
            FindProfileByCard(CurrentDriverCardId) ?? SelectedProfile;
        await ReloadHistoryAsync(_cancellation.Token);
        await ReportsWorkspace.InitializeAsync(
            Profiles,
            CurrentDriverCardId,
            _cancellation.Token);
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
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                ConnectionStatus = UiStrings.Get("Shell_TelemetryError"));
        }
    }

    private void Refresh(CrewTachographSnapshot snapshot)
    {
        JourneyPlanner.ObserveStateChange();
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
        ConnectionStatus = frame is null
            ? UiStrings.Get("Shell_WaitingForEts2")
            : frame.GamePaused
                ? UiStrings.Get("Shell_Ets2Paused")
                : UiStrings.Get("Shell_TelemetryActive");
        GameTimeText = frame is null ? "Czas gry: --" : $"Czas gry: {GameClockFormatter.Format(frame.GameTime)}";

        var driver = snapshot.Driver;
        ActivityText = driver is null ? Localization.UiStrings.Get("Card_NoCard") : ActivityDescription(driver.ProvisionalActivity ?? driver.ManualActivity);
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
                WeeklyRestCompensationDtoMapper.MapAll(driverRules.CompensationObligations),
                _gameCalendar);
            DailyExtensionsExceeded = state.DailyExtensionsUsedThisWeek > 2;
            ReducedDailyRestsExceeded = state.ReducedDailyRestsSinceWeeklyRest > 3;
            WeeklyDriving = Format(state.WeeklyDrivingMinutes);
            FortnightlyDriving = Format(state.FortnightlyDrivingMinutes);
            DailyRestDeadline = FormatDeviceDeadline(
                state.DailyRestCompletionDeadlineGameMinute,
                GameDeadlineSemantic.CompleteBy);
            WeeklyRestDeadline = WeeklyRestWindowFormatter.FormatDevice(
                state.WeeklyRestWindowElapsedMinutes,
                state.WeeklyRestStartDeadlineGameMinute,
                _gameCalendar);
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
        Driver2ActivityText = coDriver is null ? Localization.UiStrings.Get("Card_NoCard") : ActivityDescription(coDriver.ProvisionalActivity ?? coDriver.ManualActivity);
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
                WeeklyRestCompensationDtoMapper.MapAll(coRules.CompensationObligations),
                _gameCalendar);
            Driver2DailyExtensionsExceeded = state.DailyExtensionsUsedThisWeek > 2;
            Driver2ReducedDailyRestsExceeded = state.ReducedDailyRestsSinceWeeklyRest > 3;
            Driver2WeeklyDriving = Format(state.WeeklyDrivingMinutes);
            Driver2FortnightlyDriving = Format(state.FortnightlyDrivingMinutes);
            Driver2DailyRestDeadline = FormatDeviceDeadline(
                state.DailyRestCompletionDeadlineGameMinute,
                GameDeadlineSemantic.CompleteBy);
            Driver2WeeklyRestDeadline = WeeklyRestWindowFormatter.FormatDevice(
                state.WeeklyRestWindowElapsedMinutes,
                state.WeeklyRestStartDeadlineGameMinute,
                _gameCalendar);
        }
        else ResetDriver2Counters();
        UpdateRestCounters2(snapshot);

        Violations.Clear();
        if (driver?.Regulation is not null)
            foreach (var violation in driver.Regulation.Violations)
                Violations.Add(Localization.UiStrings.Format(
                    "Dashboard_ViolationAlertFormat",
                    1,
                    violation.Article,
                    HistoryActivityRow.ViolationDescription(violation.Type)));
        if (coDriver?.Regulation is not null)
            foreach (var violation in coDriver.Regulation.Violations)
                Violations.Add(Localization.UiStrings.Format(
                    "Dashboard_ViolationAlertFormat",
                    2,
                    violation.Article,
                    HistoryActivityRow.ViolationDescription(violation.Type)));
        if (snapshot.ManualEntryRequired)
            Violations.Insert(0, Localization.UiStrings.Get("Dashboard_ManualEntryRequiredAlert"));

        foreach (var record in (driver?.CompletedRecords ?? []).Concat(coDriver?.CompletedRecords ?? [])
                     .Where(x => History.All(old => old.Id != x.Id)))
            History.Add(HistoryActivityRow.From(record));
        ModesText = Localization.UiStrings.Get((driver?.OutModeEnabled, driver?.FerryModeEnabled, snapshot.MultiManning) switch
        {
            (true, _, true) => "OverlayModes_OutMulti",
            (true, _, false) => "OverlayModes_OutSingle",
            (_, true, true) => "OverlayModes_FerryMulti",
            (_, true, false) => "OverlayModes_FerrySingle",
            (_, _, true) => "OverlayModes_NormalMulti",
            _ => "OverlayModes_NormalSingle"
        });
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
        DailyRestDeadline = "—"; WeeklyRestDeadline = "—/6 (—)";
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
        Driver2DailyRestDeadline = "—"; Driver2WeeklyRestDeadline = "—/6 (—)";
    }

    private void SetActivity(DriverActivity activity)
        => SetActivity(TachographSlot.Driver, activity);

    private void SetActivity(TachographSlot slot, DriverActivity activity)
    {
        if ((slot == TachographSlot.Driver && !IsCardInserted) ||
            (slot == TachographSlot.CoDriver && !IsCard2Inserted))
        {
            OperationStatus = Localization.UiStrings.Format(
                "Operation_InsertCardRequiredFormat",
                (int)slot);
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
            OperationStatus = Localization.UiStrings.Get("Operation_ManualActivityRejected");
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
            OperationStatus = Localization.UiStrings.Format(
                "Operation_RestStartedFormat",
                SelectedRestTarget.Name);
            SaveDeviceState();
        }
    }

    private void StartSelectedRest2()
    {
        if (!IsCard2Inserted)
        {
            OperationStatus = Localization.UiStrings.Format(
                "Operation_InsertCardRequiredFormat",
                2);
            return;
        }
        try
        {
            if (_crew.Engine.VehicleMoving)
            {
                if (_crew.Current.CoDriverMovingBreakActive)
                {
                    OperationStatus = Localization.UiStrings.Get("Operation_CoDriverMovingBreakAlreadyActive");
                    return;
                }
                _selectedRestTarget2 = AvailableRestTargets.First(x => x.Minutes == CrewTachographEngine.MovingBreakMinutes);
                OnPropertyChanged(nameof(SelectedRestTarget2));
                OnPropertyChanged(nameof(RestTargetText2));
                _crew.Engine.StartCoDriverMovingBreak();
                _diagnostics.Info("CODRIVER_MOVING_BREAK", "Slot 2: rozpoczęto przerwę 45 minut podczas jazdy.");
                OperationStatus = Localization.UiStrings.Get("Operation_CoDriverMovingBreakStarted");
            }
            else
            {
                var wasResting = _crew.Current.CoDriver is { } current &&
                                 (current.ProvisionalActivity ?? current.ManualActivity) == DriverActivity.BreakOrRest;
                var startedAt = _crew.Current.Frame?.GameTime.TotalMinutes;
                SetActivity(TachographSlot.CoDriver, DriverActivity.BreakOrRest);
                if (!wasResting && startedAt is not null)
                    _restStartedAtGameMinute2 = startedAt;
                OperationStatus = Localization.UiStrings.Format(
                    "Operation_CoDriverRestStartedFormat",
                    SelectedRestTarget2.Name);
            }
            Refresh(_crew.Current);
            SaveDeviceState();
        }
        catch (InvalidOperationException exception)
        {
            _diagnostics.Error("REST_REJECTED", exception);
            OperationStatus = Localization.UiStrings.Get("Operation_RestRejected");
        }
    }

    private void UpdateRestCounters(TachographSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            _restStartedAtGameMinute = null;
            RestElapsed = "00:00"; RestRemaining = Format(SelectedRestTarget.Minutes);
            RestProgressPercent = 0; RestStatus = Localization.UiStrings.Get("RestStatus_Waiting");
            return;
        }
        var displayedActivity = snapshot.ProvisionalActivity ?? snapshot.ManualActivity;
        var isResting = IsCardInserted && displayedActivity == DriverActivity.BreakOrRest;

        if (!isResting)
        {
            _restStartedAtGameMinute = null;
            RestElapsed = "00:00";
            RestRemaining = Format(SelectedRestTarget.Minutes);
            RestProgressPercent = 0;
            RestStatus = Localization.UiStrings.Get("RestStatus_Waiting");
            return;
        }

        var projection = ProjectRestCounter(snapshot, SelectedRestTarget.Minutes);
        RestElapsed = projection.Elapsed;
        RestRemaining = projection.Remaining;
        RestProgressPercent = projection.ProgressPercent;
        RestStatus = projection.Status;
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
            RestStatus2 = Localization.UiStrings.Get("RestStatus_InProgressWhileMoving");
            return;
        }

        if (crewSnapshot.CoDriverMovingBreakCompleted)
        {
            RestElapsed2 = Format(CrewTachographEngine.MovingBreakMinutes);
            RestRemaining2 = "00:00";
            RestProgressPercent2 = 100;
            RestStatus2 = Localization.UiStrings.Get("RestStatus_CompletedWhileMoving");
            return;
        }

        var snapshot = crewSnapshot.CoDriver;
        if (snapshot is null)
        {
            _restStartedAtGameMinute2 = null;
            RestElapsed2 = "00:00"; RestRemaining2 = Format(SelectedRestTarget2.Minutes);
            RestProgressPercent2 = 0; RestStatus2 = Localization.UiStrings.Get("RestStatus_Waiting");
            return;
        }

        var activity = snapshot.ProvisionalActivity ?? snapshot.ManualActivity;
        var isResting = IsCard2Inserted && activity == DriverActivity.BreakOrRest;
        if (!isResting)
        {
            _restStartedAtGameMinute2 = null;
            RestElapsed2 = "00:00"; RestRemaining2 = Format(SelectedRestTarget2.Minutes);
            RestProgressPercent2 = 0; RestStatus2 = Localization.UiStrings.Get("RestStatus_Waiting");
            return;
        }
        var projection = ProjectRestCounter(snapshot, SelectedRestTarget2.Minutes);
        RestElapsed2 = projection.Elapsed;
        RestRemaining2 = projection.Remaining;
        RestProgressPercent2 = projection.ProgressPercent;
        RestStatus2 = projection.Status;
    }

    internal static (
        string Elapsed,
        string Remaining,
        double ProgressPercent,
        string Status) ProjectRestCounter(
        TachographSnapshot snapshot,
        int targetMinutes)
    {
        var elapsed = snapshot.Regulation?.State.CurrentContinuousBreakMinutes ?? 0;
        var remaining = Math.Max(0, targetMinutes - elapsed);
        return (
            Format(elapsed),
            Format(remaining),
            Math.Min(100, elapsed * 100d / targetMinutes),
            elapsed >= targetMinutes ? Localization.UiStrings.Get("RestStatus_Completed") : Localization.UiStrings.Get("RestStatus_InProgress"));
    }

    private void ToggleOutMode()
    {
        var driver = _crew.Current.Driver;
        if (driver is null)
        {
            OperationStatus = Localization.UiStrings.Format(
                "Operation_InsertCardRequiredFormat",
                1);
            return;
        }
        _crew.Engine.SetOutMode(TachographSlot.Driver, !driver.OutModeEnabled);
        _diagnostics.Info("OUT_MODE", $"Slot 1: {(!driver.OutModeEnabled ? "włączono" : "wyłączono")} OUT.");
        Refresh(_crew.Current); SaveDeviceState();
    }

    private void ToggleFerryMode()
    {
        var driver = _crew.Current.Driver;
        if (driver is null)
        {
            OperationStatus = Localization.UiStrings.Format(
                "Operation_InsertCardRequiredFormat",
                1);
            return;
        }
        _crew.Engine.SetFerryMode(TachographSlot.Driver, !driver.FerryModeEnabled);
        _diagnostics.Info("FERRY_MODE", $"Slot 1: {(!driver.FerryModeEnabled ? "włączono" : "wyłączono")} tryb promu.");
        Refresh(_crew.Current); SaveDeviceState();
    }

    private static string ActivityDescription(DriverActivity activity) => activity switch
    {
        DriverActivity.Driving => Localization.UiStrings.Get("Activity_Driving"),
        DriverActivity.OtherWork => Localization.UiStrings.Get("Activity_OtherWork"),
        DriverActivity.Availability => Localization.UiStrings.Get("Activity_Availability"),
        DriverActivity.BreakOrRest => Localization.UiStrings.Get("Activity_BreakOrRest"),
        DriverActivity.OutOfScope => "OUT",
        DriverActivity.Unknown => Localization.UiStrings.Get("Activity_Unknown"),
        _ => throw new ArgumentOutOfRangeException(nameof(activity))
    };

    private void OpenCardInsertion() => OpenCardInsertion(1);
    private void OpenCardInsertion(int slot)
    {
        if ((slot == 1 && IsCardInserted) || (slot == 2 && IsCard2Inserted))
        {
            OperationStatus = Localization.UiStrings.Format(
                "Operation_ReaderOccupiedFormat",
                slot);
            return;
        }
        if (_crew.Engine.VehicleMoving) { OperationStatus = Localization.UiStrings.Get("Operation_CardInsertionWhileMoving"); return; }
        _cardDialogSlot = slot;
        OnPropertyChanged(nameof(CardDialogSlotText));
        var occupiedCard = slot == 1 ? Card2Number : CardNumber;
        AvailableCardProfiles.Clear();
        foreach (var profile in Profiles.Where(x => x.Cards.All(card =>
                     !string.Equals(card.CardNumber, occupiedCard, StringComparison.OrdinalIgnoreCase))))
            AvailableCardProfiles.Add(profile);
        _cardDialogIsInsertion = true;
        SelectedProfile = AvailableCardProfiles.FirstOrDefault();
        if (SelectedProfile is null) { OperationStatus = Localization.UiStrings.Get("Operation_CreateProfileFirst"); return; }
        RefreshCountryOptions(SelectedProfile.Cards.FirstOrDefault()?.CardNumber);
        RestoreCountrySelection(SelectedProfile.Cards.FirstOrDefault()?.CardNumber);
        CardDialogTitle = Localization.UiStrings.Format(
            "CardDialog_InsertTitleFormat",
            slot);
        CardDialogMessage = Localization.UiStrings.Get("CardDialog_InsertMessage");
        OnPropertyChanged(nameof(CardCountryLabel));
        IsCardDialogVisible = true;
    }

    private void OpenCardEjection() => OpenCardEjection(1);
    private void OpenCardEjection(int slot)
    {
        if ((slot == 1 && !IsCardInserted) || (slot == 2 && !IsCard2Inserted))
        {
            OperationStatus = Localization.UiStrings.Format(
                "Operation_ReaderEmptyFormat",
                slot);
            return;
        }
        if (_currentSpeed > 0.5)
        {
            OperationStatus = Localization.UiStrings.Get("Operation_CardEjectionWhileMoving");
            return;
        }
        _cardDialogSlot = slot;
        OnPropertyChanged(nameof(CardDialogSlotText));
        _cardDialogIsInsertion = false;
        AvailableCardProfiles.Clear();
        var currentCard = slot == 1 ? CardNumber : Card2Number;
        if (FindProfileByCard(currentCard) is { } currentProfile)
        {
            AvailableCardProfiles.Add(currentProfile);
            SelectedProfile = currentProfile;
        }
        RefreshCountryOptions(currentCard);
        RestoreCountrySelection(currentCard);
        CardDialogTitle = Localization.UiStrings.Format(
            "CardDialog_EjectTitleFormat",
            slot);
        CardDialogMessage = Localization.UiStrings.Get("CardDialog_EjectMessage");
        OnPropertyChanged(nameof(CardCountryLabel));
        IsCardDialogVisible = true;
    }

    private async Task ConfirmCardOperationAsync()
    {
        if (SelectedCountry is not { } selectedCountry)
        {
            OperationStatus = Localization.UiStrings.Get("Operation_SelectCountry");
            return;
        }

        var countryCode = selectedCountry.IsoAlpha2;
        var tachographCode = selectedCountry.TachographCode;
        if (_cardDialogIsInsertion)
        {
            var card = SelectedProfile?.Cards.FirstOrDefault();
            if (SelectedProfile is null || card is null) { OperationStatus = Localization.UiStrings.Get("Operation_ProfileHasNoCard"); return; }
            var otherCard = _cardDialogSlot == 1 ? Card2Number : CardNumber;
            if (string.Equals(card.CardNumber, otherCard, StringComparison.OrdinalIgnoreCase))
            {
                OperationStatus = Localization.UiStrings.Get("Operation_CardAlreadyInOtherSlot");
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
                OperationStatus = Localization.UiStrings.Get("Operation_CardInsertionRejected");
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
            OperationStatus = Localization.UiStrings.Format(
                "Operation_CardInsertedFormat",
                countryCode,
                tachographCode);
            _diagnostics.Info("CARD_INSERTED", $"Slot {_cardDialogSlot}: {MaskCard(card.CardNumber)}, ISO {countryCode}, tachograf {tachographCode}.");
            StartCountryIso = countryCode;
            StartCountry = tachographCode;
            _lastCountriesByCard[card.CardNumber] = countryCode;
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
                    History.Add(HistoryActivityRow.From(record));
            }
            catch (InvalidOperationException exception)
            {
                _diagnostics.Error("CARD_EJECT_REJECTED", exception);
                OperationStatus = Localization.UiStrings.Get("Operation_CardEjectionRejected");
                return;
            }
            if (_cardDialogSlot == 1) { IsCardInserted = false; CardOwner = "---"; CardNumber = Localization.UiStrings.Get("Card_NoCard"); }
            else { IsCard2Inserted = false; Card2Owner = "---"; Card2Number = Localization.UiStrings.Get("Card_NoCard"); }
            OperationStatus = Localization.UiStrings.Format(
                "Operation_CardEjectedFormat",
                countryCode,
                tachographCode);
            _diagnostics.Info("CARD_EJECTED", $"Slot {_cardDialogSlot}: {MaskCard(ejectedCard)}, ISO {countryCode}, tachograf {tachographCode}.");
            EndCountryIso = countryCode;
            EndCountry = tachographCode;
            _lastCountriesByCard[ejectedCard] = countryCode;
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
                var message = Localization.UiStrings.Format(
                    "Operation_DrivingBlockedByManualEntryFormat",
                    required.Value.Slot);
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
            OperationStatus = Localization.UiStrings.Get("Operation_OptionalGapWithdrawn");
        }
        OnPropertyChanged(nameof(HasOptionalManualEntryGap));
        if (optional is not null && optional.Value.Gap.Id != _lastOptionalGapWarningId)
        {
            _lastOptionalGapWarningId = optional.Value.Gap.Id;
            _diagnostics.Warning(
                "OPTIONAL_MANUAL_ENTRY",
                $"Slot {optional.Value.Slot}: nierozliczona luka po skoku czasu {Format(optional.Value.Gap.DurationMinutes ?? 0)}.");
            OperationStatus = Localization.UiStrings.Format(
                "Operation_OptionalGapDetectedFormat",
                optional.Value.Slot);
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

    private void OpenManualEntryFromHistory(ActivityGapRow gap)
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
        ManualEntryDurationText = Localization.UiStrings.Format(
            "ManualEntry_GapDurationFormat",
            Format(gap.DurationMinutes ?? 0));
        ManualEntryDriverText =
            $"{FindProfileByCard(gap.DriverCardId)?.DisplayName ?? Localization.UiStrings.Get("ManualEntry_UnknownDriver")} · {gap.DriverCardId}";
        ManualEntryReasonText = Localization.UiStrings.Format(
            "ManualEntry_ReasonFormat",
            HistoryActivityRow.GapReasonDescription(gap.Reason));
        _manualEntryEditor = new ManualEntryPlanEditor(
            gap.Start.TotalMinutes,
            gap.EndExclusive.Value.TotalMinutes);
        ManualEntryDayOptions.Clear();
        var firstDay = (gap.Start.TotalMinutes / GameClockFormatter.MinutesPerDay) + 1;
        var lastDay = (gap.EndExclusive.Value.TotalMinutes / GameClockFormatter.MinutesPerDay) + 1;
        for (var day = firstDay; day <= lastDay; day++)
            ManualEntryDayOptions.Add(new ManualEntryDayOption(day));
        SelectedManualEntrySegment = null;
        _editingManualEntrySegment = null;
        _manualEntryIsDirty = false;
        ResetManualEntryForm();
        RefreshManualEntryPlan();
        ManualEntryValidationMessage = string.Empty;
        ManualEntryQualificationMessage = Localization.UiStrings.Get("ManualEntry_NotSaved");
        OnPropertyChanged(nameof(ManualEntryTitle));
        OnPropertyChanged(nameof(ManualEntrySlotText));
        OnPropertyChanged(nameof(HasOptionalManualEntryGap));
        IsManualEntryVisible = true;
        _deviceMenuOpen = false;
    }

    private void ApplyManualEntrySegment()
    {
        if (_manualEntryEditor is null) return;
        try
        {
            var from = ParseManualEntryDateTime(
                ManualEntryFromDay,
                ManualEntryFromTime,
                Localization.UiStrings.Get("Common_From"));
            var to = ParseManualEntryDateTime(
                ManualEntryToDay,
                ManualEntryToTime,
                Localization.UiStrings.Get("Common_To"));
            if (_editingManualEntrySegment is null)
            {
                _manualEntryEditor.Replace(
                    from,
                    to,
                    SelectedManualEntryActivity.Activity);
            }
            else
            {
                _manualEntryEditor.Edit(
                    _editingManualEntrySegment,
                    from,
                    to,
                    SelectedManualEntryActivity.Activity);
            }

            _manualEntryIsDirty = true;
            ManualEntryValidationMessage = string.Empty;
            _editingManualEntrySegment = null;
            ResetManualEntryForm();
            RefreshManualEntryPlan();
        }
        catch (Exception exception) when (exception is FormatException or InvalidOperationException)
        {
            ManualEntryValidationMessage = exception.Message;
        }
    }

    private void BeginManualEntrySegmentEdit(ManualEntrySegmentRow segment)
    {
        _editingManualEntrySegment = segment;
        SelectedManualEntrySegment = segment;
        SelectedManualEntryActivity = AvailableManualEntryActivities.Single(option =>
            option.Activity == segment.Activity);
        ManualEntryFromDay = FindManualEntryDay(segment.FromGameMinute);
        ManualEntryToDay = FindManualEntryDay(segment.ToGameMinuteExclusive);
        ManualEntryFromTime = GameClockFormatter.FormatTimeOfDay(
            new GameTime(segment.FromGameMinute));
        ManualEntryToTime = GameClockFormatter.FormatTimeOfDay(
            new GameTime(segment.ToGameMinuteExclusive));
        ManualEntryValidationMessage = string.Empty;
        OnPropertyChanged(nameof(ManualEntryEditorTitle));
        OnPropertyChanged(nameof(ManualEntryApplyButtonText));
    }

    private void RemoveManualEntrySegment(ManualEntrySegmentRow segment)
    {
        if (_manualEntryEditor is null) return;
        try
        {
            _manualEntryEditor.Remove(segment);
            _manualEntryIsDirty = true;
            _editingManualEntrySegment = null;
            SelectedManualEntrySegment = null;
            ManualEntryValidationMessage = string.Empty;
            ResetManualEntryForm();
            RefreshManualEntryPlan();
        }
        catch (InvalidOperationException exception)
        {
            ManualEntryValidationMessage = exception.Message;
        }
    }

    private void SetWholeManualEntry(DriverActivity activity)
    {
        if (_manualEntryEditor is null) return;
        _manualEntryEditor.Reset(activity);
        _manualEntryIsDirty = true;
        _editingManualEntrySegment = null;
        SelectedManualEntrySegment = null;
        ManualEntryValidationMessage = string.Empty;
        ResetManualEntryForm();
        RefreshManualEntryPlan();
    }

    private void ResetManualEntryForm()
    {
        if (_manualEntryEditor is null) return;
        SelectedManualEntryActivity = AvailableManualEntryActivities[1];
        ManualEntryFromDay = FindManualEntryDay(_manualEntryEditor.GapStart);
        ManualEntryToDay = FindManualEntryDay(_manualEntryEditor.GapEndExclusive);
        ManualEntryFromTime = GameClockFormatter.FormatTimeOfDay(
            new GameTime(_manualEntryEditor.GapStart));
        ManualEntryToTime = GameClockFormatter.FormatTimeOfDay(
            new GameTime(_manualEntryEditor.GapEndExclusive));
        OnPropertyChanged(nameof(ManualEntryEditorTitle));
        OnPropertyChanged(nameof(ManualEntryApplyButtonText));
    }

    private ManualEntryDayOption? FindManualEntryDay(long gameMinute)
    {
        var day = (gameMinute / GameClockFormatter.MinutesPerDay) + 1;
        return ManualEntryDayOptions.FirstOrDefault(option => option.DayNumber == day);
    }

    private void RefreshManualEntryPlan()
    {
        ManualEntrySegments.Clear();
        if (_manualEntryEditor is not null)
        {
            foreach (var segment in _manualEntryEditor.Segments)
                ManualEntrySegments.Add(segment);
        }

        ManualEntrySelectionMessage = Localization.UiStrings.Format(
            "ManualEntry_SelectionSummaryFormat",
            ManualEntryRestTotal,
            ManualEntryWorkTotal,
            ManualEntryAvailabilityTotal);
        OnPropertyChanged(nameof(ManualEntrySegmentCountText));
        OnPropertyChanged(nameof(ManualEntryCoverageText));
        OnPropertyChanged(nameof(ManualEntryRestTotal));
        OnPropertyChanged(nameof(ManualEntryWorkTotal));
        OnPropertyChanged(nameof(ManualEntryAvailabilityTotal));
        OnPropertyChanged(nameof(ManualEntryCanConfirm));
        OnPropertyChanged(nameof(ManualEntryCompletionText));
        OnPropertyChanged(nameof(ManualEntryCoverageDetails));
        CommandManager.InvalidateRequerySuggested();
    }

    private async Task ConfirmManualEntryAsync()
    {
        if (_manualEntryGap is null || _manualEntryGap.EndExclusive is null) return;
        try
        {
            if (_manualEntryEditor is null || !_manualEntryEditor.IsComplete)
                throw new InvalidOperationException(Localization.UiStrings.Get("ManualEntryError_IncompleteCoverage"));
            var segments = _manualEntryEditor.ToSegments();
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
                ? Localization.UiStrings.Get("ManualEntry_NoQualifiedRest")
                : QualificationText(qualified);
            _diagnostics.Info(
                "MANUAL_ENTRY_RESOLVED",
                $"Slot {_manualEntrySlot}: luka {_manualEntryGap.Id}, {ManualEntrySelectionMessage} {ManualEntryQualificationMessage}");
            OperationStatus = Localization.UiStrings.Format(
                "Operation_ManualEntrySavedFormat",
                ManualEntryQualificationMessage);
            IsManualEntryVisible = false;
            _manualEntryGap = null;
            _manualEntryEditor = null;
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
            _diagnostics.Error("MANUAL_ENTRY_REJECTED", exception);
            ManualEntryValidationMessage = exception is ManualEntryValidationException validation
                ? Localization.UiStrings.Get(ManualEntryErrorKey(validation.Error))
                : exception is FormatException or InvalidOperationException &&
                  exception is not ManualEntryDraftException
                    ? exception.Message
                    : Localization.UiStrings.Get("ManualEntryError_ApplyFailed");
        }
    }

    private void CancelManualEntry()
    {
        if (IsManualEntryForced)
        {
            ManualEntryValidationMessage = Localization.UiStrings.Get("ManualEntry_CardRemovalMustResolve");
            return;
        }
        if (_manualEntryIsDirty &&
            MessageBox.Show(
                Localization.UiStrings.Get("Dialog_ManualEntryDiscardMessage"),
            Localization.UiStrings.Get("ActivitySource_ManualEntry"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }
        IsManualEntryVisible = false;
        _manualEntryGap = null;
        _manualEntryEditor = null;
        OnPropertyChanged(nameof(HasOptionalManualEntryGap));
    }

    private static long ParseManualEntryDateTime(
        ManualEntryDayOption? day,
        string value,
        string label)
    {
        if (TryParseManualEntryDateTime(day, value, out var gameMinute))
            return gameMinute;
        if (day is null)
            throw new FormatException(Localization.UiStrings.Format(
                "ManualEntryError_SelectDayFormat",
                label));
        throw new FormatException(Localization.UiStrings.Format(
            "ManualEntryError_EnterTimeFormat",
            label));
    }

    private static bool TryParseManualEntryDateTime(
        ManualEntryDayOption? day,
        string value,
        out long gameMinute)
    {
        gameMinute = 0;
        var parts = value.Trim().Split(':');
        if (day is null ||
            parts.Length != 2 ||
            !int.TryParse(parts[0], out var hour) ||
            !int.TryParse(parts[1], out var minute) ||
            hour is < 0 or > 23 ||
            minute is < 0 or > 59)
        {
            return false;
        }

        gameMinute = checked(
            ((day.DayNumber - 1) * GameClockFormatter.MinutesPerDay) +
            (hour * 60) +
            minute);
        return true;
    }

    private static string QualificationText(QualifiedRestPeriod rest) =>
        Localization.UiStrings.Format(
            (rest.DailyClassification, rest.WeeklyClassification) switch
            {
                (DailyRestClassification.Regular, WeeklyRestClassification.Regular) =>
                    "ManualEntry_QualifiedDailyRegularWeeklyRegularFormat",
                (DailyRestClassification.Regular, WeeklyRestClassification.Reduced) =>
                    "ManualEntry_QualifiedDailyRegularWeeklyReducedFormat",
                (DailyRestClassification.Regular, null) =>
                    "ManualEntry_QualifiedDailyRegularFormat",
                (DailyRestClassification.Reduced, WeeklyRestClassification.Regular) =>
                    "ManualEntry_QualifiedDailyReducedWeeklyRegularFormat",
                (DailyRestClassification.Reduced, WeeklyRestClassification.Reduced) =>
                    "ManualEntry_QualifiedDailyReducedWeeklyReducedFormat",
                (DailyRestClassification.Reduced, null) =>
                    "ManualEntry_QualifiedDailyReducedFormat",
                _ => throw new ArgumentOutOfRangeException(nameof(rest))
            },
            GameClockFormatter.Format(rest.EndExclusive));

    private static string ManualEntryErrorKey(ManualEntryError error) => error switch
    {
        ManualEntryError.GapNotFound => "ManualEntryError_GapNotFound",
        ManualEntryError.GapNotCanonical => "ManualEntryError_GapNotCanonical",
        ManualEntryError.ProjectedGapCannotBeResolved =>
            "ManualEntryError_ProjectedGapCannotBeResolved",
        ManualEntryError.GapStillOpen => "ManualEntryError_GapStillOpen",
        ManualEntryError.InvalidActivity => "ManualEntryError_InvalidActivity",
        ManualEntryError.InvalidSegment => "ManualEntryError_InvalidSegment",
        ManualEntryError.IncompleteCoverage => "ManualEntryError_IncompleteCoverage",
        ManualEntryError.OutsideGap => "ManualEntryError_OutsideGap",
        ManualEntryError.OverlappingSegments => "ManualEntryError_OverlappingSegments",
        ManualEntryError.HistoryCollision => "ManualEntryError_HistoryCollision",
        ManualEntryError.ResolutionConflict => "ManualEntryError_ResolutionConflict",
        _ => throw new ArgumentOutOfRangeException(nameof(error), error, null)
    };

    private void CycleDriverActivity(int driver)
    {
        if (driver == 1)
        {
            if (_crew.Engine.VehicleMoving)
            {
                OperationStatus = Localization.UiStrings.Format(
                    "Operation_DriverActivityAutomaticFormat",
                    1);
                return;
            }
            var current = _crew.Current.Driver?.ManualActivity ?? DriverActivity.OtherWork;
            SetActivity(NextActivity(current));
        }
        else
        {
            if (!IsCard2Inserted)
            {
                OperationStatus = Localization.UiStrings.Format(
                    "Operation_InsertCardRequiredFormat",
                    2);
                return;
            }
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
            OperationStatus = Localization.UiStrings.Get("Operation_ConfirmRequiredManualEntry");
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
            _menuIndex = Math.Max(
                0,
                Array.IndexOf(
                    CountryCodes,
                    _menuIndex == 0 ? StartCountryIso : EndCountryIso));
        }
        else if (_deviceMenuPage is "country-start" or "country-end")
        {
            var country = AvailableCountryOptions[_menuIndex];
            if (_deviceMenuPage == "country-start")
            {
                StartCountryIso = country.IsoAlpha2;
                StartCountry = country.TachographCode;
            }
            else
            {
                EndCountryIso = country.IsoAlpha2;
                EndCountry = country.TachographCode;
            }
            OperationStatus = Localization.UiStrings.Format(
                "Operation_CountrySavedFormat",
                country.IsoAlpha2,
                country.TachographCode);
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
            OperationStatus = Localization.UiStrings.Get("Operation_SettingsInLeftPanel");
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
        "root" =>
        [
            Localization.UiStrings.Get("DeviceMenu_Print"),
            Localization.UiStrings.Get("DeviceMenu_ManualEntry"),
            Localization.UiStrings.Get("DeviceMenu_BreakOrRest"),
            Localization.UiStrings.Get("DeviceMenu_Countries"),
            Localization.UiStrings.Get("DeviceMenu_Modes"),
            Localization.UiStrings.Get("DeviceMenu_CardCounters"),
            Localization.UiStrings.Get("DeviceMenu_Settings")
        ],
        "print" => [Localization.UiStrings.Get("DeviceMenu_PrintDriver1Day"), Localization.UiStrings.Get("DeviceMenu_PrintVehicleDay")],
        "manual" =>
        [
            Localization.UiStrings.Get("ActivityUpper_OtherWork"),
            Localization.UiStrings.Get("ActivityUpper_Availability"),
            Localization.UiStrings.Get("ActivityUpper_Rest")
        ],
        "rest-target" => AvailableRestTargets.Select(x => x.DeviceLabel).ToArray(),
        "countries" =>
        [
            Localization.UiStrings.Format("DeviceMenu_StartCountryFormat", StartCountry),
            Localization.UiStrings.Format("DeviceMenu_EndCountryFormat", EndCountry)
        ],
        "country-start" or "country-end" => CountryCodes,
        "modes" =>
        [
            Localization.UiStrings.Format(
                "DeviceMenu_OutModeFormat",
                OnOff(_crew.Current.Driver?.OutModeEnabled == true)),
            Localization.UiStrings.Format(
                "DeviceMenu_FerryModeFormat",
                OnOff(_crew.Current.Driver?.FerryModeEnabled == true))
        ],
        "counter-cards" =>
        [
            Localization.UiStrings.Format(
                "DeviceMenu_CardStatusFormat",
                1,
                IsCardInserted
                    ? Localization.UiStrings.Get("DeviceState_Ready")
                    : Localization.UiStrings.Get("DeviceState_Missing")),
            Localization.UiStrings.Format(
                "DeviceMenu_CardStatusFormat",
                2,
                IsCard2Inserted
                    ? Localization.UiStrings.Get("DeviceState_Ready")
                    : Localization.UiStrings.Get("DeviceState_Missing"))
        ],
        "counters-1" => CounterItems(
            RestElapsed, RestRemaining, ContinuousDriving, UntilBreak,
            DailyDrivingWithLimit, DailyWorkWithLimit, WeeklyDriving,
            FortnightlyDriving, DailyRestDeadline, WeeklyRestDeadline,
            CompensationText, DailyExtensionsUsage, ReducedDailyRestsUsage),
        "counters-2" => CounterItems(
            RestElapsed2, RestRemaining2, Driver2ContinuousDriving, Driver2UntilBreak,
            Driver2DailyDrivingWithLimit, Driver2DailyWorkWithLimit, Driver2WeeklyDriving,
            Driver2FortnightlyDriving, Driver2DailyRestDeadline, Driver2WeeklyRestDeadline,
            Driver2CompensationText, Driver2DailyExtensionsUsage,
            Driver2ReducedDailyRestsUsage),
        _ => [Localization.UiStrings.Get("DeviceMenu_SpeedThreshold"), Localization.UiStrings.Get("DeviceMenu_RegulatoryWeek")]
    };

    private static string[] CounterItems(
        string restElapsed,
        string restRemaining,
        string continuousDriving,
        string untilBreak,
        string dailyDriving,
        string dailyDuty,
        string weeklyDriving,
        string fortnightlyDriving,
        string dailyRestDeadline,
        string weeklyRestDeadline,
        string compensation,
        string extensionsUsage,
        string reducedRestsUsage) =>
    [
        Localization.UiStrings.Format("DeviceCounter_BreakFormat", restElapsed),
        Localization.UiStrings.Format("DeviceCounter_TargetFormat", restRemaining),
        Localization.UiStrings.Format("DeviceCounter_ContinuousDrivingFormat", continuousDriving),
        Localization.UiStrings.Format("DeviceCounter_TimeToBreakFormat", untilBreak),
        Localization.UiStrings.Format("DeviceCounter_DailyDrivingFormat", dailyDriving),
        Localization.UiStrings.Format("DeviceCounter_DailyDutyFormat", dailyDuty),
        Localization.UiStrings.Format("DeviceCounter_WeeklyDrivingFormat", weeklyDriving),
        Localization.UiStrings.Format("DeviceCounter_FortnightlyDrivingFormat", fortnightlyDriving),
        Localization.UiStrings.Format("DeviceCounter_DailyRestDeadlineFormat", dailyRestDeadline),
        Localization.UiStrings.Format("DeviceCounter_WeeklyRestDeadlineFormat", weeklyRestDeadline),
        Localization.UiStrings.Format("DeviceCounter_CompensationFormat", compensation),
        Localization.UiStrings.Format("DeviceCounter_ExtensionsUsageFormat", extensionsUsage),
        Localization.UiStrings.Format("DeviceCounter_ReducedDailyRestsUsageFormat", reducedRestsUsage)
    ];

    private static string OnOff(bool enabled) => enabled ? Localization.UiStrings.Get("DeviceState_On") : Localization.UiStrings.Get("DeviceState_Off");

    private async Task PrintDailyReportAsync()
    {
        var report = await _reports.CreateAsync(CurrentDriverCardId);
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ETS2Tachograph", "Printouts");
        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, $"wydruk-24h-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
        await using var stream = File.Create(file);
        await _pdfReports.ExportAsync(report, stream);
        OperationStatus = Localization.UiStrings.Format(
            "Operation_PrintSavedFormat",
            file);
    }

    private void UpdateDeviceDisplay()
    {
        _warningBlink = !_warningBlink;
        DeviceLine2Foreground = "#111A12";
        if (_isCardLoading)
        {
            DeviceLine1 = Localization.UiStrings.Format(
                "Device_CardReadingFormat",
                _cardDialogSlot);
            DeviceLine2 = SelectedProfile?.DisplayName?.ToUpperInvariant() ??
                          Localization.UiStrings.Get("Device_DriverFallback");
            DeviceLine3 = "[##########]  OK"; return;
        }
        if (_crew.Current.ManualEntryRequired)
        {
            DeviceLine1 = Localization.UiStrings.Get("Device_ManualEntryRequired");
            DeviceLine2 = Localization.UiStrings.Format(
                "Device_RequiredSlotFormat",
                _manualEntrySlot);
            DeviceLine3 = _warningBlink ? Localization.UiStrings.Get("Device_DrivingBlocked") : Localization.UiStrings.Get("Device_ConfirmActivity");
            return;
        }
        if (IsPrinting)
        {
            DeviceLine1 = Localization.UiStrings.Get("Device_Printing"); DeviceLine2 = Localization.UiStrings.Get("DeviceMenu_PrintDriver1Day"); DeviceLine3 = "[########--]"; return;
        }
        if (_deviceMenuOpen)
        {
            var items = MenuItems();
            DeviceLine1 = _deviceMenuPage switch
            {
                "root" => Localization.UiStrings.Get("DeviceMenu_MainTitle"),
                "rest-target" => Localization.UiStrings.Get("DeviceMenu_SelectBreakTitle"),
                "counter-cards" => Localization.UiStrings.Get("DeviceMenu_SelectCardTitle"),
                "counters-1" => Localization.UiStrings.Format(
                    "DeviceMenu_CardCountersTitleFormat",
                    1),
                "counters-2" => Localization.UiStrings.Format(
                    "DeviceMenu_CardCountersTitleFormat",
                    2),
                "country-start" => Localization.UiStrings.Get("DeviceMenu_StartCountryTitle"),
                "country-end" => Localization.UiStrings.Get("DeviceMenu_EndCountryTitle"),
                "manual" => Localization.UiStrings.Get("DeviceMenu_ManualEntry"),
                "countries" => Localization.UiStrings.Get("DeviceMenu_Countries"),
                "modes" => Localization.UiStrings.Get("DeviceMenu_Modes"),
                "print" => Localization.UiStrings.Get("DeviceMenu_Print"),
                _ => throw new InvalidOperationException(
                    $"Unknown device menu page: {_deviceMenuPage}.")
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
        var activity1 = _currentSpeed > 0.5 ? Localization.UiStrings.Get("DeviceActivity_Driving") : driver is null ? Localization.UiStrings.Get("Card_NoCard") : ActivityLabel(driver.ManualActivity);
        var activity2 = coDriver is null ? Localization.UiStrings.Get("Card_NoCard") : ActivityLabel(coDriver.ProvisionalActivity ?? coDriver.ManualActivity);
        DeviceLine1 = $"{gameClock}       {_currentSpeed,3:0} km/h";
        DeviceLine2 = $"1 {activity1,-11} 2 {activity2}";
        DeviceLine3 = !IsCardInserted && _currentSpeed > 0.5
            ? (_warningBlink
                ? Localization.UiStrings.Get("Device_DrivingWithoutCard")
                : Localization.UiStrings.Format("Device_CardErrorFormat", 1))
            : IsCardInserted && _currentSpeed <= 0.5 && driver?.ManualActivity == DriverActivity.BreakOrRest
                ? $"P {RestElapsed}  > {RestRemaining}"
                : $"{_odometer:000000.0} km  " +
                  (IsCardInserted
                      ? "K1"
                      : Localization.UiStrings.Format("Device_NoCardShortFormat", 1));
    }

    private static string ActivityLabel(DriverActivity activity) => activity switch
    {
        DriverActivity.BreakOrRest => Localization.UiStrings.Get("DeviceActivity_BreakOrRest"),
        DriverActivity.OtherWork => Localization.UiStrings.Get("DeviceActivity_OtherWork"),
        DriverActivity.Availability => Localization.UiStrings.Get("DeviceActivity_Availability"),
        DriverActivity.Driving => Localization.UiStrings.Get("DeviceActivity_Driving"),
        DriverActivity.OutOfScope => "OUT",
        DriverActivity.Unknown => Localization.UiStrings.Get("DeviceActivity_Unknown"),
        _ => throw new ArgumentOutOfRangeException(nameof(activity))
    };

    private bool IsExceededCounterItem(string item) =>
        _deviceMenuPage is "counters-1" or "counters-2" &&
        (_menuIndex switch
        {
            10 => _deviceMenuPage == "counters-1"
                ? CompensationOverdue
                : Driver2CompensationOverdue,
            11 => _deviceMenuPage == "counters-1"
                ? DailyExtensionsExceeded
                : Driver2DailyExtensionsExceeded,
            12 => _deviceMenuPage == "counters-1"
                ? ReducedDailyRestsExceeded
                : Driver2ReducedDailyRestsExceeded,
            _ => false
        });

    private string CurrentDriverCardId => _crew.Current.DriverCardId ?? _defaultDriverCardId;
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
            StartCountryIso = state.StartCountryIso
                              ?? CountryCatalog.ResolveLegacyCode(state.StartCountry)?.IsoAlpha2
                              ?? state.StartCountry;
            EndCountryIso = state.EndCountryIso
                            ?? CountryCatalog.ResolveLegacyCode(state.EndCountry)?.IsoAlpha2
                            ?? state.EndCountry;
            if (state.LastCountriesByCard is not null)
                foreach (var pair in state.LastCountriesByCard)
                    if (FindCountry(pair.Value) is { } country)
                        _lastCountriesByCard[pair.Key] = country.IsoAlpha2;
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
            _diagnostics.Error("DEVICE_STATE_RESTORE_FAILED", exception);
            OperationStatus = Localization.UiStrings.Get("Operation_DeviceStateRestoreFailed");
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
                new Dictionary<string, RestCardPersistentState>(_cardRestStates, StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, string>(_lastCountriesByCard, StringComparer.OrdinalIgnoreCase),
                StartCountryIso,
                EndCountryIso);
            File.WriteAllText(DeviceStatePath, JsonSerializer.Serialize(state));
        }
        catch (Exception exception)
        {
            _diagnostics.Error("DEVICE_STATE_SAVE_FAILED", exception);
            OperationStatus = Localization.UiStrings.Get("Operation_DeviceStateSaveFailed");
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
        Dictionary<string, RestCardPersistentState>? CardRestStates = null,
        Dictionary<string, string>? LastCountriesByCard = null,
        string? StartCountryIso = null,
        string? EndCountryIso = null);

    private sealed record RestCardPersistentState(int TargetMinutes, long? StartedAtGameMinute);

    private DriverProfileDto? FindProfileByCard(string cardNumber) => Profiles.FirstOrDefault(profile =>
        profile.Cards.Any(card => string.Equals(card.CardNumber, cardNumber, StringComparison.OrdinalIgnoreCase)));

    private static CountryOption? FindCountry(string? countryCode) =>
        CountryCatalog.ResolveLegacyCode(countryCode);

    private void RefreshCountryOptions(string? currentCardNumber)
    {
        var recentCodes = new List<string>();
        if (!string.IsNullOrWhiteSpace(currentCardNumber) &&
            _lastCountriesByCard.TryGetValue(currentCardNumber, out var currentCountry))
        {
            recentCodes.Add(currentCountry);
        }

        recentCodes.AddRange(_lastCountriesByCard.Values
            .Where(code => !recentCodes.Contains(code, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase));

        CountryOptions.Clear();
        foreach (var code in recentCodes)
            if (CountryCatalog.FindByIso(code) is { } recent)
                CountryOptions.Add(recent);
        foreach (var country in AvailableCountryOptions)
            if (!recentCodes.Contains(country.IsoAlpha2, StringComparer.OrdinalIgnoreCase))
                CountryOptions.Add(country);
    }

    private void RestoreCountrySelection(string? cardNumber)
    {
        SelectedCountry = !string.IsNullOrWhiteSpace(cardNumber) &&
                          _lastCountriesByCard.TryGetValue(cardNumber, out var countryCode)
            ? FindCountry(countryCode)
            : null;
    }

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
            OperationStatus = Localization.UiStrings.Get("Operation_ProfileCreated");
        }
        catch (Exception exception)
        {
            _diagnostics.Error("PROFILE_CREATE_FAILED", exception);
            OperationStatus = Localization.UiStrings.Get("Operation_ProfileCreateFailed");
        }
    }

    private async Task ActivateProfileAsync()
    {
        if (SelectedProfile is null) return;
        await _drivers.SetActiveProfileAsync(SelectedProfile.Id);
        OperationStatus = Localization.UiStrings.Get("Operation_ProfileActivatedRestart");
    }

    private async Task SaveSettingsAsync()
    {
        if (DrivingThreshold is < 0 or > 20)
        {
            OperationStatus = UiStrings.Get("Validation_DrivingThresholdRange");
            return;
        }

        if (WeekOffset is < -6 or > 6)
        {
            OperationStatus = UiStrings.Get("Validation_WeekOffsetRange");
            return;
        }

        if (!UiCulture.TryNormalize(SelectedUiCulture?.CultureName, out var cultureName))
        {
            OperationStatus = UiStrings.Get("Validation_UnsupportedCulture");
            return;
        }

        try
        {
            await _settings.SaveAsync(new SettingsDto(DrivingThreshold, WeekOffset));
        }
        catch (Exception exception)
        {
            _diagnostics.Error("SETTINGS_SAVE_FAILED", exception);
            OperationStatus = UiStrings.Get("Operation_SettingsSaveFailed");
            return;
        }

        try
        {
            _culturePreferences.Save(cultureName);
        }
        catch (Exception exception)
        {
            _diagnostics.Error("UI_CULTURE_PREFERENCE_SAVE_FAILED", exception);
            OperationStatus = UiStrings.Get("Operation_CulturePreferenceSaveFailed");
            return;
        }

        OperationStatus = UiStrings.Get("Operation_SettingsSavedRestart");
    }

    private async Task ExportAsync()
    {
        var cardId = CurrentDriverCardId;
        var dialog = new SaveFileDialog
        {
            Filter = $"{Localization.UiStrings.Get("FileDialog_TachographSession")} (*.tacho)|*.tacho",
            FileName = $"{cardId}.tacho"
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            await using var stream = File.Create(dialog.FileName);
            await _export.ExportSessionAsync(cardId, stream);
            OperationStatus = Localization.UiStrings.Get("Operation_TachographSessionExported");
        }
        catch (Exception exception)
        {
            _diagnostics.Error("TACHOGRAPH_SESSION_EXPORT_FAILED", exception);
            OperationStatus =
                Localization.UiStrings.Get("Operation_TachographSessionExportFailed");
        }
    }

    private async Task ImportAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = $"{Localization.UiStrings.Get("FileDialog_TachographSession")} (*.tacho)|*.tacho"
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            await using var stream = File.OpenRead(dialog.FileName);
            var count = await _import.ImportSessionAsync(stream);
            await ReloadHistoryAsync(_cancellation.Token);
            OperationStatus = Localization.UiStrings.Format(
                Localization.UiPlural.Select(
                    count,
                    "Operation_ImportRecordCountOneFormat",
                    "Operation_ImportRecordCountFewFormat",
                    "Operation_ImportRecordCountManyFormat"),
                count);
            await ReportsWorkspace.RefreshAsync(_cancellation.Token);
        }
        catch (Exception exception)
        {
            _diagnostics.Error("TACHOGRAPH_SESSION_IMPORT_FAILED", exception);
            OperationStatus =
                Localization.UiStrings.Get("Operation_TachographSessionImportFailed");
        }
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

    private async Task<ReportExportResult> ExportWorkspaceReportAsync(
        ReportDto report,
        ReportExportFormat format,
        CancellationToken cancellationToken)
    {
        var (filter, extension, label) = format switch
        {
            ReportExportFormat.Pdf =>
                ($"{Localization.UiStrings.Get("ReportExport_Pdf")} (*.pdf)|*.pdf",
                    "pdf",
                    Localization.UiStrings.Get("ReportExport_Pdf")),
            ReportExportFormat.VtcJson =>
                ($"{Localization.UiStrings.Get("ReportExport_VtcJson")} (*.json)|*.json",
                    "json",
                    Localization.UiStrings.Get("ReportExport_VtcJson")),
            ReportExportFormat.CompensationCsv =>
                ($"{Localization.UiStrings.Get("ReportExport_CompensationCsvName")} (*.csv)|*.csv",
                    "csv",
                    Localization.UiStrings.Get("ReportExport_CompensationCsvName")),
            ReportExportFormat.RawActivityCsv =>
                ($"{Localization.UiStrings.Get("ReportExport_RawActivityCsvName")} (*.csv)|*.csv",
                    "csv",
                    Localization.UiStrings.Get("ReportExport_RawActivityCsvName")),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
        var dialog = new SaveFileDialog
        {
            Filter = filter,
            FileName = $"raport-{report.DriverCardId}-{format.ToString().ToLowerInvariant()}.{extension}"
        };
        if (dialog.ShowDialog() != true)
            return new ReportExportResult(false);

        try
        {
            await using var stream = File.Create(dialog.FileName);
            switch (format)
            {
                case ReportExportFormat.Pdf:
                    await _pdfReports.ExportAsync(report, stream, cancellationToken);
                    break;
                case ReportExportFormat.VtcJson:
                    await _reports.ExportVtcJsonAsync(report, stream, cancellationToken);
                    break;
                case ReportExportFormat.CompensationCsv:
                    await _reports.ExportCompensationCsvAsync(report, stream, cancellationToken);
                    break;
                case ReportExportFormat.RawActivityCsv:
                    await _reports.ExportCsvAsync(report, stream, cancellationToken);
                    break;
            }
            OperationStatus = Localization.UiStrings.Format(
                "Operation_ReportExportSavedFormat",
                label,
                dialog.FileName);
            return new ReportExportResult(true, dialog.FileName);
        }
        catch (Exception exception)
        {
            _diagnostics.Error("REPORT_EXPORT_FAILED", exception);
            OperationStatus = Localization.UiStrings.Format(
                "Operation_ReportExportFailedFormat",
                label);
            return new ReportExportResult(false);
        }
    }

    private async Task ExportDiagnosticReportAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter =
                $"{Localization.UiStrings.Get("FileDialog_DiagnosticReportZip")} (*.zip)|*.zip",
            FileName = string.Format(
                CultureInfo.InvariantCulture,
                "ets2-tachograph-diagnostics-{0:yyyyMMdd-HHmmss}.zip",
                DateTime.Now)
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
            OperationStatus = Localization.UiStrings.Format(
                "Operation_DiagnosticReportSavedFormat",
                dialog.FileName);
        }
        catch (Exception exception)
        {
            _diagnostics.Error("DIAGNOSTIC_REPORT_FAILED", exception);
            OperationStatus = Localization.UiStrings.Get("Operation_DiagnosticReportFailed");
        }
    }

    private async Task ReloadHistoryAsync(CancellationToken cancellationToken = default)
    {
        var records = new List<ActivityRecord>();
        foreach (var card in Profiles.SelectMany(x => x.Cards).DistinctBy(x => x.CardNumber))
            records.AddRange(await _crew.LoadDriverHistoryAsync(card.CardNumber, cancellationToken: cancellationToken));
        History.Clear();
        foreach (var record in records.OrderBy(x => x.Start).ThenBy(x => x.DriverCardId))
            History.Add(HistoryActivityRow.From(record));
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
            OperationStatus = Localization.UiStrings.Get("Operation_GapListRefreshFailed");
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
            ActivityGaps.Add(ActivityGapRow.From(gap));
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
            PendingRestAllocationChoices.Clear();
            foreach (var row in projection.CompensationObligations
                         .Select(item => CompensationDetailRow.From(
                             Localization.UiStrings.Get("History_CardHeader"),
                             item,
                             _gameCalendar))
                         .OrderBy(item => item.IsOpen ? 0 : 1)
                         .ThenBy(item => item.DueAtGameMinuteExclusive)
                         .ThenBy(item => item.ObligationId, StringComparer.Ordinal))
                CompensationDetails.Add(row);
            foreach (var allocation in projection.RestAllocations
                         .Where(item => item.IsPending)
                         .OrderBy(item => item.EndGameMinuteExclusive))
            {
                foreach (var candidate in allocation.Candidates)
                {
                    PendingRestAllocationChoices.Add(RestAllocationChoiceRow.From(
                        allocation,
                        candidate,
                        projection.CompensationObligations));
                }
            }
            OnPropertyChanged(nameof(CompensationDetailsHeader));
            OnPropertyChanged(nameof(HasPendingRestAllocations));
            OperationStatus = CompensationDetails.Count == 0
                ? Localization.UiStrings.Get("Operation_NoCompensationObligations")
                : Localization.UiStrings.Format(
                    CompensationDetails.Count == 1
                        ? "Operation_CompensationLoadedOneFormat"
                        : "Operation_CompensationLoadedManyFormat",
                    CompensationDetails.Count);
        }
        catch (Exception exception)
        {
            _diagnostics.Error("COMPENSATION_DETAILS_REFRESH_FAILED", exception);
            OperationStatus = Localization.UiStrings.Get("Operation_CompensationDetailsFailed");
        }
    }

    private async Task SelectRestAllocationAsync(RestAllocationChoiceRow? row)
    {
        if (row is null)
            return;
        try
        {
            var now = _crew.Current.Frame?.GameTime ??
                new GameTime(Math.Max(
                    row.EndGameMinuteExclusive + 1,
                    ReportsWorkspace.CurrentReport?.ToGameMinuteExclusive ?? 0));
            await _restAllocations.DecideAsync(
                row.DriverCardId,
                row.RestBlockId,
                row.CandidateId,
                now,
                DateTimeOffset.UtcNow,
                _cancellation.Token);
            await _crew.RefreshRestAllocationDecisionsAsync(
                row.DriverCardId,
                _cancellation.Token);
            await ReportsWorkspace.RefreshAsync(_cancellation.Token);
            await RefreshCompensationDetailsAsync();
            OperationStatus = Localization.UiStrings.Get("Operation_RestAllocationSaved");
        }
        catch (Exception exception)
        {
            _diagnostics.Error("REST_ALLOCATION_DECISION_FAILED", exception);
            OperationStatus = Localization.UiStrings.Get("Operation_RestAllocationFailed");
        }
    }

    private void CopyIdentifier(string value)
    {
        try
        {
            Clipboard.SetText(value);
            OperationStatus = Localization.UiStrings.Get("Operation_IdentifierCopied");
        }
        catch (Exception exception)
        {
            _diagnostics.Error("CLIPBOARD_COPY_FAILED", exception);
            OperationStatus = Localization.UiStrings.Get("Operation_IdentifierCopyFailed");
        }
    }

    private string FormatDeviceDeadline(
        long deadlineGameMinute,
        GameDeadlineSemantic semantic)
    {
        if (deadlineGameMinute < 0)
            return "—";

        return GameDeadlineFormatter.FormatDevice(new DeadlinePresentation(
            semantic,
            _gameCalendar.Resolve(new GameTime(deadlineGameMinute))));
    }

    private static string Format(long minutes) => minutes < 0 ? $"-{Format(-minutes)}" : $"{minutes / 60:00}:{minutes % 60:00}";
    private static string FormatWithLimit(long minutes, long limitMinutes) => $"{Format(minutes)} / {Format(limitMinutes)}";
    private static string FormatUsage(int used, int limit) => $"{used} / {limit}";
    private static string FormatCompensation(CompensationSummary summary)
    {
        if (summary.Count == 0)
            return "—";

        var count = summary.Count > 1 ? $" ({summary.Count})" : string.Empty;
        var status = summary.HasOverdue
            ? Localization.UiStrings.Get("DeviceCompensation_Overdue")
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

    private static string MaskCard(string? cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.StartsWith("BRAK", StringComparison.OrdinalIgnoreCase))
            return Localization.UiStrings.Get("Card_NoCard");
        return cardNumber.Length <= 4 ? "****" : $"****{cardNumber[^4..]}";
    }
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; OnPropertyChanged(name); return true; }
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    public event PropertyChangedEventHandler? PropertyChanged;
    public void Dispose() { SaveDeviceState(); JourneyPlanner.SaveInputState(); _clockTimer.Stop(); _cancellation.Cancel(); _cancellation.Dispose(); }

    private sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
    {
        public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;
        public void Execute(object? parameter)
        {
            if (CanExecute(parameter)) execute();
        }
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
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

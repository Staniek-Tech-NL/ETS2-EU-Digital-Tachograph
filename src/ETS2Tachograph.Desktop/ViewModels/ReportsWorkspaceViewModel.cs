using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Application.Services;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Desktop;

public enum ReportRangePreset
{
    CurrentRegulatoryWeek,
    Last24GameHours,
    AllHistory,
    Custom
}

public enum ReportPreviewStatus
{
    NoSelection,
    InvalidParameters,
    Loading,
    Current,
    CurrentIncomplete,
    OutOfDate,
    Error
}

public enum ReportWorkspaceTab
{
    Summary,
    Activities,
    Violations,
    Compensations,
    Completeness
}

public enum ReportExportFormat
{
    Pdf,
    VtcJson,
    CompensationCsv,
    RawActivityCsv
}

public sealed record ReportDriverOption(
    Guid ProfileId,
    string DriverName,
    string CardId,
    string DisplayName);

public sealed record ReportDayOption(long Day, string Name);

public sealed record ReportQueryDraft(
    string DriverCardId,
    long FromGameMinuteInclusive,
    long ToGameMinuteExclusive,
    ReportRangePreset RangePreset);

public sealed record ReportPreviewSnapshot(
    ReportQueryDraft Query,
    ReportDto Report,
    DateTimeOffset GeneratedAtUtc);

public sealed record ReportExportResult(bool Saved, string? Path = null);

public sealed record ReportActivityRow(
    string Start,
    string End,
    string Duration,
    string Activity,
    string Source,
    string Condition);

public sealed record ReportViolationRow(
    string DetectedAt,
    string Article,
    string Name,
    string Excess);

public sealed record ReportCompensationRow(
    string Status,
    string OriginalDebt,
    string RemainingDebt,
    string DueAt,
    string SourceRest,
    string PaymentRest,
    string SettledAt,
    string ObligationId);

public sealed class ReportsWorkspaceViewModel : INotifyPropertyChanged
{
    private readonly ReportService _reports;
    private readonly Func<long?> _currentGameMinute;
    private readonly int _weekEpochOffsetDays;
    private readonly Func<ReportDto, ReportExportFormat, CancellationToken, Task<ReportExportResult>>
        _export;
    private readonly Func<Task> _showGaps;
    private readonly Action<string>? _publishStatus;
    private readonly Action<string, Exception>? _diagnosticError;
    private readonly AsyncCommand _refreshCommand;
    private readonly AsyncCommand _exportPdfCommand;
    private readonly AsyncCommand _exportJsonCommand;
    private readonly AsyncCommand _exportCompensationCsvCommand;
    private readonly AsyncCommand _exportRawCsvCommand;
    private ReportDriverOption? _selectedDriver;
    private ReportRangePreset _selectedPreset = ReportRangePreset.CurrentRegulatoryWeek;
    private ReportPreviewStatus _previewStatus = ReportPreviewStatus.NoSelection;
    private ReportWorkspaceTab _selectedTab;
    private ReportDayOption? _fromDay;
    private ReportDayOption? _toDay;
    private string _fromTime = "00:00";
    private string _toTime = "00:00";
    private string _validationMessage = string.Empty;
    private string _statusMessage = Localization.UiStrings.Get("ReportStatus_SelectDriverAndRange");
    private string _statusDetail = string.Empty;
    private string _statusForeground = "#5F6874";
    private string _rangeDescription = "—";
    private string _driving = "—";
    private string _work = "—";
    private string _availability = "—";
    private string _rest = "—";
    private string _openDebt = "—";
    private string _violationCount = "—";
    private string _summaryIdentity = "—";
    private string _summaryRange = "—";
    private string _summaryGeneratedAt = "—";
    private string _completenessRange = "—";
    private string _completenessActivities = "—";
    private string _completenessGaps = "—";
    private string _completenessBalance = "—";
    private string _completenessEvidence = "—";
    private string _pendingAllocations = "—";
    private bool _showTechnicalData;
    private bool _isLoading;
    private bool _hasData;
    private long _resolvedFrom;
    private long _resolvedTo;
    private ReportAvailableRange _availableRange = new(false, 0, 0);
    private ReportPreviewSnapshot? _preview;

    public ReportsWorkspaceViewModel(
        ReportService reports,
        Func<long?> currentGameMinute,
        int weekEpochOffsetDays,
        Func<ReportDto, ReportExportFormat, CancellationToken, Task<ReportExportResult>> export,
        Func<Task> showGaps,
        Action<string>? publishStatus = null,
        Action<string, Exception>? diagnosticError = null)
    {
        _reports = reports;
        _currentGameMinute = currentGameMinute;
        _weekEpochOffsetDays = weekEpochOffsetDays;
        _export = export;
        _showGaps = showGaps;
        _publishStatus = publishStatus;
        _diagnosticError = diagnosticError;
        _refreshCommand = new AsyncCommand(() => RefreshAsync(), () => CanRefresh);
        _exportPdfCommand = ExportCommand(ReportExportFormat.Pdf);
        _exportJsonCommand = ExportCommand(ReportExportFormat.VtcJson);
        _exportCompensationCsvCommand = ExportCommand(ReportExportFormat.CompensationCsv);
        _exportRawCsvCommand = ExportCommand(ReportExportFormat.RawActivityCsv);
        RefreshCommand = _refreshCommand;
        ExportPdfCommand = _exportPdfCommand;
        ExportVtcJsonCommand = _exportJsonCommand;
        ExportCompensationCsvCommand = _exportCompensationCsvCommand;
        ExportRawCsvCommand = _exportRawCsvCommand;
        CurrentWeekCommand = new AsyncCommand(
            () => SelectPresetAsync(ReportRangePreset.CurrentRegulatoryWeek),
            () => !IsLoading && SelectedDriver is not null);
        Last24HoursCommand = new AsyncCommand(
            () => SelectPresetAsync(ReportRangePreset.Last24GameHours),
            () => !IsLoading && SelectedDriver is not null);
        AllHistoryCommand = new AsyncCommand(
            () => SelectPresetAsync(ReportRangePreset.AllHistory),
            () => !IsLoading && SelectedDriver is not null);
        CustomRangeCommand = new AsyncCommand(
            () => SelectPresetAsync(ReportRangePreset.Custom),
            () => !IsLoading && SelectedDriver is not null);
        ShowGapsCommand = new AsyncCommand(_showGaps, () => HasUnresolvedGaps);
    }

    public ObservableCollection<ReportDriverOption> Drivers { get; } = [];
    public ObservableCollection<ReportDayOption> DayOptions { get; } = [];
    public ObservableCollection<ReportActivityRow> Activities { get; } = [];
    public ObservableCollection<ReportViolationRow> Violations { get; } = [];
    public ObservableCollection<ReportCompensationRow> Compensations { get; } = [];

    public ICommand RefreshCommand { get; }
    public ICommand CurrentWeekCommand { get; }
    public ICommand Last24HoursCommand { get; }
    public ICommand AllHistoryCommand { get; }
    public ICommand CustomRangeCommand { get; }
    public ICommand ExportPdfCommand { get; }
    public ICommand ExportVtcJsonCommand { get; }
    public ICommand ExportCompensationCsvCommand { get; }
    public ICommand ExportRawCsvCommand { get; }
    public ICommand ShowGapsCommand { get; }

    public ReportDriverOption? SelectedDriver
    {
        get => _selectedDriver;
        set
        {
            if (!Set(ref _selectedDriver, value))
                return;
            MarkOutOfDate();
            RaiseCommands();
            if (value is not null)
                _ = ReloadSelectedDriverAsync();
        }
    }
    public ReportRangePreset SelectedPreset
    {
        get => _selectedPreset;
        private set
        {
            if (Set(ref _selectedPreset, value))
            {
                OnPropertyChanged(nameof(IsCustomRange));
                OnPropertyChanged(nameof(PresetDescription));
            }
        }
    }
    public bool IsCustomRange => SelectedPreset == ReportRangePreset.Custom;
    public string PresetDescription => SelectedPreset switch
    {
        ReportRangePreset.CurrentRegulatoryWeek => Localization.UiStrings.Get("ReportRange_CurrentWeekDescription"),
        ReportRangePreset.Last24GameHours => Localization.UiStrings.Get("ReportRange_Last24HoursDescription"),
        ReportRangePreset.AllHistory => Localization.UiStrings.Get("ReportRange_AllHistoryDescription"),
        _ => Localization.UiStrings.Get("ReportRange_CustomDescription")
    };
    public ReportDayOption? FromDay
    {
        get => _fromDay;
        set
        {
            if (Set(ref _fromDay, value))
                CustomInputChanged();
        }
    }
    public ReportDayOption? ToDay
    {
        get => _toDay;
        set
        {
            if (Set(ref _toDay, value))
                CustomInputChanged();
        }
    }
    public string FromTime
    {
        get => _fromTime;
        set
        {
            if (Set(ref _fromTime, value))
                CustomInputChanged();
        }
    }
    public string ToTime
    {
        get => _toTime;
        set
        {
            if (Set(ref _toTime, value))
                CustomInputChanged();
        }
    }
    public ReportPreviewStatus PreviewStatus
    {
        get => _previewStatus;
        private set
        {
            if (Set(ref _previewStatus, value))
            {
                OnPropertyChanged(nameof(HasPreview));
                OnPropertyChanged(nameof(CanExport));
                RaiseCommands();
            }
        }
    }
    public ReportWorkspaceTab SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (Set(ref _selectedTab, value))
                OnPropertyChanged(nameof(SelectedTabIndex));
        }
    }
    public int SelectedTabIndex
    {
        get => (int)SelectedTab;
        set
        {
            if (Enum.IsDefined(typeof(ReportWorkspaceTab), value))
                SelectedTab = (ReportWorkspaceTab)value;
        }
    }
    public string ValidationMessage { get => _validationMessage; private set => Set(ref _validationMessage, value); }
    public string StatusMessage { get => _statusMessage; private set => Set(ref _statusMessage, value); }
    public string StatusDetail { get => _statusDetail; private set => Set(ref _statusDetail, value); }
    public string StatusForeground { get => _statusForeground; private set => Set(ref _statusForeground, value); }
    public string RangeDescription { get => _rangeDescription; private set => Set(ref _rangeDescription, value); }
    public string Driving { get => _driving; private set => Set(ref _driving, value); }
    public string Work { get => _work; private set => Set(ref _work, value); }
    public string Availability { get => _availability; private set => Set(ref _availability, value); }
    public string Rest { get => _rest; private set => Set(ref _rest, value); }
    public string OpenDebt { get => _openDebt; private set => Set(ref _openDebt, value); }
    public string ViolationCount { get => _violationCount; private set => Set(ref _violationCount, value); }
    public string SummaryIdentity { get => _summaryIdentity; private set => Set(ref _summaryIdentity, value); }
    public string SummaryRange { get => _summaryRange; private set => Set(ref _summaryRange, value); }
    public string SummaryGeneratedAt { get => _summaryGeneratedAt; private set => Set(ref _summaryGeneratedAt, value); }
    public string CompletenessRange { get => _completenessRange; private set => Set(ref _completenessRange, value); }
    public string CompletenessActivities { get => _completenessActivities; private set => Set(ref _completenessActivities, value); }
    public string CompletenessGaps { get => _completenessGaps; private set => Set(ref _completenessGaps, value); }
    public string CompletenessBalance { get => _completenessBalance; private set => Set(ref _completenessBalance, value); }
    public string CompletenessEvidence { get => _completenessEvidence; private set => Set(ref _completenessEvidence, value); }
    public string PendingAllocations { get => _pendingAllocations; private set => Set(ref _pendingAllocations, value); }
    public bool ShowTechnicalData { get => _showTechnicalData; set => Set(ref _showTechnicalData, value); }
    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (Set(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(CanRefresh));
                OnPropertyChanged(nameof(CanExport));
                RaiseCommands();
            }
        }
    }
    public bool HasData { get => _hasData; private set => Set(ref _hasData, value); }
    public bool HasPreview => _preview is not null;
    public bool HasUnresolvedGaps => _preview?.Report.UnresolvedGapCount > 0;
    public bool HasPendingAllocations => _preview?.Report.PendingRestAllocation == true;
    public bool CanRefresh => !IsLoading && SelectedDriver is not null && HasData &&
                              (SelectedPreset != ReportRangePreset.Custom || ValidateCustomRange(updateMessage: false));
    public bool CanExport => !IsLoading && SelectedDriver is not null && HasData &&
                             PreviewStatus is ReportPreviewStatus.Current or
                                 ReportPreviewStatus.CurrentIncomplete or
                                 ReportPreviewStatus.OutOfDate;
    public ReportDto? CurrentReport => _preview?.Report;
    public int ViolationBadge => Violations.Count;
    public int CompensationBadge => Compensations.Count;
    public bool HasViolations => ViolationBadge > 0;
    public bool HasCompensations => CompensationBadge > 0;

    public async Task InitializeAsync(
        IEnumerable<DriverProfileDto> profiles,
        string? preferredCardId = null,
        CancellationToken cancellationToken = default)
    {
        Drivers.Clear();
        foreach (var profile in profiles)
        {
            foreach (var card in profile.Cards)
            {
                Drivers.Add(new ReportDriverOption(
                    profile.Id,
                    profile.DisplayName,
                    card.CardNumber,
                    Localization.UiStrings.Format(
                        "Report_DriverCardFormat",
                        profile.DisplayName,
                        card.CardNumber)));
            }
        }
        _selectedDriver = Drivers.FirstOrDefault(option =>
            string.Equals(option.CardId, preferredCardId, StringComparison.OrdinalIgnoreCase))
            ?? Drivers.FirstOrDefault();
        OnPropertyChanged(nameof(SelectedDriver));
        if (SelectedDriver is null)
        {
            PreviewStatus = ReportPreviewStatus.NoSelection;
            StatusMessage = Localization.UiStrings.Get("ReportStatus_NoReportableCard");
            return;
        }
        await SelectPresetAsync(SelectedPreset, cancellationToken);
    }

    public async Task SelectPresetAsync(
        ReportRangePreset preset,
        CancellationToken cancellationToken = default)
    {
        SelectedPreset = preset;
        if (SelectedDriver is null)
            return;
        _availableRange = await _reports.GetAvailableRangeAsync(
            SelectedDriver.CardId,
            cancellationToken);
        HasData = _availableRange.HasData;
        BuildDayOptions();
        if (!HasData)
        {
            _preview = null;
            Activities.Clear();
            Violations.Clear();
            Compensations.Clear();
            PreviewStatus = ReportPreviewStatus.NoSelection;
            StatusMessage = Localization.UiStrings.Get("ReportStatus_SelectedCardNoHistory");
            StatusDetail = Localization.UiStrings.Get("ReportStatus_ExportUnavailableNoData");
            RangeDescription = "—";
            RaiseProjectionProperties();
            return;
        }

        if (preset != ReportRangePreset.Custom)
            ResolvePreset(preset);
        else
            ResolveCustomDefaults();
        UpdateRangeDescription();
        MarkOutOfDate();
        await RefreshAsync(cancellationToken);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (IsLoading || SelectedDriver is null || !HasData)
            return;
        if (!TryBuildQuery(out var query))
        {
            PreviewStatus = ReportPreviewStatus.InvalidParameters;
            return;
        }

        IsLoading = true;
        PreviewStatus = ReportPreviewStatus.Loading;
        StatusMessage = Localization.UiStrings.Get("ReportStatus_CalculatingPreview");
        StatusDetail = Localization.UiStrings.Get("ReportStatus_ReadingCanonicalHistory");
        try
        {
            var report = await _reports.CreateAsync(
                query.DriverCardId,
                new GameTime(query.FromGameMinuteInclusive),
                new GameTime(query.ToGameMinuteExclusive),
                cancellationToken);
            Present(new ReportPreviewSnapshot(query, report, DateTimeOffset.UtcNow));
            _publishStatus?.Invoke(Localization.UiStrings.Format(
                Localization.UiPlural.Select(
                    report.Records.Count,
                    "Operation_ReportGeneratedOneFormat",
                    "Operation_ReportGeneratedFewFormat",
                    "Operation_ReportGeneratedManyFormat"),
                report.Records.Count,
                FormatMinutes(report.RangeMinutes)));
        }
        catch (Exception exception)
        {
            PreviewStatus = ReportPreviewStatus.Error;
            StatusMessage = Localization.UiStrings.Get("ReportStatus_PreviewError");
            StatusDetail = Localization.UiStrings.Get("ReportStatus_PreviewErrorDetail");
            StatusForeground = "#B3261E";
            _diagnosticError?.Invoke("report_preview_failed", exception);
            _publishStatus?.Invoke(Localization.UiStrings.Get("ReportStatus_PreviewErrorDetail"));
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task ExportAsync(
        ReportExportFormat format,
        CancellationToken cancellationToken = default)
    {
        if (SelectedDriver is null || !HasData || IsLoading)
            return;
        if (!TryBuildQuery(out _))
        {
            PreviewStatus = ReportPreviewStatus.InvalidParameters;
            return;
        }
        await RefreshAsync(cancellationToken);
        if (_preview is null ||
            PreviewStatus is not (ReportPreviewStatus.Current or ReportPreviewStatus.CurrentIncomplete))
            return;
        var result = await _export(_preview.Report, format, cancellationToken);
        if (result.Saved)
        {
            var message = Localization.UiStrings.Format(
                "Operation_ReportExportSavedFormat",
                ExportName(format),
                result.Path ?? "—");
            _publishStatus?.Invoke(message);
        }
    }

    private AsyncCommand ExportCommand(ReportExportFormat format) =>
        new(() => ExportAsync(format), () => CanExport);

    private async Task ReloadSelectedDriverAsync()
    {
        try
        {
            await SelectPresetAsync(SelectedPreset);
        }
        catch (Exception exception)
        {
            PreviewStatus = ReportPreviewStatus.Error;
            StatusMessage = Localization.UiStrings.Get("ReportStatus_PreviewError");
            StatusDetail = Localization.UiStrings.Get("ReportStatus_PreviewErrorDetail");
            StatusForeground = "#B3261E";
            _diagnosticError?.Invoke("report_reload_failed", exception);
        }
    }

    private void ResolvePreset(ReportRangePreset preset)
    {
        var now = _currentGameMinute() ?? _availableRange.ToGameMinuteExclusive;
        _resolvedTo = Math.Max(
            _availableRange.FromGameMinuteInclusive + 1,
            now);
        _resolvedFrom = preset switch
        {
            ReportRangePreset.CurrentRegulatoryWeek => Math.Max(
                _availableRange.FromGameMinuteInclusive,
                GameWeek.From(new GameTime(now), _weekEpochOffsetDays)
                    .GetBounds(_weekEpochOffsetDays)
                    .StartGameMinute),
            ReportRangePreset.Last24GameHours => Math.Max(
                _availableRange.FromGameMinuteInclusive,
                checked(_resolvedTo - GameWeek.MinutesPerDay)),
            _ => _availableRange.FromGameMinuteInclusive
        };
        if (preset == ReportRangePreset.AllHistory)
            _resolvedTo = _availableRange.ToGameMinuteExclusive;
        SetCustomControls(_resolvedFrom, _resolvedTo);
    }

    private void ResolveCustomDefaults()
    {
        if (_resolvedTo <= _resolvedFrom)
        {
            _resolvedFrom = _availableRange.FromGameMinuteInclusive;
            _resolvedTo = _availableRange.ToGameMinuteExclusive;
            SetCustomControls(_resolvedFrom, _resolvedTo);
        }
    }

    private void SetCustomControls(long from, long to)
    {
        var fromDay = (from / GameWeek.MinutesPerDay) + 1;
        var toDay = (to / GameWeek.MinutesPerDay) + 1;
        _fromDay = DayOptions.FirstOrDefault(option => option.Day == fromDay);
        _toDay = DayOptions.FirstOrDefault(option => option.Day == toDay);
        _fromTime = Clock(from);
        _toTime = Clock(to);
        OnPropertyChanged(nameof(FromDay));
        OnPropertyChanged(nameof(ToDay));
        OnPropertyChanged(nameof(FromTime));
        OnPropertyChanged(nameof(ToTime));
    }

    private void BuildDayOptions()
    {
        DayOptions.Clear();
        if (!HasData)
            return;
        var first = (_availableRange.FromGameMinuteInclusive / GameWeek.MinutesPerDay) + 1;
        var endReference = Math.Max(
            _availableRange.ToGameMinuteExclusive,
            _currentGameMinute() ?? 0);
        var last = (endReference / GameWeek.MinutesPerDay) + 1;
        for (var day = first; day <= last; day++)
            DayOptions.Add(new ReportDayOption(
                day,
                Localization.UiStrings.Format("GameCalendar_DayFormat", day)));
    }

    private bool TryBuildQuery(out ReportQueryDraft query)
    {
        query = default!;
        ValidationMessage = string.Empty;
        if (SelectedDriver is null)
        {
            ValidationMessage = Localization.UiStrings.Get("ReportValidation_SelectDriverCard");
            return false;
        }
        if (SelectedPreset == ReportRangePreset.Custom)
        {
            if (!ValidateCustomRange(updateMessage: true))
                return false;
            _resolvedFrom = ToGameMinute(FromDay!.Day, FromTime);
            _resolvedTo = ToGameMinute(ToDay!.Day, ToTime);
        }
        if (_resolvedTo <= _resolvedFrom)
        {
            ValidationMessage = Localization.UiStrings.Get("ReportValidation_EndAfterStart");
            return false;
        }
        query = new ReportQueryDraft(
            SelectedDriver.CardId,
            _resolvedFrom,
            _resolvedTo,
            SelectedPreset);
        return true;
    }

    private bool ValidateCustomRange(bool updateMessage)
    {
        string? error = null;
        if (FromDay is null || ToDay is null)
            error = Localization.UiStrings.Get("ReportValidation_SelectStartEndDay");
        else if (!TryParseClock(FromTime, out _) || !TryParseClock(ToTime, out _))
            error = Localization.UiStrings.Get("ReportValidation_TimeFormat");
        else if (ToGameMinute(ToDay.Day, ToTime) <= ToGameMinute(FromDay.Day, FromTime))
            error = Localization.UiStrings.Get("ReportValidation_EndAfterStart");
        if (updateMessage)
            ValidationMessage = error ?? string.Empty;
        return error is null;
    }

    private void CustomInputChanged()
    {
        if (!IsCustomRange)
            return;
        ValidateCustomRange(updateMessage: true);
        UpdateRangeDescription();
        MarkOutOfDate();
    }

    private void MarkOutOfDate()
    {
        if (_preview is not null)
        {
            PreviewStatus = ReportPreviewStatus.OutOfDate;
            StatusMessage = Localization.UiStrings.Get("ReportStatus_ParametersChanged");
            StatusDetail = Localization.UiStrings.Get("ReportStatus_RefreshBeforeAnalysisExport");
            StatusForeground = "#C67A00";
        }
        OnPropertyChanged(nameof(CanRefresh));
        OnPropertyChanged(nameof(CanExport));
        RaiseCommands();
    }

    private void Present(ReportPreviewSnapshot preview)
    {
        _preview = preview;
        var report = preview.Report;
        var calendar = new GameCalendarResolver(
            new GameCalendarContext(_weekEpochOffsetDays));
        PreviewStatus = report.EvidenceComplete
            ? ReportPreviewStatus.Current
            : ReportPreviewStatus.CurrentIncomplete;
        StatusMessage = report.EvidenceComplete
            ? Localization.UiStrings.Get("ReportStatus_PreviewCurrent")
            : Localization.UiStrings.Get("ReportStatus_ReportIncomplete");
        var openObligations = report.CompensationObligations.Count(item => item.IsOpen);
        var pendingAllocations = report.RestAllocations.Count(item => item.IsPending);
        StatusDetail = report.EvidenceComplete
            ? Localization.UiStrings.Format(
                "ReportStatus_DataCompleteFormat",
                CountText(
                    report.Violations.Count,
                    "ReportStatus_ViolationCountOneFormat",
                    "ReportStatus_ViolationCountFewFormat",
                    "ReportStatus_ViolationCountManyFormat"),
                CountText(
                    openObligations,
                    "ReportStatus_OpenObligationCountOneFormat",
                    "ReportStatus_OpenObligationCountFewFormat",
                    "ReportStatus_OpenObligationCountManyFormat"))
            : Localization.UiStrings.Format(
                "ReportStatus_DataIncompleteFormat",
                CountText(
                    report.UnresolvedGapCount,
                    "ReportStatus_UnresolvedGapCountOneFormat",
                    "ReportStatus_UnresolvedGapCountFewFormat",
                    "ReportStatus_UnresolvedGapCountManyFormat"),
                FormatMinutes(report.GapMinutes),
                CoverageStatus(report),
                CountText(
                    pendingAllocations,
                    "ReportStatus_PendingAllocationCountOneFormat",
                    "ReportStatus_PendingAllocationCountFewFormat",
                    "ReportStatus_PendingAllocationCountManyFormat"));
        StatusForeground = report.EvidenceComplete ? "#258A4B" : "#C67A00";
        Driving = FormatMinutes(report.DrivingMinutes);
        Work = FormatMinutes(report.OtherWorkMinutes);
        Availability = FormatMinutes(report.AvailabilityMinutes);
        Rest = FormatMinutes(report.RestMinutes);
        OpenDebt = FormatMinutes(report.CompensationObligations
            .Where(item => item.IsOpen)
            .Sum(item => item.RemainingMinutes));
        ViolationCount = report.Violations.Count.ToString(CultureInfo.InvariantCulture);
        SummaryIdentity = SelectedDriver?.DisplayName ?? report.DriverCardId;
        SummaryRange =
            $"{Format(calendar, report.FromGameMinute)} → " +
            $"{Format(calendar, report.ToGameMinuteExclusive)}";
        SummaryGeneratedAt = preview.GeneratedAtUtc.ToLocalTime()
            .ToString("g", CultureInfo.CurrentCulture);
        CompletenessRange = FormatMinutes(report.RangeMinutes);
        CompletenessActivities = FormatMinutes(report.TotalMinutes);
        CompletenessGaps =
            $"{report.UnresolvedGapCount} · {FormatMinutes(report.GapMinutes)}";
        CompletenessBalance = report.CoverageBalanceText;
        CompletenessEvidence = report.EvidenceComplete ? Localization.UiStrings.Get("ReportEvidence_Complete") : Localization.UiStrings.Get("ReportEvidence_Incomplete");
        PendingAllocations = report.RestAllocations.Count(item => item.IsPending)
            .ToString(CultureInfo.InvariantCulture);

        Activities.Clear();
        foreach (var record in report.Records)
        {
            Activities.Add(new ReportActivityRow(
                Format(calendar, record.Start.TotalMinutes),
                Format(calendar, record.EndExclusive.TotalMinutes),
                FormatMinutes(record.DurationMinutes),
                ActivityName(record.Activity),
                Localization.UiStrings.Format(
                    "ReportActivity_SourceFormat",
                    ActivitySourceName(record.Source)),
                Localization.UiStrings.Format(
                    "ReportActivity_ConditionFormat",
                    SpecialConditionName(record.Condition))));
        }
        Violations.Clear();
        foreach (var violation in report.Violations)
        {
            Violations.Add(new ReportViolationRow(
                Format(calendar, violation.DetectedAtGameMinute),
                violation.Article,
                violation.Type,
                FormatMinutes(violation.ExcessMinutes)));
        }
        Compensations.Clear();
        foreach (var obligation in report.CompensationObligations)
        {
            Compensations.Add(new ReportCompensationRow(
                CompensationStatus(obligation.Status),
                FormatMinutes(obligation.OriginalOwedMinutes),
                FormatMinutes(obligation.RemainingMinutes),
                Format(calendar, obligation.DueAtGameMinuteExclusive),
                ShortId(obligation.SourceRestBlockId),
                ShortId(obligation.PaymentRestBlockId),
                obligation.SettledAtGameMinute is { } settled
                    ? Format(calendar, settled)
                    : "—",
                obligation.ObligationId));
        }
        RaiseProjectionProperties();
    }

    private void RaiseProjectionProperties()
    {
        OnPropertyChanged(nameof(HasPreview));
        OnPropertyChanged(nameof(HasUnresolvedGaps));
        OnPropertyChanged(nameof(HasPendingAllocations));
        OnPropertyChanged(nameof(CanExport));
        OnPropertyChanged(nameof(ViolationBadge));
        OnPropertyChanged(nameof(CompensationBadge));
        OnPropertyChanged(nameof(HasViolations));
        OnPropertyChanged(nameof(HasCompensations));
        RaiseCommands();
    }

    private void UpdateRangeDescription()
    {
        if (SelectedPreset == ReportRangePreset.Custom &&
            FromDay is not null && ToDay is not null &&
            TryParseClock(FromTime, out _) && TryParseClock(ToTime, out _))
        {
            var duration = ToGameMinute(ToDay.Day, ToTime) -
                           ToGameMinute(FromDay.Day, FromTime);
            RangeDescription = duration > 0
                ? Localization.UiStrings.Format(
                    "ReportRange_DescriptionFormat",
                    FormatLongDuration(duration))
                : Localization.UiStrings.Get("ReportRange_Invalid");
            return;
        }
        RangeDescription = _resolvedTo > _resolvedFrom
            ? Localization.UiStrings.Format(
                "ReportRange_DescriptionFormat",
                FormatLongDuration(_resolvedTo - _resolvedFrom))
            : "—";
    }

    private void RaiseCommands()
    {
        _refreshCommand.RaiseCanExecuteChanged();
        _exportPdfCommand.RaiseCanExecuteChanged();
        _exportJsonCommand.RaiseCanExecuteChanged();
        _exportCompensationCsvCommand.RaiseCanExecuteChanged();
        _exportRawCsvCommand.RaiseCanExecuteChanged();
        (CurrentWeekCommand as AsyncCommand)?.RaiseCanExecuteChanged();
        (Last24HoursCommand as AsyncCommand)?.RaiseCanExecuteChanged();
        (AllHistoryCommand as AsyncCommand)?.RaiseCanExecuteChanged();
        (CustomRangeCommand as AsyncCommand)?.RaiseCanExecuteChanged();
        (ShowGapsCommand as AsyncCommand)?.RaiseCanExecuteChanged();
    }

    private static long ToGameMinute(long displayedDay, string clock)
    {
        TryParseClock(clock, out var time);
        return checked(
            ((displayedDay - 1) * GameWeek.MinutesPerDay) +
            (time.Hour * 60L) +
            time.Minute);
    }

    private static bool TryParseClock(string? value, out TimeOnly time) =>
        TimeOnly.TryParseExact(
            value,
            "HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out time);

    private static string Clock(long minute)
    {
        var value = minute % GameWeek.MinutesPerDay;
        return $"{value / 60:00}:{value % 60:00}";
    }

    private static string Format(
        GameCalendarResolver calendar,
        long minute) =>
        GameCalendarFormatter.FormatCompact(
            calendar.Resolve(new GameTime(minute)));

    private static string FormatMinutes(long minutes) =>
        $"{minutes / 60:00}:{minutes % 60:00}";

    private static string CoverageStatus(ReportDto report)
    {
        var difference = report.RangeMinutes - report.CoveredMinutes;
        return difference switch
        {
            > 0 => Localization.UiStrings.Format(
                "ReportCoverage_MissingFormat",
                FormatMinutes(difference)),
            < 0 => Localization.UiStrings.Format(
                "ReportCoverage_ExcessFormat",
                FormatMinutes(-difference)),
            _ => Localization.UiStrings.Get("ReportCoverage_Matches")
        };
    }

    private static string FormatLongDuration(long minutes)
    {
        var days = minutes / GameWeek.MinutesPerDay;
        var time = $"{minutes % GameWeek.MinutesPerDay / 60:00}:{minutes % 60:00}";
        return Localization.UiStrings.Format(
            days == 1 ? "ReportDuration_DayOneFormat" : "ReportDuration_DaysFormat",
            days,
            time);
    }

    private static string ActivityName(DriverActivity activity) => activity switch
    {
        DriverActivity.Driving => Localization.UiStrings.Get("Activity_Driving"),
        DriverActivity.OtherWork => Localization.UiStrings.Get("Activity_OtherWork"),
        DriverActivity.Availability => Localization.UiStrings.Get("Activity_Availability"),
        DriverActivity.BreakOrRest => Localization.UiStrings.Get("Activity_Rest"),
        DriverActivity.OutOfScope => Localization.UiStrings.Get("ReportActivity_OutOfScope"),
        DriverActivity.Unknown => Localization.UiStrings.Get("Activity_Unknown"),
        _ => throw new ArgumentOutOfRangeException(nameof(activity), activity, null)
    };

    private static string CompensationStatus(
        WeeklyRestCompensationStatusDto status) => status switch
    {
        WeeklyRestCompensationStatusDto.OpenOnTime =>
            Localization.UiStrings.Get("ReportCompensationStatus_OpenOnTime"),
        WeeklyRestCompensationStatusDto.Overdue =>
            Localization.UiStrings.Get("ReportCompensationStatus_Overdue"),
        WeeklyRestCompensationStatusDto.PaidOnTime =>
            Localization.UiStrings.Get("ReportCompensationStatus_PaidOnTime"),
        WeeklyRestCompensationStatusDto.PaidLate =>
            Localization.UiStrings.Get("ReportCompensationStatus_PaidLate"),
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    private static string ShortId(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? "—"
            : value.Length <= 12 ? value : value[..12] + "…";

    private static string ExportName(ReportExportFormat format) => format switch
    {
        ReportExportFormat.Pdf => Localization.UiStrings.Get("ReportExport_Pdf"),
        ReportExportFormat.VtcJson => Localization.UiStrings.Get("ReportExport_VtcJson"),
        ReportExportFormat.CompensationCsv =>
            Localization.UiStrings.Get("ReportExport_CompensationCsvName"),
        ReportExportFormat.RawActivityCsv =>
            Localization.UiStrings.Get("ReportExport_RawActivityCsvName"),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
    };

    private static string CountText(
        long count,
        string one,
        string few,
        string many) =>
        Localization.UiStrings.Format(
            Localization.UiPlural.Select(count, one, few, many),
            count);

    private static string ActivitySourceName(ActivitySource source) => source switch
    {
        ActivitySource.Telemetry => Localization.UiStrings.Get("ActivitySource_Telemetry"),
        ActivitySource.Manual => Localization.UiStrings.Get("ActivitySource_Manual"),
        ActivitySource.Reconstructed => Localization.UiStrings.Get("ActivitySource_Reconstructed"),
        ActivitySource.Mixed => Localization.UiStrings.Get("ActivitySource_Mixed"),
        ActivitySource.ManualEntry => Localization.UiStrings.Get("ActivitySource_ManualEntry"),
        ActivitySource.AutomaticCrewReconstruction =>
            Localization.UiStrings.Get("ActivitySource_AutomaticCrewReconstruction"),
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
    };

    private static string SpecialConditionName(SpecialCondition condition) => condition switch
    {
        SpecialCondition.None => Localization.UiStrings.Get("SpecialCondition_None"),
        SpecialCondition.FerryCrossing =>
            Localization.UiStrings.Get("SpecialCondition_FerryCrossing"),
        SpecialCondition.Mixed => Localization.UiStrings.Get("SpecialCondition_Mixed"),
        SpecialCondition.CrewBreakInMotion =>
            Localization.UiStrings.Get("SpecialCondition_CrewBreakInMotion"),
        _ => throw new ArgumentOutOfRangeException(nameof(condition), condition, null)
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private sealed class AsyncCommand(
        Func<Task> execute,
        Func<bool>? canExecute = null) : ICommand
    {
        private bool _running;
        public bool CanExecute(object? parameter) =>
            !_running && (canExecute?.Invoke() ?? true);
        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
                return;
            _running = true;
            RaiseCanExecuteChanged();
            try { await execute(); }
            finally
            {
                _running = false;
                RaiseCanExecuteChanged();
            }
        }
        public void RaiseCanExecuteChanged() =>
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        public event EventHandler? CanExecuteChanged;
    }
}

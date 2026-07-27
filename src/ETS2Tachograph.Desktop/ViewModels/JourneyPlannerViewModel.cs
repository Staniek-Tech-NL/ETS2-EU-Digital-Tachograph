using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ETS2Tachograph.Application.Services;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.RuleEngine.JourneyPlanning;

namespace ETS2Tachograph.Desktop;

public sealed record JourneyPlannerModeOption(bool IsMarketOffer, string Name);
public sealed record JourneyPlannerSlotOption(int Slot, string Name);
public sealed record GameWeekdayOption(GameWeekday Value, string Name);
public sealed record JourneyPlanSegmentRow(
    int Number,
    string Start,
    string End,
    string Vehicle,
    string Slot1,
    string Slot2,
    string Duration,
    string Reason);

public sealed class JourneyPlannerViewModel : INotifyPropertyChanged, IDataErrorInfo
{
    private readonly IDeliveryPlannerService _service;
    private readonly Func<string, string> _driverName;
    private readonly IJourneyPlannerInputStateStore? _inputStateStore;
    private readonly Action<string, Exception>? _diagnosticError;
    private readonly AsyncCommand _calculateCommand;
    private JourneyPlannerModeOption _selectedMode;
    private JourneyPlannerSlotOption _selectedSlot;
    private GameWeekdayOption _windowStartDay;
    private GameWeekdayOption _windowEndDay;
    private string _driveToPickup = "01:00";
    private string _offerExpiresIn = "09:22";
    private string _loadedDrive = "03:11";
    private int _windowStartHour = 21;
    private int _windowStartMinute = 54;
    private int _windowEndHour = 1;
    private int _windowEndMinute = 16;
    private string _pickupWork = "00:15";
    private string _unloadingWork = "00:30";
    private string _postDeliveryWork = "00:00";
    private string _tightMargin = "01:00";
    private string _validationMessage = string.Empty;
    private string _statusText = Localization.UiStrings.Get("PlannerStatus_CheckingReadiness");
    private string _currentTimeText = "—";
    private string _crewText = Localization.UiStrings.Get("PlannerCrew_WaitingForSnapshot");
    private string _offerExpiryText = "—";
    private string _deliveryWindowText = "—";
    private string _pickupText = "—";
    private string _arrivalText = "—";
    private string _completionText = "—";
    private string _marginText = "—";
    private string _statusForeground = "#5F6874";
    private string _inputOriginText = Localization.UiStrings.Get("PlannerInput_DefaultValuesAutosave");
    private bool _hasResult;
    private bool _snapshotReady;
    private bool _requiresRecalculation;
    private CrewDeliveryPlanSnapshotIdentity? _resultIdentity;
    private GameCalendarResolver? _previewCalendar;
    private long? _previewNow;

    public JourneyPlannerViewModel(
        IDeliveryPlannerService service,
        Func<string, string>? driverName = null,
        IJourneyPlannerInputStateStore? inputStateStore = null,
        Action<string, Exception>? diagnosticError = null)
    {
        _service = service;
        _driverName = driverName ?? (cardId => cardId);
        _inputStateStore = inputStateStore;
        _diagnosticError = diagnosticError;
        Modes =
        [
            new(true, Localization.UiStrings.Get("PlannerMode_MarketOffer")),
            new(false, Localization.UiStrings.Get("PlannerMode_ActiveDelivery"))
        ];
        Slots =
        [
            new(1, Localization.UiStrings.Get("PlannerSlot_Driver")),
            new(2, Localization.UiStrings.Get("PlannerSlot_CoDriver"))
        ];
        Weekdays =
        [
            new(GameWeekday.Monday, Localization.UiStrings.Get("Weekday_Full_Monday")),
            new(GameWeekday.Tuesday, Localization.UiStrings.Get("Weekday_Full_Tuesday")),
            new(GameWeekday.Wednesday, Localization.UiStrings.Get("Weekday_Full_Wednesday")),
            new(GameWeekday.Thursday, Localization.UiStrings.Get("Weekday_Full_Thursday")),
            new(GameWeekday.Friday, Localization.UiStrings.Get("Weekday_Full_Friday")),
            new(GameWeekday.Saturday, Localization.UiStrings.Get("Weekday_Full_Saturday")),
            new(GameWeekday.Sunday, Localization.UiStrings.Get("Weekday_Full_Sunday"))
        ];
        _selectedMode = Modes[0];
        _selectedSlot = Slots[0];
        _windowStartDay = Weekdays[3];
        _windowEndDay = Weekdays[5];
        Hours = Enumerable.Range(0, 24).ToArray();
        Minutes = Enumerable.Range(0, 60).ToArray();
        RestoreInputState();
        _calculateCommand = new AsyncCommand(CalculateAsync, () => CanCalculate);
        CalculateCommand = _calculateCommand;
        SetDurationPresetCommand = new RelayCommand<string>(SetDurationPreset);
        AdjustDurationCommand = new RelayCommand<string>(AdjustDuration);
        _ = RefreshReadinessAsync();
    }

    public IReadOnlyList<JourneyPlannerModeOption> Modes { get; }
    public IReadOnlyList<JourneyPlannerSlotOption> Slots { get; }
    public IReadOnlyList<GameWeekdayOption> Weekdays { get; }
    public IReadOnlyList<int> Hours { get; }
    public IReadOnlyList<int> Minutes { get; }
    public ObservableCollection<JourneyPlanSegmentRow> Segments { get; } = [];
    public ObservableCollection<string> Warnings { get; } = [];
    public ObservableCollection<string> Summary { get; } = [];
    public ObservableCollection<string> ReadinessIssues { get; } = [];
    public ICommand CalculateCommand { get; }
    public ICommand SetDurationPresetCommand { get; }
    public ICommand AdjustDurationCommand { get; }

    public JourneyPlannerModeOption SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (Set(ref _selectedMode, value))
            {
                OnPropertyChanged(nameof(IsMarketOffer));
                OnPropertyChanged(nameof(IsMarketOfferMode));
                OnPropertyChanged(nameof(IsActiveDeliveryMode));
                InputChanged();
            }
        }
    }
    public bool IsMarketOffer => SelectedMode.IsMarketOffer;
    public bool IsMarketOfferMode
    {
        get => IsMarketOffer;
        set
        {
            if (value)
                SelectedMode = Modes[0];
        }
    }
    public bool IsActiveDeliveryMode
    {
        get => !IsMarketOffer;
        set
        {
            if (value)
                SelectedMode = Modes[1];
        }
    }
    public JourneyPlannerSlotOption SelectedSlot
    {
        get => _selectedSlot;
        set
        {
            if (Set(ref _selectedSlot, value))
                InputChanged();
        }
    }
    public string DriveToPickup { get => _driveToPickup; set => SetInput(ref _driveToPickup, value); }
    public string OfferExpiresIn { get => _offerExpiresIn; set => SetInput(ref _offerExpiresIn, value); }
    public string LoadedDrive { get => _loadedDrive; set => SetInput(ref _loadedDrive, value); }
    public GameWeekdayOption WindowStartDay { get => _windowStartDay; set => SetInput(ref _windowStartDay, value); }
    public GameWeekdayOption WindowEndDay { get => _windowEndDay; set => SetInput(ref _windowEndDay, value); }
    public int WindowStartHour { get => _windowStartHour; set => SetInput(ref _windowStartHour, value); }
    public int WindowStartMinute { get => _windowStartMinute; set => SetInput(ref _windowStartMinute, value); }
    public int WindowEndHour { get => _windowEndHour; set => SetInput(ref _windowEndHour, value); }
    public int WindowEndMinute { get => _windowEndMinute; set => SetInput(ref _windowEndMinute, value); }
    public string PickupWork { get => _pickupWork; set => SetInput(ref _pickupWork, value); }
    public string UnloadingWork { get => _unloadingWork; set => SetInput(ref _unloadingWork, value); }
    public string PostDeliveryWork { get => _postDeliveryWork; set => SetInput(ref _postDeliveryWork, value); }
    public string TightMargin { get => _tightMargin; set => SetInput(ref _tightMargin, value); }
    public string ValidationMessage { get => _validationMessage; private set => Set(ref _validationMessage, value); }
    public string StatusText { get => _statusText; private set => Set(ref _statusText, value); }
    public string CurrentTimeText { get => _currentTimeText; private set => Set(ref _currentTimeText, value); }
    public string CrewText { get => _crewText; private set => Set(ref _crewText, value); }
    public string OfferExpiryText { get => _offerExpiryText; private set => Set(ref _offerExpiryText, value); }
    public string DeliveryWindowText { get => _deliveryWindowText; private set => Set(ref _deliveryWindowText, value); }
    public string PickupText { get => _pickupText; private set => Set(ref _pickupText, value); }
    public string ArrivalText { get => _arrivalText; private set => Set(ref _arrivalText, value); }
    public string CompletionText { get => _completionText; private set => Set(ref _completionText, value); }
    public string MarginText { get => _marginText; private set => Set(ref _marginText, value); }
    public string StatusForeground { get => _statusForeground; private set => Set(ref _statusForeground, value); }
    public string InputOriginText { get => _inputOriginText; private set => Set(ref _inputOriginText, value); }
    public bool HasResult { get => _hasResult; private set => Set(ref _hasResult, value); }
    public bool SnapshotReady
    {
        get => _snapshotReady;
        private set
        {
            if (Set(ref _snapshotReady, value))
            {
                OnPropertyChanged(nameof(CanCalculate));
                _calculateCommand.RaiseCanExecuteChanged();
            }
        }
    }
    public bool CanCalculate => SnapshotReady && InputsAreValid();

    public async Task RefreshReadinessAsync()
    {
        DeliveryPlannerReadiness readiness;
        try
        {
            readiness = await _service.GetReadinessAsync();
        }
        catch (Exception exception)
        {
            _diagnosticError?.Invoke("PLANNER_READINESS_FAILED", exception);
            SnapshotReady = false;
            ReadinessIssues.Clear();
            ReadinessIssues.Add(Localization.UiStrings.Get("PlannerError_SnapshotLoadFailed"));
            StatusText = Localization.UiStrings.Get("PlannerVerdict_UnreliableData");
            StatusForeground = "#B3261E";
            return;
        }
        ReadinessIssues.Clear();
        if (!readiness.IsReady)
        {
            ReadinessIssues.Add(readiness.HasBlockingCardRemovedGap
                ? Localization.UiStrings.Get("PlannerReadiness_ResolveCardRemovalGap")
                : Localization.UiStrings.Get("PlannerReadiness_CurrentCrewSnapshotRequired"));
        }
        SnapshotReady = readiness.IsReady;
        CrewText = readiness.Slot1CardId is not null && readiness.Slot2CardId is not null
            ? Localization.UiStrings.Format(
                "PlannerCrew_CrewFormat",
                _driverName(readiness.Slot1CardId),
                _driverName(readiness.Slot2CardId))
            : Localization.UiStrings.Get("PlannerCrew_Incomplete");
        if (readiness.CurrentGameMinute is { } now)
        {
            _previewNow = now;
            _previewCalendar = new GameCalendarResolver(
                new GameCalendarContext(readiness.WeekEpochOffsetDays));
            CurrentTimeText = Format(_previewCalendar, now);
            RefreshInputPreviews();
        }
        if (!SnapshotReady)
        {
            StatusText = Localization.UiStrings.Get("PlannerVerdict_UnreliableData");
            StatusForeground = "#B3261E";
        }
        else if (!HasResult && !_requiresRecalculation)
        {
            StatusText = Localization.UiStrings.Get("PlannerStatus_DataReady");
            StatusForeground = "#5F6874";
        }
    }

    public void ObserveStateChange()
    {
        if (_resultIdentity is not null && !_service.IsCurrent(_resultIdentity))
            InvalidateResult(Localization.UiStrings.Get("PlannerStatus_CrewStateChanged"));
    }

    public static bool TryParseDuration(string? text, out int minutes)
    {
        minutes = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var normalized = string.Concat(text.Where(character => !char.IsWhiteSpace(character)))
            .ToLowerInvariant();
        try
        {
            if (normalized.Contains(':'))
            {
                var parts = normalized.Split(':');
                if (parts.Length != 2 ||
                    !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var hours) ||
                    !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var minutePart) ||
                    hours < 0 || minutePart is < 0 or > 59)
                    return false;
                minutes = checked((hours * 60) + minutePart);
                return true;
            }

            var hourMarker = normalized.IndexOf('h');
            if (hourMarker >= 0)
            {
                if (normalized.LastIndexOf('h') != hourMarker ||
                    !int.TryParse(normalized[..hourMarker], NumberStyles.None, CultureInfo.InvariantCulture, out var hours) ||
                    hours < 0)
                    return false;
                var minuteText = normalized[(hourMarker + 1)..];
                var minutePart = 0;
                if (minuteText.Length > 0 &&
                    (!int.TryParse(minuteText, NumberStyles.None, CultureInfo.InvariantCulture, out minutePart) ||
                     minutePart is < 0 or > 59))
                    return false;
                minutes = checked((hours * 60) + minutePart);
                return true;
            }

            if (normalized.Contains('.') || normalized.Contains(','))
            {
                var invariant = normalized.Replace(',', '.');
                if (!decimal.TryParse(invariant, NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture, out var fractionalHours) ||
                    fractionalHours < 0)
                    return false;
                var totalMinutes = fractionalHours * 60;
                if (totalMinutes != decimal.Truncate(totalMinutes) || totalMinutes > int.MaxValue)
                    return false;
                minutes = decimal.ToInt32(totalMinutes);
                return true;
            }

            return int.TryParse(normalized, NumberStyles.None, CultureInfo.InvariantCulture, out minutes) &&
                   minutes >= 0;
        }
        catch (OverflowException)
        {
            minutes = 0;
            return false;
        }
    }

    public async Task CalculateAsync()
    {
        ValidationMessage = string.Empty;
        await RefreshReadinessAsync();
        if (!CanCalculate)
        {
            ValidationMessage = SnapshotReady
                ? FirstValidationError()
                : ReadinessIssues.FirstOrDefault() ?? Localization.UiStrings.Get("PlannerValidation_NoCurrentSnapshot");
            return;
        }

        ParseCommon(out var loaded, out var unloading, out var post, out var tight);
        var windowStart = new GameWeekdayTime(
            WindowStartDay.Value, WindowStartHour, WindowStartMinute);
        var windowEnd = new GameWeekdayTime(
            WindowEndDay.Value, WindowEndHour, WindowEndMinute);
        try
        {
            DeliveryPlanResult result;
            if (IsMarketOffer)
            {
                TryParseDuration(DriveToPickup, out var approach);
                TryParseDuration(OfferExpiresIn, out var expiry);
                TryParseDuration(PickupWork, out var pickup);
                result = await _service.PlanMarketOfferAsync(new(
                    SelectedSlot.Slot, approach, expiry, loaded,
                    windowStart, windowEnd,
                    pickup, unloading, post, tight));
            }
            else
            {
                result = await _service.PlanActiveDeliveryAsync(new(
                    SelectedSlot.Slot, loaded, windowStart, windowEnd,
                    unloading, post, tight));
            }
            Present(result);
        }
        catch (Exception exception)
        {
            _diagnosticError?.Invoke("PLANNER_CALCULATION_FAILED", exception);
            ValidationMessage = Localization.UiStrings.Get("PlannerError_CalculationFailed");
        }
    }

    private bool InputsAreValid() =>
        ParseCommon(out _, out _, out _, out _) &&
        (!IsMarketOffer ||
         TryParseDuration(DriveToPickup, out _) &&
         TryParseDuration(OfferExpiresIn, out _) &&
         TryParseDuration(PickupWork, out _));

    private bool ParseCommon(
        out int loaded, out int unloading, out int post, out int tight)
    {
        var loadedValid = TryParseDuration(LoadedDrive, out loaded);
        var unloadingValid = TryParseDuration(UnloadingWork, out unloading);
        var postValid = TryParseDuration(PostDeliveryWork, out post);
        var tightValid = TryParseDuration(TightMargin, out tight);
        return loadedValid && unloadingValid && postValid && tightValid;
    }

    private void Present(DeliveryPlanResult result)
    {
        _requiresRecalculation = false;
        _resultIdentity = result.SnapshotIdentity;
        HasResult = true;
        var calendar = new GameCalendarResolver(new(result.WeekEpochOffsetDays));
        CurrentTimeText = Format(calendar, result.StartGameMinute);
        OfferExpiryText = result.OfferExpiresAtGameMinuteExclusive is { } expiry
            ? Format(calendar, expiry)
            : Localization.UiStrings.Get("Planner_NotApplicable");
        DeliveryWindowText =
            $"{Localization.UiStrings.Get("PlannerTime_FromPrefix")}: " +
            $"{Format(calendar, result.DeliveryWindowStartGameMinute)}\n" +
            $"{Localization.UiStrings.Get("PlannerTime_ToPrefix")}: " +
            $"{Format(calendar, result.DeliveryWindowEndGameMinuteExclusive)}";
        PickupText = Format(calendar, result.PickupCompletedAtGameMinute);
        ArrivalText = Format(calendar, result.ArrivedAtDeliveryGameMinute);
        CompletionText = Format(calendar, result.DeliveryCompletedAtGameMinute);
        MarginText = result.DeliveryCompletedAtGameMinute is null
            ? "—"
            : FormatSigned(result.MarginMinutes);
        (StatusText, StatusForeground) = Verdict(result);

        Segments.Clear();
        var number = 1;
        foreach (var segment in result.Segments)
        {
            Segments.Add(new(
                number++,
                Format(calendar, segment.StartGameMinute),
                Format(calendar, segment.EndGameMinute),
                segment.DrivingSlot is null
                    ? Localization.UiStrings.Get("PlannerVehicle_Parked")
                    : Localization.UiStrings.Get("Activity_Driving"),
                Activity(segment.Slot1Activity),
                segment.Slot2Activity is { } slot2 ? Activity(slot2) : "—",
                FormatDuration(segment.DurationMinutes),
                segment.RegulatoryReason?.ToString() ?? Phase(segment.Phase)));
        }

        Warnings.Clear();
        foreach (var warning in result.Warnings)
            Warnings.Add(warning.Context is null
                ? WarningText(warning.Code)
                : $"{WarningText(warning.Code)}: {warning.Context}");
        if (result.FailureReason != DeliveryPlanFailureReason.None)
            Warnings.Add(FailureText(result.FailureReason));
        Summary.Clear();
        Summary.Add(
            $"{(result.Verdict == DeliveryPlanVerdict.Reject ? "✕" : "✓")} " +
            (result.Verdict == DeliveryPlanVerdict.Reject
                ? Localization.UiStrings.Get("PlannerSummary_PlanRejected")
                : Localization.UiStrings.Get("PlannerSummary_PlanFitsWindow")));
        Summary.Add($"✓ {Localization.UiStrings.Get("PlannerSummary_BothCardsIncluded")}");
        if (result.DeliveryCompletedAtGameMinute is not null)
            Summary.Add(
                $"✓ {Localization.UiStrings.Format("PlannerSummary_MarginFormat", MarginText)}");
    }

    private static (string Text, string Color) Verdict(DeliveryPlanResult result) =>
        result.FailureReason switch
        {
            DeliveryPlanFailureReason.OfferExpired =>
                (Localization.UiStrings.Get("PlannerVerdict_PickupMissed"), "#B3261E"),
            DeliveryPlanFailureReason.DeliveryWindowMissed =>
                (Localization.UiStrings.Get("PlannerVerdict_DeliveryMissed"), "#B3261E"),
            DeliveryPlanFailureReason.NoLegalContinuation or
            DeliveryPlanFailureReason.CalculationLimitReached =>
                (Localization.UiStrings.Get("PlannerVerdict_Reject"), "#B3261E"),
            DeliveryPlanFailureReason.InsufficientData or
            DeliveryPlanFailureReason.StaleSnapshot =>
                (Localization.UiStrings.Get("PlannerVerdict_UnreliableData"), "#B3261E"),
            _ when result.Verdict == DeliveryPlanVerdict.Take =>
                (Localization.UiStrings.Get("PlannerVerdict_Take"), "#258A4B"),
            _ when result.Verdict == DeliveryPlanVerdict.Tight =>
                (Localization.UiStrings.Get("PlannerVerdict_Tight"), "#C67A00"),
            _ => (Localization.UiStrings.Get("PlannerVerdict_Reject"), "#B3261E")
        };

    private void InputChanged()
    {
        InvalidateResult(Localization.UiStrings.Get("PlannerStatus_InputChanged"));
        RefreshInputPreviews();
        ValidationMessage = string.Empty;
        InputOriginText = Localization.UiStrings.Get("PlannerInput_UserValuesAutosave");
        SaveInputState();
        OnPropertyChanged(nameof(CanCalculate));
        foreach (var property in DurationPropertyNames)
            OnPropertyChanged(property);
        _calculateCommand.RaiseCanExecuteChanged();
    }

    private void RefreshInputPreviews()
    {
        if (_previewCalendar is null || _previewNow is null)
            return;
        if (!IsMarketOffer)
            OfferExpiryText = Localization.UiStrings.Get("Planner_NotApplicable");
        else if (TryParseDuration(OfferExpiresIn, out var expires))
            OfferExpiryText = Format(_previewCalendar, checked(_previewNow.Value + expires));
        var resolvedStart = _previewCalendar.ResolveNext(
            WindowStartDay.Value, WindowStartHour, WindowStartMinute, new GameTime(_previewNow.Value));
        var resolvedEnd = _previewCalendar.ResolveNext(
            WindowEndDay.Value, WindowEndHour, WindowEndMinute, resolvedStart.GameTime.AddMinutes(1));
        DeliveryWindowText =
            $"Od: {GameCalendarFormatter.FormatCompact(resolvedStart)}\n" +
            $"Do: {GameCalendarFormatter.FormatCompact(resolvedEnd)}";
    }

    private void InvalidateResult(string message)
    {
        if (!HasResult)
            return;
        _resultIdentity = null;
        _requiresRecalculation = true;
        HasResult = false;
        StatusText = message;
        StatusForeground = "#5F6874";
        PickupText = ArrivalText = CompletionText = MarginText = "—";
        Segments.Clear();
        Warnings.Clear();
        Summary.Clear();
    }

    private void SetInput<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Set(ref field, value, name))
            InputChanged();
    }

    public string Error => string.Empty;

    public string this[string columnName] =>
        IsDurationProperty(columnName) &&
        !TryParseDuration(GetDurationValue(columnName), out _)
            ? Localization.UiStrings.Format(
                "PlannerValidation_DurationFormat",
                DurationLabel(columnName))
            : string.Empty;

    private static readonly string[] DurationPropertyNames =
    [
        nameof(DriveToPickup), nameof(OfferExpiresIn), nameof(PickupWork),
        nameof(LoadedDrive), nameof(UnloadingWork), nameof(PostDeliveryWork),
        nameof(TightMargin)
    ];

    private static bool IsDurationProperty(string propertyName) =>
        DurationPropertyNames.Contains(propertyName, StringComparer.Ordinal);

    private string GetDurationValue(string propertyName) => propertyName switch
    {
        nameof(DriveToPickup) => DriveToPickup,
        nameof(OfferExpiresIn) => OfferExpiresIn,
        nameof(PickupWork) => PickupWork,
        nameof(LoadedDrive) => LoadedDrive,
        nameof(UnloadingWork) => UnloadingWork,
        nameof(PostDeliveryWork) => PostDeliveryWork,
        nameof(TightMargin) => TightMargin,
        _ => string.Empty
    };

    private static string DurationLabel(string propertyName) => propertyName switch
    {
        nameof(DriveToPickup) => Localization.UiStrings.Get("PlannerPhase_DriveToPickup"),
        nameof(OfferExpiresIn) => Localization.UiStrings.Get("PlannerField_OfferExpiresIn"),
        nameof(PickupWork) => Localization.UiStrings.Get("PlannerField_Pickup"),
        nameof(LoadedDrive) => Localization.UiStrings.Get("PlannerPhase_DriveWithCargo"),
        nameof(UnloadingWork) => Localization.UiStrings.Get("PlannerPhase_Unloading"),
        nameof(PostDeliveryWork) => Localization.UiStrings.Get("PlannerPhase_PostDeliveryWork"),
        nameof(TightMargin) => Localization.UiStrings.Get("PlannerField_TightThreshold"),
        _ => Localization.UiStrings.Get("PlannerValidation_TimeLabel")
    };

    private string FirstValidationError()
    {
        foreach (var propertyName in DurationPropertyNames)
        {
            if (!IsMarketOffer &&
                propertyName is nameof(DriveToPickup) or nameof(OfferExpiresIn) or nameof(PickupWork))
                continue;
            var error = this[propertyName];
            if (error.Length > 0)
                return error;
        }
        return Localization.UiStrings.Get("PlannerValidation_CorrectTimeField");
    }

    private void SetDurationPreset(string? parameter)
    {
        if (!TrySplitCommandParameter(parameter, out var propertyName, out var minutes))
            return;
        SetDurationValue(propertyName, FormatDuration(minutes));
    }

    private void AdjustDuration(string? parameter)
    {
        if (!TrySplitCommandParameter(parameter, out var propertyName, out var delta) ||
            !TryParseDuration(GetDurationValue(propertyName), out var current))
            return;
        var adjusted = Math.Max(0, (long)current + delta);
        if (adjusted > int.MaxValue)
            return;
        SetDurationValue(propertyName, FormatDuration((int)adjusted));
    }

    private static bool TrySplitCommandParameter(
        string? parameter, out string propertyName, out int minutes)
    {
        propertyName = string.Empty;
        minutes = 0;
        var parts = parameter?.Split('|');
        if (parts is not { Length: 2 } || !IsDurationProperty(parts[0]) ||
            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out minutes))
            return false;
        propertyName = parts[0];
        return true;
    }

    private void SetDurationValue(string propertyName, string value)
    {
        switch (propertyName)
        {
            case nameof(DriveToPickup): DriveToPickup = value; break;
            case nameof(OfferExpiresIn): OfferExpiresIn = value; break;
            case nameof(PickupWork): PickupWork = value; break;
            case nameof(LoadedDrive): LoadedDrive = value; break;
            case nameof(UnloadingWork): UnloadingWork = value; break;
            case nameof(PostDeliveryWork): PostDeliveryWork = value; break;
            case nameof(TightMargin): TightMargin = value; break;
        }
    }

    private void RestoreInputState()
    {
        if (_inputStateStore is null)
            return;
        try
        {
            var state = _inputStateStore.Load();
            if (state is null)
                return;
            _selectedMode = state.IsMarketOffer ? Modes[0] : Modes[1];
            _selectedSlot = Slots.FirstOrDefault(option => option.Slot == state.SelectedSlot) ?? Slots[0];
            _driveToPickup = state.DriveToPickup;
            _offerExpiresIn = state.OfferExpiresIn;
            _pickupWork = state.PickupWork;
            _loadedDrive = state.LoadedDrive;
            _windowStartDay = Weekdays.FirstOrDefault(option => option.Value == state.WindowStartDay) ?? Weekdays[3];
            _windowStartHour = Math.Clamp(state.WindowStartHour, 0, 23);
            _windowStartMinute = Math.Clamp(state.WindowStartMinute, 0, 59);
            _windowEndDay = Weekdays.FirstOrDefault(option => option.Value == state.WindowEndDay) ?? Weekdays[5];
            _windowEndHour = Math.Clamp(state.WindowEndHour, 0, 23);
            _windowEndMinute = Math.Clamp(state.WindowEndMinute, 0, 59);
            _unloadingWork = state.UnloadingWork;
            _postDeliveryWork = state.PostDeliveryWork;
            _tightMargin = state.TightMargin;
            _inputOriginText = Localization.UiStrings.Get("PlannerInput_RestoredValues");
        }
        catch (Exception exception)
        {
            _diagnosticError?.Invoke("PLANNER_INPUT_RESTORE_FAILED", exception);
            _inputOriginText = Localization.UiStrings.Get("PlannerInput_RestoreFailed");
        }
    }

    public void SaveInputState()
    {
        if (_inputStateStore is null)
            return;
        try
        {
            _inputStateStore.Save(new(
                IsMarketOffer, SelectedSlot.Slot,
                DriveToPickup, OfferExpiresIn, PickupWork, LoadedDrive,
                WindowStartDay.Value, WindowStartHour, WindowStartMinute,
                WindowEndDay.Value, WindowEndHour, WindowEndMinute,
                UnloadingWork, PostDeliveryWork, TightMargin));
        }
        catch (Exception exception)
        {
            _diagnosticError?.Invoke("PLANNER_INPUT_SAVE_FAILED", exception);
            InputOriginText = Localization.UiStrings.Get("PlannerInput_SaveFailed");
        }
    }

    private static string FailureText(DeliveryPlanFailureReason reason) => reason switch
    {
        DeliveryPlanFailureReason.OfferExpired => Localization.UiStrings.Get("PlannerFailure_OfferExpired"),
        DeliveryPlanFailureReason.DeliveryWindowMissed => Localization.UiStrings.Get("PlannerFailure_DeliveryWindowMissed"),
        DeliveryPlanFailureReason.NoLegalContinuation => Localization.UiStrings.Get("PlannerFailure_NoLegalContinuation"),
        DeliveryPlanFailureReason.InsufficientData => Localization.UiStrings.Get("PlannerFailure_InsufficientData"),
        DeliveryPlanFailureReason.StaleSnapshot =>
            Localization.UiStrings.Get("PlannerFailure_StaleSnapshot"),
        DeliveryPlanFailureReason.CalculationLimitReached =>
            Localization.UiStrings.Get("PlannerFailure_CalculationLimitReached"),
        DeliveryPlanFailureReason.NotImplemented =>
            Localization.UiStrings.Get("PlannerFailure_NotImplemented"),
        DeliveryPlanFailureReason.None => string.Empty,
        _ => throw new ArgumentOutOfRangeException(nameof(reason))
    };
    private static string Phase(DeliveryPlanPhase phase) => phase switch
    {
        DeliveryPlanPhase.DriveToPickup => Localization.UiStrings.Get("PlannerPhase_DriveToPickup"),
        DeliveryPlanPhase.Pickup => Localization.UiStrings.Get("PlannerPhase_Pickup"),
        DeliveryPlanPhase.DriveWithCargo => Localization.UiStrings.Get("PlannerPhase_DriveWithCargo"),
        DeliveryPlanPhase.WaitForDeliveryWindow => Localization.UiStrings.Get("PlannerPhase_WaitForDeliveryWindow"),
        DeliveryPlanPhase.Unloading => Localization.UiStrings.Get("PlannerPhase_Unloading"),
        DeliveryPlanPhase.PostDeliveryWork => Localization.UiStrings.Get("PlannerPhase_PostDeliveryWork"),
        DeliveryPlanPhase.RegulatoryInterruption =>
            Localization.UiStrings.Get("PlannerPhase_RegulatoryInterruption"),
        _ => throw new ArgumentOutOfRangeException(nameof(phase))
    };
    private static string Activity(DriverActivity activity) => activity switch
    {
        DriverActivity.Driving => Localization.UiStrings.Get("Activity_Driving"),
        DriverActivity.OtherWork => Localization.UiStrings.Get("Activity_OtherWork"),
        DriverActivity.Availability => Localization.UiStrings.Get("Activity_Availability"),
        DriverActivity.BreakOrRest => Localization.UiStrings.Get("Activity_Rest"),
        DriverActivity.OutOfScope => "OUT",
        DriverActivity.Unknown => Localization.UiStrings.Get("Activity_Unknown"),
        _ => throw new ArgumentOutOfRangeException(nameof(activity))
    };
    private static string WarningText(JourneyPlanWarningCode code) => code switch
    {
        JourneyPlanWarningCode.IncompleteHistory =>
            Localization.UiStrings.Get("PlannerWarning_IncompleteHistory"),
        JourneyPlanWarningCode.LastSavedState =>
            Localization.UiStrings.Get("PlannerWarning_LastSavedState"),
        JourneyPlanWarningCode.CompensationModelLimited =>
            Localization.UiStrings.Get("PlannerWarning_CompensationModelLimited"),
        JourneyPlanWarningCode.ReducedWeeklyRestUnavailable =>
            Localization.UiStrings.Get("PlannerWarning_ReducedWeeklyRestUnavailable"),
        JourneyPlanWarningCode.MultiManningPlanningUnsupported =>
            Localization.UiStrings.Get("PlannerWarning_MultiManningPlanningUnsupported"),
        JourneyPlanWarningCode.RegulatoryExceptionUsed =>
            Localization.UiStrings.Get("PlannerWarning_RegulatoryExceptionUsed"),
        _ => throw new ArgumentOutOfRangeException(nameof(code))
    };
    private static string Format(GameCalendarResolver calendar, long? minute) =>
        minute is null ? "—" : GameCalendarFormatter.FormatCompact(
            calendar.Resolve(new GameTime(minute.Value)));
    private static string FormatDuration(int minutes) =>
        $"{minutes / 60:00}:{minutes % 60:00}";
    private static string FormatSigned(int minutes) =>
        $"{(minutes < 0 ? "−" : "+")}{FormatDuration(Math.Abs(minutes))}";

    public event PropertyChangedEventHandler? PropertyChanged;
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
    private void OnPropertyChanged(string? name) =>
        PropertyChanged?.Invoke(this, new(name));

    private sealed class AsyncCommand(Func<Task> execute, Func<bool> canExecute) : ICommand
    {
        private bool _running;
        public bool CanExecute(object? parameter) => !_running && canExecute();
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

    private sealed class RelayCommand<T>(Action<T?> execute) : ICommand
    {
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute(parameter is T typed ? typed : default);
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}

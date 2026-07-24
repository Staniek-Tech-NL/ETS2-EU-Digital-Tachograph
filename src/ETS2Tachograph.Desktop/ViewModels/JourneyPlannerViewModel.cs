using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ETS2Tachograph.Application.Services;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.RuleEngine.JourneyPlanning;

namespace ETS2Tachograph.Desktop;

public sealed record JourneyPlannerSlotOption(int Slot, string Name);
public sealed record JourneyPlanSegmentRow(
    string Type,
    string Start,
    string End,
    string Duration,
    string Reason,
    string Activity,
    string Warning);

public sealed class JourneyPlannerViewModel : INotifyPropertyChanged
{
    private readonly IJourneyPlannerService _service;
    private JourneyPlannerSlotOption _selectedSlot;
    private string _remainingDrive = "01:00";
    private string _deliveryWindow = "02:00";
    private string _operationalBuffer = "00:00";
    private string _validationMessage = string.Empty;
    private string _statusText = "Wprowadź dane i oblicz plan.";
    private string _confidenceText = "—";
    private string _arrivalText = "—";
    private string _completionText = "—";
    private string _marginText = "—";
    private bool _hasResult;
    private JourneyPlanSnapshotIdentity? _resultIdentity;

    public JourneyPlannerViewModel(IJourneyPlannerService service)
    {
        _service = service;
        Slots =
        [
            new(1, "S1 — kierowca"),
            new(2, "S2 — zmiennik")
        ];
        _selectedSlot = Slots[0];
        CalculateCommand = new AsyncCommand(CalculateAsync);
    }

    public IReadOnlyList<JourneyPlannerSlotOption> Slots { get; }
    public ObservableCollection<JourneyPlanSegmentRow> Segments { get; } = [];
    public ObservableCollection<string> Warnings { get; } = [];
    public ICommand CalculateCommand { get; }
    public JourneyPlannerSlotOption SelectedSlot
    {
        get => _selectedSlot;
        set
        {
            if (Set(ref _selectedSlot, value))
            {
                InvalidateResult("Wybrano inną kartę. Oblicz plan ponownie.");
            }
        }
    }
    public string RemainingDrive { get => _remainingDrive; set => Set(ref _remainingDrive, value); }
    public string DeliveryWindow { get => _deliveryWindow; set => Set(ref _deliveryWindow, value); }
    public string OperationalBuffer { get => _operationalBuffer; set => Set(ref _operationalBuffer, value); }
    public string ValidationMessage { get => _validationMessage; private set => Set(ref _validationMessage, value); }
    public string StatusText { get => _statusText; private set => Set(ref _statusText, value); }
    public string ConfidenceText { get => _confidenceText; private set => Set(ref _confidenceText, value); }
    public string ArrivalText { get => _arrivalText; private set => Set(ref _arrivalText, value); }
    public string CompletionText { get => _completionText; private set => Set(ref _completionText, value); }
    public string MarginText { get => _marginText; private set => Set(ref _marginText, value); }
    public bool HasResult { get => _hasResult; private set => Set(ref _hasResult, value); }

    public void ObserveStateChange()
    {
        if (_resultIdentity is not null && !_service.IsCurrent(_resultIdentity))
        {
            InvalidateResult("Stan kierowcy zmienił się. Oblicz plan ponownie.");
        }
    }

    public static bool TryParseDuration(string? text, out int minutes)
    {
        minutes = 0;
        var parts = text?.Split(':');
        if (parts is not { Length: 2 } ||
            !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var hours) ||
            !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var minutePart) ||
            hours < 0 || minutePart is < 0 or > 59)
        {
            return false;
        }
        try
        {
            minutes = checked((hours * 60) + minutePart);
            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    public async Task CalculateAsync()
    {
        ValidationMessage = string.Empty;
        if (!TryParseDuration(RemainingDrive, out var drive) ||
            !TryParseDuration(DeliveryWindow, out var delivery) ||
            !TryParseDuration(OperationalBuffer, out var buffer))
        {
            ValidationMessage = "Podaj czas w formacie HH:MM; minuty muszą mieścić się w zakresie 00–59.";
            return;
        }

        try
        {
            var result = await _service.PlanAsync(new(
                SelectedSlot.Slot,
                drive,
                delivery,
                buffer));
            Present(result);
        }
        catch (Exception exception)
        {
            ValidationMessage = $"Nie można obliczyć planu: {exception.Message}";
        }
    }

    private void Present(JourneyPlanResult result)
    {
        _resultIdentity = result.SnapshotIdentity;
        HasResult = true;
        StatusText = Status(result.Status);
        ConfidenceText = Confidence(result.Confidence);
        ArrivalText = FormatGameMinute(result.EarliestArrivalGameMinute);
        CompletionText = FormatGameMinute(result.EarliestCompletionGameMinute);
        MarginText = result.EarliestCompletionGameMinute is null
            ? "—"
            : FormatSigned(result.MarginMinutes);
        Segments.Clear();
        foreach (var segment in result.Segments)
        {
            Segments.Add(new(
                segment.Type.ToString(),
                FormatGameMinute(segment.StartGameMinute),
                FormatGameMinute(segment.EndGameMinute),
                FormatDuration(segment.DurationMinutes),
                segment.Reason.ToString(),
                segment.RegulatoryActivity.ToString(),
                segment.WarningCode ?? string.Empty));
        }
        Warnings.Clear();
        foreach (var warning in result.Warnings)
        {
            Warnings.Add($"{warning.Code}: {warning.Context}".TrimEnd(' ', ':'));
        }
    }

    private void InvalidateResult(string message)
    {
        if (!HasResult)
        {
            return;
        }
        _resultIdentity = null;
        HasResult = false;
        StatusText = message;
        ConfidenceText = ArrivalText = CompletionText = MarginText = "—";
        Segments.Clear();
        Warnings.Clear();
    }

    private static string Status(JourneyPlanStatus status) => status switch
    {
        JourneyPlanStatus.MeetsDeadline => "Plan mieści się w terminie.",
        JourneyPlanStatus.MissesDeadline => "Legalny plan kończy się po terminie.",
        JourneyPlanStatus.BlockedByGap => "Planowanie blokuje nierozliczona luka.",
        JourneyPlanStatus.InsufficientData => "Brak danych do obliczenia planu.",
        JourneyPlanStatus.StaleSnapshot => "Stan zmienił się podczas obliczenia. Spróbuj ponownie.",
        JourneyPlanStatus.UnsupportedScenario => "Scenariusz nie jest obsługiwany w MVP.",
        JourneyPlanStatus.NoLegalContinuation => "Brak legalnej kontynuacji w horyzoncie.",
        _ => "Osiągnięto limit obliczeń."
    };

    private static string Confidence(JourneyPlanConfidence confidence) => confidence switch
    {
        JourneyPlanConfidence.VerifiedByCurrentRuleModel => "Potwierdzone przez bieżący model",
        JourneyPlanConfidence.LimitedByCompensationModel => "Ograniczone przez model rekompensat",
        JourneyPlanConfidence.BasedOnIncompleteHistory => "Na podstawie niepełnej historii",
        _ => "Na podstawie ostatniego zapisanego stanu"
    };

    private static string FormatGameMinute(long? minute) =>
        minute is null ? "—" : GameClockFormatter.Format(new GameTime(minute.Value));
    private static string FormatDuration(int minutes) =>
        string.Create(CultureInfo.InvariantCulture, $"{minutes / 60:00}:{minutes % 60:00}");
    private static string FormatSigned(int minutes) =>
        $"{(minutes < 0 ? "−" : "+")}{FormatDuration(Math.Abs(minutes))}";

    public event PropertyChangedEventHandler? PropertyChanged;
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new(name));
        return true;
    }

    private sealed class AsyncCommand(Func<Task> execute) : ICommand
    {
        private bool _running;
        public bool CanExecute(object? parameter) => !_running;
        public async void Execute(object? parameter)
        {
            if (_running) return;
            _running = true; CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            try { await execute(); }
            finally { _running = false; CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
        }
        public event EventHandler? CanExecuteChanged;
    }
}

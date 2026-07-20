using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Settings;
using ETS2Tachograph.Core.Telemetry;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.RuleEngine;

namespace ETS2Tachograph.Engine;

public enum TachographSlot
{
    Driver = 1,
    CoDriver = 2
}

public sealed record CrewTachographSnapshot
{
    public TelemetryFrame? Frame { get; init; }
    public string? DriverCardId { get; init; }
    public string? CoDriverCardId { get; init; }
    public TachographSnapshot? Driver { get; init; }
    public TachographSnapshot? CoDriver { get; init; }
    public IReadOnlyList<CardSnapshotUpdate> DetachedCardUpdates { get; init; } = [];
    public bool MultiManning => DriverCardId is not null && CoDriverCardId is not null;
    public bool CoDriverMovingBreakActive { get; init; }
    public bool CoDriverMovingBreakCompleted { get; init; }
    public long CoDriverMovingBreakElapsedMinutes { get; init; }
    public long CoDriverMovingBreakRemainingMinutes =>
        Math.Max(0, CrewTachographEngine.MovingBreakMinutes - CoDriverMovingBreakElapsedMinutes);
    public bool ManualEntryRequired =>
        Driver?.RequiredManualEntryGap is not null || CoDriver?.RequiredManualEntryGap is not null;
    public bool DrivingLockedByManualEntry => ManualEntryRequired;
}

public sealed record EjectedCardResult(string CardId, TachographSnapshot Snapshot);
public sealed record InsertedCardResult(string CardId, TachographSnapshot Snapshot);
public sealed record CardSnapshotUpdate(string CardId, TachographSnapshot Snapshot);

/// <summary>
/// Coordinates two card-owned tachograph engines. Slot 1 receives vehicle movement;
/// slot 2 records availability, except for an explicitly requested 45-minute crew break.
/// </summary>
public sealed class CrewTachographEngine
{
    public const int MovingBreakMinutes = 45;

    private readonly TachographSettings _settings;
    private readonly RegulationOptions _regulationOptions;
    private readonly Dictionary<string, TachographEngine> _engines =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TachographSlot> _removedCards =
        new(StringComparer.OrdinalIgnoreCase);
    private DriverActivity _coDriverStoppedActivity = DriverActivity.Availability;
    private bool _coDriverMovingBreakActive;
    private bool _coDriverMovingBreakCompleted;
    private GameTime? _coDriverMovingBreakStartedAt;
    private GameTime? _coDriverRestStartedAt;
    private TelemetryFrame? _lastFrame;
    private GameTime? _lastValidGameTime;

    public CrewTachographEngine(
        TachographSettings? settings = null,
        RegulationOptions? regulationOptions = null)
    {
        _settings = settings ?? new TachographSettings();
        _regulationOptions = regulationOptions ?? new RegulationOptions();
        Current = new CrewTachographSnapshot();
    }

    public CrewTachographSnapshot Current { get; private set; }
    public string? DriverCardId { get; private set; }
    public string? CoDriverCardId { get; private set; }
    public bool VehicleMoving =>
        _lastFrame is not null && Math.Abs(_lastFrame.SpeedKph) > _settings.DrivingSpeedThresholdKph;
    public IReadOnlyCollection<string> RegisteredCardIds => _engines.Keys;
    public IReadOnlyCollection<string> RemovedCardIds => _removedCards.Keys;

    public TachographEngine RegisterCard(
        string cardId,
        IEnumerable<RestoredActivitySession>? restoredSessions = null)
    {
        if (string.IsNullOrWhiteSpace(cardId))
            throw new ArgumentException("Driver card id is required.", nameof(cardId));
        if (_engines.TryGetValue(cardId, out var existing))
            return existing;

        var engine = new TachographEngine(
            cardId,
            _settings,
            regulationOptions: _regulationOptions);
        if (restoredSessions is not null)
            engine.RestoreSessions(restoredSessions);
        _engines.Add(cardId, engine);
        if (engine.History.OpenCardRemovedGap is { } openGap)
            _removedCards[cardId] = (TachographSlot)openGap.Slot;
        return engine;
    }

    public TachographEngine? GetEngine(string? cardId) =>
        cardId is not null && _engines.TryGetValue(cardId, out var engine) ? engine : null;

    public TachographEngine? GetEngine(TachographSlot slot) =>
        GetEngine(slot == TachographSlot.Driver ? DriverCardId : CoDriverCardId);

    public InsertedCardResult InsertCard(TachographSlot slot, string cardId)
    {
        var engine = RegisterCard(cardId);
        var occupiedCard = slot == TachographSlot.Driver ? DriverCardId : CoDriverCardId;
        if (occupiedCard is not null && !string.Equals(occupiedCard, cardId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Slot {(int)slot} already contains a card.");

        var otherCard = slot == TachographSlot.Driver ? CoDriverCardId : DriverCardId;
        if (string.Equals(otherCard, cardId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The same driver card cannot be inserted into both slots.");

        var snapshot = _lastValidGameTime is null
            ? engine.Current
            : engine.CloseCardRemovedGap(_lastValidGameTime.Value);
        _removedCards.Remove(cardId);

        if (slot == TachographSlot.Driver)
        {
            DriverCardId = cardId;
            engine.SetManualActivity(_settings.ActivityAfterStop);
        }
        else
        {
            CoDriverCardId = cardId;
            _coDriverStoppedActivity = DriverActivity.Availability;
            _coDriverMovingBreakCompleted = false;
            _coDriverRestStartedAt = null;
            engine.SetManualActivity(DriverActivity.Availability);
        }

        UpdateMultiManning();
        RebuildCurrent();
        return new InsertedCardResult(cardId, snapshot);
    }

    public EjectedCardResult EjectCard(TachographSlot slot, DateTimeOffset recordedAtUtc)
    {
        EnsureManualEntryResolved(slot);
        if (VehicleMoving)
            throw new InvalidOperationException("Cards cannot be removed while the vehicle is moving.");

        var cardId = slot == TachographSlot.Driver ? DriverCardId : CoDriverCardId;
        if (cardId is null)
            throw new InvalidOperationException($"Slot {(int)slot} is empty.");
        if (_lastValidGameTime is null)
            throw new InvalidOperationException("The card cannot be removed before the first valid game-time frame.");

        var engine = _engines[cardId];
        var snapshot = engine.OpenCardRemovedGap(
            _lastValidGameTime.Value,
            (int)slot,
            recordedAtUtc);
        _removedCards[cardId] = slot;
        if (slot == TachographSlot.Driver)
            DriverCardId = null;
        else
        {
            CoDriverCardId = null;
            CancelCoDriverMovingBreak();
            _coDriverRestStartedAt = null;
        }

        UpdateMultiManning();
        RebuildCurrent();
        return new EjectedCardResult(cardId, snapshot);
    }

    public void SetManualActivity(TachographSlot slot, DriverActivity activity)
    {
        EnsureManualEntryResolved(slot);
        var engine = GetEngine(slot) ?? throw new InvalidOperationException($"Slot {(int)slot} is empty.");
        if (VehicleMoving)
        {
            if (slot == TachographSlot.Driver)
                throw new InvalidOperationException("The driver's activity cannot be changed while driving.");
            if (activity != DriverActivity.Availability)
                throw new InvalidOperationException("The co-driver may only use availability while moving, or start a dedicated 45-minute break.");
        }

        if (slot == TachographSlot.CoDriver)
        {
            CancelCoDriverMovingBreak();
            _coDriverStoppedActivity = activity;
            _coDriverRestStartedAt = activity == DriverActivity.BreakOrRest
                ? _lastFrame?.GameTime
                : null;
        }
        engine.SetManualActivity(activity);
        RebuildCurrent();
    }

    public void SetOutMode(TachographSlot slot, bool enabled)
    {
        EnsureManualEntryResolved(slot);
        var engine = GetEngine(slot) ?? throw new InvalidOperationException($"Slot {(int)slot} is empty.");
        engine.SetOutMode(enabled);
        RebuildCurrent();
    }

    public void SetFerryMode(TachographSlot slot, bool enabled)
    {
        EnsureManualEntryResolved(slot);
        var engine = GetEngine(slot) ?? throw new InvalidOperationException($"Slot {(int)slot} is empty.");
        engine.SetFerryMode(enabled);
        RebuildCurrent();
    }

    public void StartCoDriverMovingBreak()
    {
        EnsureManualEntryResolved(TachographSlot.CoDriver);
        var engine = GetEngine(TachographSlot.CoDriver) ??
            throw new InvalidOperationException("Slot 2 is empty.");
        if (!VehicleMoving || _lastFrame is null)
            throw new InvalidOperationException("The special crew break is available only while another driver is driving.");
        if (_coDriverMovingBreakActive)
            return;

        _coDriverMovingBreakActive = true;
        _coDriverMovingBreakCompleted = false;
        _coDriverMovingBreakStartedAt = _lastFrame.GameTime;
        _coDriverRestStartedAt = _lastFrame.GameTime;
        _coDriverStoppedActivity = DriverActivity.BreakOrRest;
        engine.SetManualActivity(DriverActivity.BreakOrRest);
        RebuildCurrent();
    }

    public void ApplyManualEntryResolution(
        string cardId,
        ActivityGap resolvedGap,
        IReadOnlyList<ActivityRecord> segments)
    {
        var cardEngine = GetEngine(cardId) ??
            throw new InvalidOperationException("The resolved card is not registered.");
        cardEngine.ApplyManualEntryResolution(resolvedGap, segments);
        RebuildCurrent();
    }

    public CrewTachographSnapshot ProcessFrame(TelemetryFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        _lastFrame = frame;
        if (!frame.GamePaused)
            _lastValidGameTime = frame.GameTime;
        UpdateMultiManning();

        TachographSnapshot? driver = null;
        TachographSnapshot? coDriver = null;
        var detachedCardUpdates = new List<CardSnapshotUpdate>();
        if (GetEngine(TachographSlot.Driver) is { } driverEngine)
            driver = driverEngine.ProcessFrame(frame);

        if (GetEngine(TachographSlot.CoDriver) is { } coDriverEngine)
        {
            var coDriverActivity = ResolveCoDriverActivity(frame);
            coDriverEngine.SetManualActivity(coDriverActivity);
            coDriver = coDriverEngine.ProcessFrame(
                frame with { SpeedKph = 0 },
                vehicleSpeedKph: frame.SpeedKph,
                slot: (int)TachographSlot.CoDriver);
        }

        foreach (var (cardId, removedFromSlot) in _removedCards.ToList())
        {
            var detached = _engines[cardId].ObserveRemovedCard(frame, (int)removedFromSlot);
            detachedCardUpdates.Add(new CardSnapshotUpdate(cardId, detached));
        }

        Current = BuildSnapshot(frame, driver, coDriver, detachedCardUpdates);
        return Current;
    }

    private DriverActivity ResolveCoDriverActivity(TelemetryFrame frame)
    {
        var vehicleMoving = Math.Abs(frame.SpeedKph) > _settings.DrivingSpeedThresholdKph;

        if (!vehicleMoving && _coDriverStoppedActivity == DriverActivity.BreakOrRest)
            _coDriverRestStartedAt ??= frame.GameTime;

        // A break selected in slot 2 while stopped must not be overwritten when slot 1
        // begins driving. Continue it as the permitted 45-minute crew break.
        if (vehicleMoving && !_coDriverMovingBreakActive &&
            _coDriverStoppedActivity == DriverActivity.BreakOrRest)
        {
            _coDriverMovingBreakActive = true;
            _coDriverMovingBreakCompleted = false;
            _coDriverMovingBreakStartedAt = _coDriverRestStartedAt ?? frame.GameTime;
        }

        if (_coDriverMovingBreakActive)
        {
            _coDriverMovingBreakStartedAt ??= frame.GameTime;
            // The activity processor closes the previous minute when the next minute arrives.
            // Keep the break active at elapsed == 45 so the complete 45th minute is persisted.
            if (MovingBreakElapsed(frame.GameTime) <= MovingBreakMinutes)
                return DriverActivity.BreakOrRest;

            CancelCoDriverMovingBreak(completed: true);
            _coDriverStoppedActivity = DriverActivity.Availability;
            _coDriverRestStartedAt = null;
        }

        return vehicleMoving
            ? DriverActivity.Availability
            : _coDriverStoppedActivity;
    }

    private long MovingBreakElapsed(GameTime now) => _coDriverMovingBreakStartedAt is null
        ? 0
        : Math.Max(0, now - _coDriverMovingBreakStartedAt.Value);

    private void CancelCoDriverMovingBreak(bool completed = false)
    {
        _coDriverMovingBreakActive = false;
        _coDriverMovingBreakStartedAt = null;
        _coDriverMovingBreakCompleted = completed;
    }

    private void UpdateMultiManning()
    {
        var active = DriverCardId is not null && CoDriverCardId is not null;
        foreach (var pair in _engines)
        {
            var inserted = string.Equals(pair.Key, DriverCardId, StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(pair.Key, CoDriverCardId, StringComparison.OrdinalIgnoreCase);
            pair.Value.SetMultiManning(active && inserted);
        }
    }

    private void EnsureManualEntryResolved(TachographSlot slot)
    {
        if (GetEngine(slot)?.Current.RequiredManualEntryGap is not null)
            throw new InvalidOperationException(
                $"Slot {(int)slot}: przed dalszą obsługą rozlicz wymagany wpis manualny.");
    }

    private void RebuildCurrent() => Current = BuildSnapshot(
        _lastFrame,
        GetEngine(TachographSlot.Driver)?.Current,
        GetEngine(TachographSlot.CoDriver)?.Current);

    private CrewTachographSnapshot BuildSnapshot(
        TelemetryFrame? frame,
        TachographSnapshot? driver,
        TachographSnapshot? coDriver,
        IReadOnlyList<CardSnapshotUpdate>? detachedCardUpdates = null)
    {
        var elapsed = _coDriverMovingBreakActive && frame is not null
            ? Math.Min(MovingBreakMinutes, MovingBreakElapsed(frame.GameTime))
            : 0;
        return new CrewTachographSnapshot
        {
            Frame = frame,
            DriverCardId = DriverCardId,
            CoDriverCardId = CoDriverCardId,
            Driver = driver,
            CoDriver = coDriver,
            DetachedCardUpdates = detachedCardUpdates ?? [],
            CoDriverMovingBreakActive = _coDriverMovingBreakActive,
            CoDriverMovingBreakCompleted = _coDriverMovingBreakCompleted,
            CoDriverMovingBreakElapsedMinutes = elapsed
        };
    }
}

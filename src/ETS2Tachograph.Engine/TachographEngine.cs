using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Rules;
using ETS2Tachograph.Core.Settings;
using ETS2Tachograph.Core.Telemetry;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.RuleEngine;

namespace ETS2Tachograph.Engine;

public sealed class TachographEngine : ITachographEngine
{
    private readonly ActivityHistoryProcessor _history;
    private readonly RegulationEngine _rules;
    private readonly int _weekEpochOffsetDays;

    public TachographEngine(
        string driverCardId,
        TachographSettings? settings = null,
        RegulationEngine? regulationEngine = null,
        RegulationOptions? regulationOptions = null)
    {
        _history = new ActivityHistoryProcessor(driverCardId, settings);
        _rules = regulationEngine ?? new RegulationEngine();
        _weekEpochOffsetDays = regulationOptions?.WeekEpochOffsetDays ?? 0;
        MultiManningEnabled = regulationOptions?.MultiManning ?? false;
        Current = new TachographSnapshot { MultiManningEnabled = MultiManningEnabled };
    }

    public TachographSnapshot Current { get; private set; }
    public ActivityHistoryProcessor History => _history;
    public bool MultiManningEnabled { get; private set; }
    public event EventHandler<TachographSnapshot>? SnapshotChanged;

    public TachographSnapshot ProcessFrame(TelemetryFrame frame) =>
        ProcessFrame(frame, frame.SpeedKph, slot: 1);

    internal TachographSnapshot ProcessFrame(TelemetryFrame frame, double vehicleSpeedKph, int slot)
    {
        // An inserted card closes a restored open CardRemoved gap at the first
        // trustworthy game minute. A rollback before its start leaves it on the
        // abandoned source branch and Process opens a clean activity branch.
        var closedGap = _history.CloseCardRemoved(frame.GameTime);
        var processed = _history.Process(frame, vehicleSpeedKph, slot);
        return ApplyUpdate(frame, Merge(closedGap, processed), frame.GameTime);
    }

    public TachographSnapshot Flush(DateTimeOffset recordedAtUtc)
    {
        var update = _history.Flush(recordedAtUtc);
        return ApplyUpdate(Current.Frame, update, Current.Frame?.GameTime);
    }

    internal TachographSnapshot OpenCardRemovedGap(
        GameTime start,
        int slot,
        DateTimeOffset recordedAtUtc)
    {
        var flushed = _history.FlushBeforeCardRemoval(recordedAtUtc);
        var opened = _history.OpenCardRemoved(start, slot);
        return ApplyUpdate(Current.Frame, Merge(flushed, opened), start);
    }

    internal TachographSnapshot CloseCardRemovedGap(GameTime endExclusive)
    {
        var update = _history.CloseCardRemoved(endExclusive);
        return ApplyUpdate(Current.Frame, update, endExclusive);
    }

    internal TachographSnapshot ObserveRemovedCard(TelemetryFrame frame, int slot)
    {
        var update = _history.ObserveRemovedCard(frame, slot);
        return ApplyUpdate(frame, update, frame.GameTime);
    }

    public void RestoreSessions(IEnumerable<IReadOnlyList<ActivityRecord>> sessions)
    {
        _history.RestoreSessions(sessions);
        RefreshAfterRestore();
    }

    public void RestoreSessions(IEnumerable<RestoredActivitySession> sessions)
    {
        _history.RestoreSessions(sessions);
        RefreshAfterRestore();
    }

    private void RefreshAfterRestore()
    {
        var regulationRecords = _history.RegulationRecords();
        var lastRecord = regulationRecords.LastOrDefault();
        var regulation = lastRecord is null
            ? null
            : _rules.Evaluate(
                new RuleContext(lastRecord.EndExclusive, regulationRecords),
                new RegulationOptions
                {
                    MultiManning = MultiManningEnabled,
                    WeekEpochOffsetDays = _weekEpochOffsetDays
                });
        Current = Current with
        {
            LastClosedRecord = _history.CurrentTimeline.Records.LastOrDefault(),
            CurrentSessionRecords = _history.CurrentTimeline.Records,
            CurrentSessionGaps = _history.CurrentSessionGaps,
            RequiredManualEntryGap = _history.RequiredManualEntryGap,
            OptionalManualEntryGap = _history.OptionalManualEntryGap,
            SessionIndex = _history.Sessions.Count - 1,
            Regulation = regulation
        };
        SnapshotChanged?.Invoke(this, Current);
    }

    public void SetManualActivity(DriverActivity activity)
    {
        _history.ManualActivity = activity;
        RefreshModeState();
    }

    public void SetOutMode(bool enabled)
    {
        _history.SetOutMode(enabled);
        RefreshModeState();
    }

    public void SetFerryMode(bool enabled)
    {
        _history.SetFerryMode(enabled);
        RefreshModeState();
    }

    public void SetMultiManning(bool enabled)
    {
        MultiManningEnabled = enabled;
        RefreshModeState();
    }

    public void ApplyManualEntryResolution(
        ActivityGap resolvedGap,
        IReadOnlyList<ActivityRecord> segments)
    {
        _history.ApplyManualEntryResolution(resolvedGap, segments);
        RefreshModeState();
    }

    private void RefreshModeState()
    {
        var regulation = Current.Frame is null
            ? Current.Regulation
            : _rules.Evaluate(
                new RuleContext(Current.Frame.GameTime, _history.RegulationRecords()),
                new RegulationOptions
                {
                    MultiManning = MultiManningEnabled,
                    WeekEpochOffsetDays = _weekEpochOffsetDays
                });

        Current = Current with
        {
            ManualActivity = _history.ManualActivity,
            OutModeEnabled = _history.OutModeEnabled,
            FerryModeEnabled = _history.FerryModeEnabled,
            MultiManningEnabled = MultiManningEnabled,
            CurrentSessionRecords = _history.CurrentTimeline.Records,
            CurrentSessionGaps = _history.CurrentSessionGaps,
            RequiredManualEntryGap = _history.RequiredManualEntryGap,
            OptionalManualEntryGap = _history.OptionalManualEntryGap,
            Regulation = regulation
        };
        SnapshotChanged?.Invoke(this, Current);
    }

    private TachographSnapshot ApplyUpdate(
        TelemetryFrame? frame,
        ActivityHistoryUpdate update,
        GameTime? ruleTime)
    {
        var regulation = ruleTime is null
            ? Current.Regulation
            : _rules.Evaluate(
                new RuleContext(ruleTime.Value, _history.RegulationRecords()),
                new RegulationOptions
                {
                    MultiManning = MultiManningEnabled,
                    WeekEpochOffsetDays = _weekEpochOffsetDays
                });

        Current = new TachographSnapshot
        {
            Frame = frame,
            ManualActivity = _history.ManualActivity,
            ProvisionalActivity = update.ProvisionalActivity,
            LastClosedRecord = update.CompletedRecords.LastOrDefault() ?? Current.LastClosedRecord,
            CompletedBatches = update.CompletedBatches,
            CreatedGapBatches = update.CreatedGapBatches,
            OpenedSessions = update.OpenedSessions,
            CompletedRecords = update.CompletedRecords,
            CreatedGaps = update.CreatedGaps,
            CurrentSessionRecords = _history.CurrentTimeline.Records,
            CurrentSessionGaps = _history.CurrentSessionGaps,
            RequiredManualEntryGap = _history.RequiredManualEntryGap,
            OptionalManualEntryGap = _history.OptionalManualEntryGap,
            Regulation = regulation,
            OutModeEnabled = _history.OutModeEnabled,
            FerryModeEnabled = _history.FerryModeEnabled,
            MultiManningEnabled = MultiManningEnabled,
            ClockMovedBackward = update.ClockMovedBackward,
            GameTimeJumpDetected = update.GameTimeJumpDetected,
            WorldGenerationChanged = update.WorldGenerationChanged,
            SessionIndex = update.SessionIndex
        };
        SnapshotChanged?.Invoke(this, Current);
        return Current;
    }

    private static ActivityHistoryUpdate Merge(
        ActivityHistoryUpdate first,
        ActivityHistoryUpdate second) =>
        new(
            first.CompletedBatches.Concat(second.CompletedBatches).ToList(),
            first.OpenedSessions.Concat(second.OpenedSessions).ToList(),
            first.ClockMovedBackward || second.ClockMovedBackward,
            first.GameTimeJumpDetected || second.GameTimeJumpDetected,
            first.WorldGenerationChanged || second.WorldGenerationChanged,
            second.SessionIndex,
            second.ProvisionalActivity)
        {
            CreatedGapBatches = first.CreatedGapBatches
                .Concat(second.CreatedGapBatches)
                .ToList()
        };
}

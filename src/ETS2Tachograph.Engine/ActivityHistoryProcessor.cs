using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.OneMinuteRule;
using ETS2Tachograph.Core.Services;
using ETS2Tachograph.Core.Settings;
using ETS2Tachograph.Core.Telemetry;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Engine;

/// <summary>Transforms generic telemetry frames into append-only tachograph history.</summary>
public sealed class ActivityHistoryProcessor
{
    private const long MaxAutomaticReconstructionJumpMinutes = 2;

    private sealed record PendingForwardJump(
        Guid GapId,
        int SessionIndex,
        GameTime Start,
        GameTime EndExclusive,
        DriverActivity Activity,
        SpecialCondition Condition);

    private readonly string _driverCardId;
    private readonly TachographSettings _settings;
    private readonly OneMinuteRuleAggregator _aggregator = new();
    private readonly List<ActivityTimeline> _sessions = [new()];
    private readonly List<List<ActivityGap>> _gapSessions = [[]];
    private readonly List<GameTime?> _sessionStartedAt = [null];
    private readonly List<ActivitySlice> _slices = [];
    private GameTime? _currentMinute;
    private DriverActivity? _lastActivity;
    private DriverActivity? _activityBeforePause;
    private DateTimeOffset? _lastRecordedAtUtc;
    private DriverActivity _manualActivity;
    private SpecialCondition _currentCondition;
    private SpecialCondition _conditionBeforePause;
    private uint? _lastWorldGeneration;
    private uint? _lastCargoOperationGeneration;
    private double? _lastVehicleSpeedKph;
    private GameTime? _lastObservedGameTime;
    private PendingForwardJump? _pendingForwardJump;

    public ActivityHistoryProcessor(string driverCardId, TachographSettings? settings = null)
    {
        if (string.IsNullOrWhiteSpace(driverCardId))
        {
            throw new ArgumentException("Driver card id is required.", nameof(driverCardId));
        }

        _driverCardId = driverCardId;
        _settings = settings ?? new TachographSettings();
        ManualActivity = _settings.ActivityAfterStop;
    }

    public IReadOnlyList<ActivityTimeline> Sessions => _sessions.AsReadOnly();
    public IReadOnlyList<IReadOnlyList<ActivityGap>> GapSessions => _gapSessions
        .Select(gaps => (IReadOnlyList<ActivityGap>)gaps.AsReadOnly())
        .ToList();
    public ActivityTimeline CurrentTimeline => _sessions[^1];
    public IReadOnlyList<ActivityGap> CurrentSessionGaps => _gapSessions[^1].AsReadOnly();
    public ActivityGap? OpenCardRemovedGap => CanonicalGaps()
        .LastOrDefault(gap => gap.Reason == ActivityGapReason.CardRemoved && gap.IsOpen);
    public ActivityGap? RequiredManualEntryGap => CanonicalGaps()
        .FirstOrDefault(gap =>
            gap.Reason == ActivityGapReason.CardRemoved &&
            gap.State == ActivityGapState.Unresolved &&
            !gap.IsOpen);
    public ActivityGap? OptionalManualEntryGap => CanonicalGaps()
        .FirstOrDefault(gap =>
            gap.Reason == ActivityGapReason.ForwardTimeJump &&
            gap.State == ActivityGapState.Unresolved &&
            !gap.IsOpen);
    public bool OutModeEnabled { get; private set; }
    public bool FerryModeEnabled { get; private set; }
    public ActivityRecord? ProvisionalRecord
    {
        get
        {
            var minute = _aggregator.ProvisionalActivity;
            if (minute is null) return null;
            return new ActivityRecord
            {
                Id = Guid.Empty,
                DriverCardId = _driverCardId,
                Activity = minute.LongestContinuousActivity,
                Start = minute.Minute,
                EndExclusive = minute.Minute.AddMinutes(1),
                RecordedAtUtc = _lastRecordedAtUtc ?? DateTimeOffset.UtcNow,
                Source = minute.Source,
                Condition = minute.Condition
            };
        }
    }

    public IReadOnlyList<ActivityRecord> RegulationRecords()
    {
        var provisional = ProvisionalRecord;
        var records = new List<ActivityRecord>();

        for (var index = 0; index < _sessions.Count; index++)
        {
            var sessionRecords = _sessions[index].Records;
            var branchStart = _sessionStartedAt[index] ??
                              sessionRecords.FirstOrDefault()?.Start ??
                              _gapSessions[index].FirstOrDefault()?.Start;
            if (branchStart is null && index == _sessions.Count - 1)
                branchStart = provisional?.Start ?? _currentMinute;

            // A later session is a new branch created after the game clock moved
            // backwards. Preserve the earlier history, but replace the overlapping
            // future that belongs to the abandoned branch.
            if (index > 0 && branchStart is not null)
                records.RemoveAll(record => record.EndExclusive > branchStart.Value);

            records.AddRange(sessionRecords);
        }

        if (provisional is not null &&
            (records.Count == 0 || records[^1].EndExclusive <= provisional.Start))
            records.Add(provisional);

        return records;
    }

    public IReadOnlyList<ActivityGap> CanonicalGaps()
    {
        var gaps = new List<ActivityGap>();
        for (var index = 0; index < _sessions.Count; index++)
        {
            var branchStart = _sessionStartedAt[index] ??
                              _sessions[index].Records.FirstOrDefault()?.Start ??
                              _gapSessions[index].FirstOrDefault()?.Start;
            if (index > 0 && branchStart is not null)
                TruncateGapsAfter(gaps, branchStart.Value);
            foreach (var gap in _gapSessions[index])
            {
                if (gap.ProjectionSourceGapId is { } sourceGapId)
                    gaps.RemoveAll(existing => existing.Id == sourceGapId);
                gaps.Add(gap);
            }
        }

        return gaps.OrderBy(gap => gap.Start).ToList();
    }

    public DriverActivity ManualActivity
    {
        get => _manualActivity;
        set
        {
            if (value is not (
                DriverActivity.OtherWork or
                DriverActivity.Availability or
                DriverActivity.BreakOrRest))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _manualActivity = value;
        }
    }

    public void RestoreSessions(IEnumerable<IReadOnlyList<ActivityRecord>> sessions)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        RestoreSessions(sessions.Select((records, index) => new RestoredActivitySession(
            index,
            records.FirstOrDefault()?.Start,
            records)));
    }

    public void RestoreSessions(IEnumerable<RestoredActivitySession> sessions)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        if (_currentMinute is not null || _aggregator.ProvisionalActivity is not null)
            throw new InvalidOperationException("History can only be restored before telemetry processing starts.");

        _sessions.Clear();
        _gapSessions.Clear();
        _sessionStartedAt.Clear();
        foreach (var session in sessions.OrderBy(x => x.SessionIndex))
        {
            var timeline = new ActivityTimeline();
            foreach (var record in session.Records.OrderBy(x => x.Start))
            {
                if (!string.Equals(record.DriverCardId, _driverCardId, StringComparison.Ordinal))
                    throw new InvalidOperationException("Restored activity belongs to another driver card.");
                timeline.Append(record);
            }
            _sessions.Add(timeline);
            var restoredGaps = (session.Gaps ?? [])
                .OrderBy(gap => gap.Start)
                .ToList();
            if (restoredGaps.Any(gap =>
                    !string.Equals(gap.DriverCardId, _driverCardId, StringComparison.Ordinal) ||
                    gap.SessionIndex != session.SessionIndex))
                throw new InvalidOperationException("Restored activity gap belongs to another card or session.");
            _gapSessions.Add(restoredGaps);
            _sessionStartedAt.Add(session.StartedAt);
        }

        if (_sessions.Count == 0)
        {
            _sessions.Add(new ActivityTimeline());
            _gapSessions.Add([]);
            _sessionStartedAt.Add(null);
        }
    }

    public void ApplyManualEntryResolution(
        ActivityGap resolvedGap,
        IReadOnlyList<ActivityRecord> segments)
    {
        ArgumentNullException.ThrowIfNull(resolvedGap);
        ArgumentNullException.ThrowIfNull(segments);
        if (resolvedGap.State != ActivityGapState.Resolved || resolvedGap.EndExclusive is null)
            throw new InvalidOperationException("A manual-entry resolution requires a closed, resolved gap.");
        if (segments.Count == 0 || segments.Any(record =>
                !string.Equals(record.DriverCardId, _driverCardId, StringComparison.OrdinalIgnoreCase) ||
                record.Source != ActivitySource.ManualEntry ||
                record.SourceGapId != resolvedGap.Id))
            throw new InvalidOperationException("Invalid manual-entry records for the resolved gap.");

        if (resolvedGap.ProjectionSourceGapId is { } projectionSourceGapId)
        {
            var sourceProjection = CanonicalGaps().SingleOrDefault(gap => gap.Id == projectionSourceGapId) ??
                throw new InvalidOperationException(
                    $"Projected source gap {projectionSourceGapId} is not canonical.");
            if (sourceProjection.Start != resolvedGap.Start ||
                sourceProjection.EndExclusive != resolvedGap.EndExclusive)
                throw new InvalidOperationException(
                    "The materialized resolution does not match the canonical gap projection.");
            if (resolvedGap.SessionIndex < 0 || resolvedGap.SessionIndex >= _sessions.Count)
                throw new InvalidOperationException("The resolved projection targets an unknown clock session.");

            var targetSessionIndex = resolvedGap.SessionIndex;
            var rebuiltProjection = new ActivityTimeline();
            foreach (var record in _sessions[targetSessionIndex].Records
                         .Where(record => record.SourceGapId != resolvedGap.Id)
                         .Concat(segments)
                         .OrderBy(record => record.Start))
                rebuiltProjection.Append(record);

            _sessions[targetSessionIndex] = rebuiltProjection;
            _gapSessions[targetSessionIndex].Add(resolvedGap);
            return;
        }

        var gapSessionIndex = -1;
        var gapIndex = -1;
        for (var sessionIndex = 0; sessionIndex < _gapSessions.Count; sessionIndex++)
        {
            gapIndex = _gapSessions[sessionIndex].FindIndex(gap => gap.Id == resolvedGap.Id);
            if (gapIndex < 0) continue;
            gapSessionIndex = sessionIndex;
            break;
        }
        if (gapSessionIndex < 0)
            throw new InvalidOperationException($"Activity gap {resolvedGap.Id} is not loaded in the card engine.");

        var sourceGap = _gapSessions[gapSessionIndex][gapIndex];
        if (sourceGap.Start != resolvedGap.Start || sourceGap.EndExclusive != resolvedGap.EndExclusive)
            throw new InvalidOperationException("The resolved gap does not match the loaded clock branch.");

        var rebuilt = new ActivityTimeline();
        foreach (var record in _sessions[gapSessionIndex].Records
                     .Where(record => record.SourceGapId != resolvedGap.Id)
                     .Concat(segments)
                     .OrderBy(record => record.Start))
            rebuilt.Append(record);

        _sessions[gapSessionIndex] = rebuilt;
        _gapSessions[gapSessionIndex][gapIndex] = resolvedGap;
    }

    public void SetOutMode(bool enabled)
    {
        OutModeEnabled = enabled;
        if (enabled)
        {
            FerryModeEnabled = false;
        }
    }

    public void SetFerryMode(bool enabled)
    {
        FerryModeEnabled = enabled;
        if (enabled)
        {
            OutModeEnabled = false;
        }
    }

    public ActivityHistoryUpdate OpenCardRemoved(GameTime start, int slot)
    {
        if (slot is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(slot));
        if (_gapSessions[^1].Any(gap =>
                gap.Reason == ActivityGapReason.CardRemoved && gap.IsOpen))
            return Update([], false, false);

        _sessionStartedAt[^1] ??= start;
        var gap = new ActivityGap
        {
            Id = Guid.NewGuid(),
            DriverCardId = _driverCardId,
            Slot = slot,
            SessionIndex = _sessions.Count - 1,
            Start = start,
            EndExclusive = null,
            Reason = ActivityGapReason.CardRemoved,
            State = ActivityGapState.Unresolved
        };
        _gapSessions[^1].Add(gap);
        _lastObservedGameTime = start;
        return Update([], false, false, createdGaps: [gap]);
    }

    public ActivityHistoryUpdate CloseCardRemoved(GameTime endExclusive)
    {
        var sessionIndex = _sessions.Count - 1;
        var gaps = _gapSessions[sessionIndex];
        var index = gaps.FindLastIndex(gap =>
            gap.Reason == ActivityGapReason.CardRemoved && gap.IsOpen);
        if (index < 0)
            return Update([], false, false);

        var open = gaps[index];
        if (endExclusive < open.Start)
            return Update([], false, false);

        if (endExclusive == open.Start)
        {
            gaps.RemoveAt(index);
            return Update(
                [],
                false,
                false,
                removedGapIds: [open.Id],
                gapSessionIndex: sessionIndex);
        }

        var closed = open with { EndExclusive = endExclusive };
        gaps[index] = closed;
        return Update(
            [],
            false,
            false,
            createdGaps: [closed],
            gapSessionIndex: sessionIndex);
    }

    /// <summary>
    /// Observes the shared game clock while this card is outside the device.
    /// Forward jumps remain inside CardRemoved; a new clock branch reopens one
    /// CardRemoved gap at the branch anchor without creating activity.
    /// </summary>
    public ActivityHistoryUpdate ObserveRemovedCard(TelemetryFrame frame, int slot)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (slot is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(slot));
        if (frame.GamePaused)
            return Update([], false, false);

        var openGap = OpenCardRemovedGap;
        if (openGap is null)
            return Update([], false, false);

        var previousObserved = _lastObservedGameTime;
        var worldGenerationChanged = _lastWorldGeneration is not null &&
                                     _lastWorldGeneration.Value != frame.WorldGeneration;
        var clockMovedBackward = previousObserved is not null
            ? frame.GameTime < previousObserved.Value
            : frame.GameTime < openGap.Start;
        _lastWorldGeneration = frame.WorldGeneration;
        _lastCargoOperationGeneration = frame.CargoOperationGeneration;
        _lastObservedGameTime = frame.GameTime;

        if (!worldGenerationChanged && !clockMovedBackward)
            return Update([], false, false);

        var openedSession = StartNewSession(frame.GameTime);
        _sessionStartedAt[^1] = frame.GameTime;
        var replacement = new ActivityGap
        {
            Id = Guid.NewGuid(),
            DriverCardId = _driverCardId,
            Slot = slot,
            SessionIndex = _sessions.Count - 1,
            Start = frame.GameTime,
            EndExclusive = null,
            Reason = ActivityGapReason.CardRemoved,
            State = ActivityGapState.Unresolved
        };
        _gapSessions[^1].Add(replacement);
        return Update(
            [],
            clockMovedBackward,
            false,
            openedSessions: [openedSession],
            worldGenerationChanged: worldGenerationChanged,
            createdGaps: [replacement]);
    }

    public ActivityHistoryUpdate Process(TelemetryFrame frame) =>
        Process(frame, frame.SpeedKph, slot: 1);

    internal ActivityHistoryUpdate Process(
        TelemetryFrame frame,
        double vehicleSpeedKph,
        int slot,
        CrewTimeJumpResolution? crewTimeJumpResolution = null)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (slot is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(slot));
        var completed = new List<ActivityRecord>();
        var createdGaps = new List<ActivityGap>();

        if (frame.GamePaused)
        {
            _lastWorldGeneration ??= frame.WorldGeneration;
            AccumulateUntil(frame.RecordedAtUtc);
            if (_lastActivity is not null && _activityBeforePause is null)
            {
                // Paused/menu frames must not add real-time slices, but a cargo
                // operation may advance the game clock while telemetry is paused.
                // Preserve the last selected pictogram separately so the resumed
                // frame can classify that known loading/unloading interval.
                _activityBeforePause = _lastActivity;
                _conditionBeforePause = _currentCondition;
            }
            _lastActivity = null;
            _lastRecordedAtUtc = frame.RecordedAtUtc;
            return Update(completed, false, false);
        }

        var activityBeforePause = _activityBeforePause;
        var conditionBeforePause = _conditionBeforePause;
        _activityBeforePause = null;
        _conditionBeforePause = SpecialCondition.None;

        _lastObservedGameTime = frame.GameTime;
        var previousVehicleSpeedKph = _lastVehicleSpeedKph;
        _lastVehicleSpeedKph = vehicleSpeedKph;
        var activity = SelectActivity(frame);
        var condition = FerryModeEnabled ? SpecialCondition.FerryCrossing : SpecialCondition.None;
        var cargoOperationCompleted = _lastCargoOperationGeneration is not null &&
                                      _lastCargoOperationGeneration.Value != frame.CargoOperationGeneration;
        _lastCargoOperationGeneration = frame.CargoOperationGeneration;
        var worldGenerationChanged = _lastWorldGeneration is not null &&
                                     _lastWorldGeneration.Value != frame.WorldGeneration;
        _lastWorldGeneration = frame.WorldGeneration;
        if (worldGenerationChanged)
        {
            _pendingForwardJump = null;
            var clockMovedBackward = _currentMinute is not null && frame.GameTime < _currentMinute.Value;
            int? completedSessionIndex = null;
            if (_currentMinute is not null)
            {
                completedSessionIndex = _sessions.Count - 1;
                FlushInto(completed, frame.RecordedAtUtc);
            }

            var openedSession = StartNewSession(frame.GameTime);
            StartMinute(frame.GameTime, activity, condition, frame.RecordedAtUtc);
            return Update(
                completed,
                clockMovedBackward,
                false,
                completedSessionIndex,
                [openedSession],
                worldGenerationChanged: true);
        }

        if (_currentMinute is null)
        {
            var lastRestoredRecord = CurrentTimeline.Records.LastOrDefault();
            var lastRestoredGap = CurrentSessionGaps.LastOrDefault();
            var lastKnownMinute = new[]
                {
                    lastRestoredRecord?.EndExclusive,
                    lastRestoredGap?.EndExclusive ?? lastRestoredGap?.Start
                }
                .Where(value => value is not null)
                .Select(value => value!.Value)
                .DefaultIfEmpty(frame.GameTime)
                .Max();
            if (frame.GameTime < lastKnownMinute)
            {
                var openedSession = StartNewSession(frame.GameTime);
                StartMinute(frame.GameTime, activity, condition, frame.RecordedAtUtc);
                return Update(completed, false, false, openedSessions: [openedSession]);
            }
            StartMinute(frame.GameTime, activity, condition, frame.RecordedAtUtc);
            return Update(completed, false, false);
        }

        if (frame.GameTime < _currentMinute.Value)
        {
            _pendingForwardJump = null;
            var completedSessionIndex = _sessions.Count - 1;
            FlushInto(completed, frame.RecordedAtUtc);
            var openedSession = StartNewSession(frame.GameTime);
            StartMinute(frame.GameTime, activity, condition, frame.RecordedAtUtc);
            return Update(
                completed,
                true,
                false,
                completedSessionIndex,
                [openedSession]);
        }

        var removedLateCargoGapId = cargoOperationCompleted
            ? ReclassifyPendingCargoOperation(
                frame.GameTime,
                previousVehicleSpeedKph,
                vehicleSpeedKph,
                completed,
                frame.RecordedAtUtc)
            : null;

        if (frame.GameTime == _currentMinute.Value)
        {
            AccumulateUntil(frame.RecordedAtUtc);
            _lastActivity = activity;
            _lastRecordedAtUtc = frame.RecordedAtUtc;
            if (condition == SpecialCondition.FerryCrossing)
            {
                _currentCondition = condition;
            }

            return Update(
                completed,
                false,
                false,
                removedGapIds: removedLateCargoGapId is null ? null : [removedLateCargoGapId.Value]);
        }

        var previousMinute = _currentMinute.Value;
        var reconstructionActivity = _lastActivity ?? activityBeforePause;
        var reconstructionCondition = _lastActivity is null && activityBeforePause is not null
            ? conditionBeforePause
            : _currentCondition;
        AccumulateUntil(frame.RecordedAtUtc);
        CloseCurrentMinute(completed, ActivitySource.Telemetry, frame.RecordedAtUtc);

        var jumpMinutes = frame.GameTime - previousMinute;
        var jump = jumpMinutes > 1;
        DriverActivity? missingMinutesActivity = null;
        var missingMinutesCondition = reconstructionCondition;
        if (reconstructionActivity is not null)
        {
            if (jumpMinutes <= MaxAutomaticReconstructionJumpMinutes)
            {
                missingMinutesActivity = reconstructionActivity;
            }
            else if (cargoOperationCompleted &&
                     VehicleWasAndIsStopped(previousVehicleSpeedKph, vehicleSpeedKph))
            {
                // The official cargo-loaded/job-delivered event makes this a
                // known loading or unloading interval, not an unexplained gap.
                // Preserve the activity explicitly selected on each card; the
                // cargo event identifies the interval but does not choose its
                // tachograph pictogram for the driver.
                missingMinutesActivity = reconstructionActivity;
            }
            else if (CanApplyCrewTimeJumpResolution(
                         crewTimeJumpResolution,
                         slot,
                         previousMinute,
                         frame.GameTime,
                         reconstructionActivity.Value,
                         activity))
            {
                missingMinutesActivity = reconstructionActivity;
            }
            else if (CanReconstructLongRest(
                         reconstructionActivity.Value,
                         activity,
                         previousVehicleSpeedKph,
                         vehicleSpeedKph))
            {
                missingMinutesActivity = reconstructionActivity;
            }
        }

        if (missingMinutesActivity is not null)
        {
            for (var minute = previousMinute.TotalMinutes + 1;
                 minute < frame.GameTime.TotalMinutes;
                 minute++)
            {
                Push(
                    MinuteActivity.FromSlices(
                        new GameTime(minute),
                        [new ActivitySlice(missingMinutesActivity.Value, TimeSpan.FromMinutes(1))],
                        ActivitySource.Reconstructed,
                        missingMinutesCondition),
                    completed,
                    frame.RecordedAtUtc);
            }
        }
        else if (jump)
        {
            var gap = new ActivityGap
            {
                Id = Guid.NewGuid(),
                DriverCardId = _driverCardId,
                Slot = slot,
                SessionIndex = _sessions.Count - 1,
                Start = previousMinute.AddMinutes(1),
                EndExclusive = frame.GameTime,
                Reason = ActivityGapReason.ForwardTimeJump,
                State = ActivityGapState.Unresolved
            };
            _gapSessions[^1].Add(gap);
            createdGaps.Add(gap);
            if (reconstructionActivity is not null &&
                VehicleWasAndIsStopped(previousVehicleSpeedKph, vehicleSpeedKph))
            {
                _pendingForwardJump = new PendingForwardJump(
                    gap.Id,
                    gap.SessionIndex,
                    gap.Start,
                    gap.EndExclusive.Value,
                    reconstructionActivity.Value,
                    reconstructionCondition);
            }
            // A deliberate gap is a hard boundary for the one-minute aggregator.
            // Flush the minute before the jump so a later telemetry minute is not
            // required to be consecutive with it and no synthetic activity is added.
            FlushPendingMinute(completed, frame.RecordedAtUtc);
        }

        StartMinute(frame.GameTime, activity, condition, frame.RecordedAtUtc);
        if (_pendingForwardJump is not null &&
            _pendingForwardJump.EndExclusive < frame.GameTime)
            _pendingForwardJump = null;
        return Update(
            completed,
            false,
            jump,
            createdGaps: createdGaps,
            removedGapIds: removedLateCargoGapId is null ? null : [removedLateCargoGapId.Value]);
    }

    public ActivityHistoryUpdate Flush(DateTimeOffset recordedAtUtc)
    {
        var completed = new List<ActivityRecord>();
        FlushInto(completed, recordedAtUtc);
        return Update(completed, false, false);
    }

    public ActivityHistoryUpdate FlushBeforeCardRemoval(DateTimeOffset recordedAtUtc)
    {
        var completed = new List<ActivityRecord>();
        // The current game minute becomes the first minute of CardRemoved. Keep
        // only an already aggregated preceding minute, otherwise that same minute
        // would exist both as trusted activity and as an audit gap.
        FlushPendingMinute(completed, recordedAtUtc);
        ResetCurrentMinuteState();
        return Update(completed, false, false);
    }

    private ActivitySessionStart StartNewSession(GameTime startedAt)
    {
        _pendingForwardJump = null;
        _sessions.Add(new ActivityTimeline());
        _gapSessions.Add([]);
        _sessionStartedAt.Add(startedAt);
        return new ActivitySessionStart(_sessions.Count - 1, startedAt);
    }

    private DriverActivity SelectActivity(TelemetryFrame frame)
    {
        if (OutModeEnabled)
        {
            return DriverActivity.OutOfScope;
        }

        return Math.Abs(frame.SpeedKph) > _settings.DrivingSpeedThresholdKph
            ? DriverActivity.Driving
            : ManualActivity;
    }

    private bool CanReconstructLongRest(
        DriverActivity previousActivity,
        DriverActivity currentActivity,
        double? previousVehicleSpeedKph,
        double currentVehicleSpeedKph) =>
        previousActivity == DriverActivity.BreakOrRest &&
        currentActivity == DriverActivity.BreakOrRest &&
        previousVehicleSpeedKph is not null &&
        !IsVehicleMoving(previousVehicleSpeedKph.Value) &&
        !IsVehicleMoving(currentVehicleSpeedKph);

    internal DriverActivity? StableActivityForCrewJump(TelemetryFrame frame)
    {
        var previousActivity = _lastActivity ?? _activityBeforePause;
        var currentActivity = SelectActivity(frame);
        return previousActivity is not null && previousActivity.Value == currentActivity
            ? currentActivity
            : null;
    }

    private static bool CanApplyCrewTimeJumpResolution(
        CrewTimeJumpResolution? resolution,
        int slot,
        GameTime previousMinute,
        GameTime currentMinute,
        DriverActivity previousActivity,
        DriverActivity currentActivity)
    {
        if (resolution is null ||
            !resolution.VehicleStationaryBeforeAndAfter ||
            !resolution.ExplainedByCrewRest ||
            resolution.StartGameMinute != previousMinute.TotalMinutes + 1 ||
            resolution.EndGameMinuteExclusive != currentMinute.TotalMinutes ||
            previousActivity != currentActivity ||
            !resolution.ReconstructedActivities.TryGetValue(slot, out var resolvedActivity) ||
            resolvedActivity != currentActivity)
        {
            return false;
        }

        return resolvedActivity is
            DriverActivity.BreakOrRest or
            DriverActivity.OtherWork or
            DriverActivity.Availability;
    }

    private bool VehicleWasAndIsStopped(
        double? previousVehicleSpeedKph,
        double currentVehicleSpeedKph) =>
        previousVehicleSpeedKph is not null &&
        !IsVehicleMoving(previousVehicleSpeedKph.Value) &&
        !IsVehicleMoving(currentVehicleSpeedKph);

    private Guid? ReclassifyPendingCargoOperation(
        GameTime observedGameTime,
        double? previousVehicleSpeedKph,
        double currentVehicleSpeedKph,
        List<ActivityRecord> completed,
        DateTimeOffset recordedAtUtc)
    {
        var pending = _pendingForwardJump;
        if (pending is null ||
            pending.SessionIndex != _sessions.Count - 1 ||
            _currentMinute != pending.EndExclusive ||
            observedGameTime < pending.EndExclusive ||
            !VehicleWasAndIsStopped(previousVehicleSpeedKph, currentVehicleSpeedKph))
            return null;

        var gaps = _gapSessions[pending.SessionIndex];
        var gapIndex = gaps.FindIndex(gap =>
            gap.Id == pending.GapId &&
            gap.Reason == ActivityGapReason.ForwardTimeJump &&
            gap.State == ActivityGapState.Unresolved &&
            gap.Start == pending.Start &&
            gap.EndExclusive == pending.EndExclusive);
        if (gapIndex < 0)
        {
            _pendingForwardJump = null;
            return null;
        }

        gaps.RemoveAt(gapIndex);
        for (var minute = pending.Start.TotalMinutes;
             minute < pending.EndExclusive.TotalMinutes;
             minute++)
        {
            Push(
                MinuteActivity.FromSlices(
                    new GameTime(minute),
                    [new ActivitySlice(pending.Activity, TimeSpan.FromMinutes(1))],
                    ActivitySource.Reconstructed,
                    pending.Condition),
                completed,
                recordedAtUtc);
        }

        _pendingForwardJump = null;
        return pending.GapId;
    }

    private bool IsVehicleMoving(double speedKph) =>
        Math.Abs(speedKph) > _settings.DrivingSpeedThresholdKph;

    private void StartMinute(
        GameTime minute,
        DriverActivity activity,
        SpecialCondition condition,
        DateTimeOffset recordedAtUtc)
    {
        _sessionStartedAt[^1] ??= minute;
        _currentMinute = minute;
        _lastActivity = activity;
        _currentCondition = condition;
        _lastRecordedAtUtc = recordedAtUtc;
        _slices.Clear();
    }

    private void AccumulateUntil(DateTimeOffset recordedAtUtc)
    {
        if (_lastActivity is null || _lastRecordedAtUtc is null)
        {
            return;
        }

        var duration = recordedAtUtc - _lastRecordedAtUtc.Value;
        if (duration > TimeSpan.Zero)
        {
            _slices.Add(new ActivitySlice(_lastActivity.Value, duration));
        }
    }

    private void CloseCurrentMinute(
        List<ActivityRecord> completed,
        ActivitySource source,
        DateTimeOffset recordedAtUtc)
    {
        if (_currentMinute is null)
        {
            return;
        }

        if (_slices.Count == 0)
        {
            _slices.Add(new ActivitySlice(
                _lastActivity ?? _settings.ActivityAfterStop,
                TimeSpan.FromTicks(1)));
        }

        Push(
            MinuteActivity.FromSlices(
                _currentMinute.Value,
                _slices,
                source,
                _currentCondition),
            completed,
            recordedAtUtc);
        _slices.Clear();
    }

    private void Push(
        MinuteActivity minute,
        List<ActivityRecord> completed,
        DateTimeOffset recordedAtUtc)
    {
        var aggregated = _aggregator.Push(minute);
        if (aggregated is not null)
        {
            Append(aggregated, completed, recordedAtUtc);
        }
    }

    private void FlushInto(List<ActivityRecord> completed, DateTimeOffset recordedAtUtc)
    {
        if (_currentMinute is not null)
        {
            CloseCurrentMinute(completed, ActivitySource.Telemetry, recordedAtUtc);
        }

        FlushPendingMinute(completed, recordedAtUtc);
        ResetCurrentMinuteState();
    }

    private void ResetCurrentMinuteState()
    {
        _pendingForwardJump = null;
        _currentMinute = null;
        _lastActivity = null;
        _activityBeforePause = null;
        _lastRecordedAtUtc = null;
        _currentCondition = SpecialCondition.None;
        _conditionBeforePause = SpecialCondition.None;
        _slices.Clear();
    }

    private void FlushPendingMinute(
        List<ActivityRecord> completed,
        DateTimeOffset recordedAtUtc)
    {
        var pending = _aggregator.Flush();
        if (pending is not null)
        {
            Append(pending, completed, recordedAtUtc);
        }
    }

    private void Append(
        AggregatedMinute minute,
        List<ActivityRecord> completed,
        DateTimeOffset recordedAtUtc)
    {
        var record = new ActivityRecord
        {
            Id = Guid.NewGuid(),
            DriverCardId = _driverCardId,
            Activity = minute.Activity,
            Start = minute.Minute,
            EndExclusive = minute.Minute.AddMinutes(1),
            RecordedAtUtc = recordedAtUtc,
            Source = minute.Source,
            Condition = minute.Condition
        };

        CurrentTimeline.Append(record);
        completed.Add(record);
    }

    private ActivityHistoryUpdate Update(
        IReadOnlyList<ActivityRecord> completed,
        bool clockMovedBackward,
        bool jumpDetected,
        int? completedSessionIndex = null,
        IReadOnlyList<ActivitySessionStart>? openedSessions = null,
        bool worldGenerationChanged = false,
        IReadOnlyList<ActivityGap>? createdGaps = null,
        IReadOnlyList<Guid>? removedGapIds = null,
        int? gapSessionIndex = null)
    {
        var batchSessionIndex = completedSessionIndex ?? _sessions.Count - 1;
        IReadOnlyList<ActivityCompletionBatch> batches = completed.Count == 0
            ? []
            :
            [
                new ActivityCompletionBatch(
                    batchSessionIndex,
                    _sessionStartedAt[batchSessionIndex] ?? completed[0].Start,
                    completed)
            ];

        var hasGapUpserts = createdGaps is not null && createdGaps.Count > 0;
        var hasGapRemovals = removedGapIds is not null && removedGapIds.Count > 0;
        var ownerSessionIndex = gapSessionIndex ?? _sessions.Count - 1;
        IReadOnlyList<ActivityGapBatch> gapBatches = !hasGapUpserts && !hasGapRemovals
            ? []
            :
            [
                new ActivityGapBatch(
                    ownerSessionIndex,
                    _sessionStartedAt[ownerSessionIndex] ?? createdGaps?.FirstOrDefault()?.Start ??
                        throw new InvalidOperationException("A gap change requires a session anchor."),
                    createdGaps ?? [],
                    removedGapIds ?? [])
            ];

        return new ActivityHistoryUpdate(
            batches,
            openedSessions ?? [],
            clockMovedBackward,
            jumpDetected,
            worldGenerationChanged,
            _sessions.Count - 1,
            _lastActivity)
        {
            CreatedGapBatches = gapBatches
        };
    }

    private static void TruncateGapsAfter(List<ActivityGap> gaps, GameTime branchStart)
    {
        for (var index = gaps.Count - 1; index >= 0; index--)
        {
            var gap = gaps[index];
            if (gap.Start >= branchStart)
            {
                gaps.RemoveAt(index);
                continue;
            }

            if (gap.EndExclusive is null || gap.EndExclusive.Value > branchStart)
            {
                var resolutionWasTruncated = gap.ResolvedAt is not null &&
                                             gap.ResolvedAt.Value >= branchStart;
                gaps[index] = gap with
                {
                    EndExclusive = branchStart,
                    State = resolutionWasTruncated ? ActivityGapState.Unresolved : gap.State,
                    ResolvedAt = resolutionWasTruncated ? null : gap.ResolvedAt
                };
            }
        }
    }
}

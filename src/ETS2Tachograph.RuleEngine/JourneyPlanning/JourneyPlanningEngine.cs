using ETS2Tachograph.Core.Enums;

namespace ETS2Tachograph.RuleEngine.JourneyPlanning;

public sealed class JourneyPlanningEngine
{
    private const int ContinuousDrivingLimit = 270;
    private const int NormalDailyDrivingLimit = 540;
    private const int ExtendedDailyDrivingLimit = 600;
    private const int WeeklyDrivingLimit = 3_360;
    private const int BiweeklyDrivingLimit = 5_400;
    private const int RegularDailyRest = 660;
    private const int ReducedDailyRest = 540;
    private const int ReducedWeeklyRest = 1_440;
    private const int RegularWeeklyRest = 2_700;
    private const int RegulatoryWeek = 10_080;

    public JourneyPlanResult Plan(JourneyPlanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Snapshot);
        ArgumentNullException.ThrowIfNull(request.Limits);

        var validation = Validate(request);
        if (validation is not null)
        {
            return validation;
        }

        var source = request.Snapshot.Evaluation.State;
        var state = new JourneyPlanningState
        {
            CurrentGameMinute = request.Snapshot.StartGameMinute,
            RemainingDriveMinutes = request.RemainingDriveMinutes,
            ContinuousDrivingMinutes = source.ContinuousDrivingMinutes,
            DailyDrivingMinutes = source.DailyDrivingMinutes,
            WeeklyDrivingMinutes = source.WeeklyDrivingMinutes,
            PreviousWeekDrivingMinutes = source.PreviousWeekDrivingMinutes,
            DailyExtensionsUsed = source.DailyExtensionsUsedThisWeek,
            ReducedDailyRestsUsed = source.ReducedDailyRestsSinceWeeklyRest,
            DailyRestCompletionDeadline = checked(
                request.Snapshot.StartGameMinute + source.MinutesUntilDailyRestDeadline),
            WeeklyRestStartDeadline = checked(
                request.Snapshot.StartGameMinute + source.MinutesUntilWeeklyRestDeadline),
            MultiManningActive = request.Snapshot.MultiManningActive,
            ReducedWeeklyRestSupported = !source.PendingRestAllocation,
            ExistingSplitBreakAvailable =
                source.CurrentContinuousBreakMinutes is >= 15 and < 45
        };
        var usage = new MutableUsage
        {
            UsedThirtyHourWindow = request.Snapshot.MultiManningActive
        };
        var confidence = Confidence(request);
        var warnings = Warnings(request, confidence);

        while (state.RemainingDriveMinutes > 0)
        {
            if (LimitReached(request, state))
            {
                return Terminal(
                    request,
                    state,
                    JourneyPlanStatus.CalculationLimitReached,
                    confidence,
                    usage,
                    warnings);
            }

            state.VisitedStates++;
            if (!state.SeenStates.Add(StateKey(state)))
            {
                return Terminal(
                    request,
                    state,
                    JourneyPlanStatus.NoLegalContinuation,
                    confidence,
                    usage,
                    warnings);
            }

            if (state.CurrentGameMinute >= state.WeeklyRestStartDeadline)
            {
                var reducedSupported = state.ReducedWeeklyRestSupported;
                if (!AddRest(
                    request,
                    state,
                    reducedSupported ? ReducedWeeklyRest : RegularWeeklyRest,
                    JourneyPlanSegmentType.WeeklyRest,
                    JourneyPlanSegmentReason.WeeklyRestRequirement,
                    usesException: reducedSupported))
                {
                    return Terminal(request, state, JourneyPlanStatus.CalculationLimitReached,
                        confidence, usage, warnings);
                }
                usage.UsedReducedWeeklyRest |= reducedSupported;
                usage.UsedRegularWeeklyRest |= !reducedSupported;
                if (reducedSupported)
                {
                    usage.RecognizedCompensationObligationMinutes +=
                        RegularWeeklyRest - ReducedWeeklyRest;
                }
                if (!reducedSupported)
                {
                    warnings.Add(new(
                        JourneyPlanWarningCode.ReducedWeeklyRestUnavailable,
                        JourneyPlanWarningSeverity.Limitation));
                }
                ResetAfterWeeklyRest(state);
                continue;
            }

            var weeklyCapacity = WeeklyDrivingLimit - state.WeeklyDrivingMinutes;
            var biweeklyCapacity = BiweeklyDrivingLimit -
                state.WeeklyDrivingMinutes -
                state.PreviousWeekDrivingMinutes;
            if (weeklyCapacity <= 0 || biweeklyCapacity <= 0)
            {
                var reason = weeklyCapacity <= 0
                    ? JourneyPlanSegmentReason.WaitForNewRegulatoryWeek
                    : JourneyPlanSegmentReason.WaitForBiweeklyCapacity;
                usage.ReachedWeeklyDrivingLimit |= weeklyCapacity <= 0;
                usage.ReachedBiweeklyDrivingLimit |= biweeklyCapacity <= 0;
                usage.UsedCalendarWait = true;
                var wait = checked((int)(NextWeekBoundary(
                    state.CurrentGameMinute,
                    request.Snapshot.WeekEpochOffsetDays) -
                    state.CurrentGameMinute));
                if (!TryAdd(
                        request,
                        state,
                        JourneyPlanSegmentType.CalendarWait,
                        wait,
                        reason,
                        DriverActivity.BreakOrRest))
                {
                    return Terminal(
                        request,
                        state,
                        JourneyPlanStatus.CalculationLimitReached,
                        confidence,
                        usage,
                        warnings);
                }
                ApplyRestEffects(state, wait);
                state.PreviousWeekDrivingMinutes = state.WeeklyDrivingMinutes;
                state.WeeklyDrivingMinutes = 0;
                state.DailyExtensionsUsed = 0;
                continue;
            }

            var dailyRestMinutes = ChooseDailyRestDuration(state);
            var latestDailyRestStart = state.DailyRestCompletionDeadline - dailyRestMinutes;
            if (state.CurrentGameMinute >= latestDailyRestStart)
            {
                if (!TryAddDailyRest(request, state, dailyRestMinutes, usage))
                {
                    return Terminal(
                        request,
                        state,
                        JourneyPlanStatus.NoLegalContinuation,
                        confidence,
                        usage,
                        warnings);
                }
                continue;
            }

            var dailyLimit = ChooseDailyDrivingLimit(state);
            var continuousCapacity = ContinuousDrivingLimit - state.ContinuousDrivingMinutes;
            var dailyCapacity = dailyLimit - state.DailyDrivingMinutes;
            if (continuousCapacity <= 0)
            {
                var split = state.ExistingSplitBreakAvailable;
                var breakMinutes = split ? 30 : 45;
                if (!TryAdd(
                        request,
                        state,
                        JourneyPlanSegmentType.Break,
                        breakMinutes,
                        split
                            ? JourneyPlanSegmentReason.SplitBreakCompletion
                            : JourneyPlanSegmentReason.ContinuousDrivingBreak,
                        DriverActivity.BreakOrRest))
                {
                    return Terminal(request, state, JourneyPlanStatus.CalculationLimitReached,
                        confidence, usage, warnings);
                }
                state.ContinuousDrivingMinutes = 0;
                state.ExistingSplitBreakAvailable = false;
                usage.UsedExistingFifteenMinuteBreak |= split;
                continue;
            }

            if (dailyCapacity <= 0)
            {
                if (!TryAddDailyRest(request, state, dailyRestMinutes, usage))
                {
                    return Terminal(request, state, JourneyPlanStatus.NoLegalContinuation,
                        confidence, usage, warnings);
                }
                continue;
            }

            var driveMinutes = checked((int)Math.Min(
                state.RemainingDriveMinutes,
                Math.Min(
                    continuousCapacity,
                    Math.Min(
                        dailyCapacity,
                        Math.Min(
                            weeklyCapacity,
                            Math.Min(
                                biweeklyCapacity,
                                latestDailyRestStart - state.CurrentGameMinute))))));
            if (driveMinutes <= 0)
            {
                return Terminal(request, state, JourneyPlanStatus.NoLegalContinuation,
                    confidence, usage, warnings);
            }
            if (!TryAdd(
                    request,
                    state,
                    JourneyPlanSegmentType.Drive,
                    driveMinutes,
                    JourneyPlanSegmentReason.RemainingRouteDrive,
                    DriverActivity.Driving))
            {
                return Terminal(request, state, JourneyPlanStatus.CalculationLimitReached,
                    confidence, usage, warnings);
            }

            var dailyDrivingBeforeSegment = state.DailyDrivingMinutes;
            state.RemainingDriveMinutes -= driveMinutes;
            state.ContinuousDrivingMinutes += driveMinutes;
            state.DailyDrivingMinutes += driveMinutes;
            state.WeeklyDrivingMinutes += driveMinutes;
            if (dailyDrivingBeforeSegment <= NormalDailyDrivingLimit &&
                state.DailyDrivingMinutes > NormalDailyDrivingLimit)
            {
                usage.DailyDrivingExtensionsUsed++;
            }
        }

        state.ArrivalGameMinute = state.CurrentGameMinute;
        if (request.OperationalBufferMinutes > 0)
        {
            var restMinutes = ChooseDailyRestDuration(state);
            if (state.CurrentGameMinute + request.OperationalBufferMinutes + restMinutes >
                state.DailyRestCompletionDeadline)
            {
                if (!TryAddDailyRest(request, state, restMinutes, usage))
                {
                    return Terminal(request, state, JourneyPlanStatus.NoLegalContinuation,
                        confidence, usage, warnings);
                }
            }

            if (!TryAdd(
                    request,
                    state,
                    JourneyPlanSegmentType.OtherWork,
                    request.OperationalBufferMinutes,
                    JourneyPlanSegmentReason.OperationalBufferAfterArrival,
                    DriverActivity.OtherWork))
            {
                return Terminal(request, state, JourneyPlanStatus.CalculationLimitReached,
                    confidence, usage, warnings);
            }
        }

        var elapsed = checked((int)(state.CurrentGameMinute - request.Snapshot.StartGameMinute));
        var margin = request.DeliveryWindowMinutes - elapsed;
        return new JourneyPlanResult(
            margin >= 0 ? JourneyPlanStatus.MeetsDeadline : JourneyPlanStatus.MissesDeadline,
            confidence,
            request.Snapshot.StartGameMinute,
            state.ArrivalGameMinute,
            state.CurrentGameMinute,
            elapsed,
            margin,
            state.Segments.ToArray(),
            warnings.ToArray(),
            usage.ToContract(),
            request.Snapshot.Identity);
    }

    private static JourneyPlanResult? Validate(JourneyPlanRequest request)
    {
        if (request.RemainingDriveMinutes < 0 ||
            request.DeliveryWindowMinutes < 0 ||
            request.OperationalBufferMinutes < 0 ||
            request.Limits.MaximumSegments <= 0 ||
            request.Limits.MaximumElapsedMinutes <= 0 ||
            request.Limits.MaximumVisitedStates <= 0 ||
            request.Snapshot.WeekEpochOffsetDays is < -6 or > 6 ||
            request.Snapshot.Evaluation is null)
        {
            return Empty(request, JourneyPlanStatus.InsufficientData);
        }

        if (request.Snapshot.Gaps.Any(gap =>
                gap.State == ActivityGapState.Unresolved &&
                gap.Reason == ActivityGapReason.CardRemoved))
        {
            return Empty(request, JourneyPlanStatus.BlockedByGap);
        }

        return null;
    }

    private static JourneyPlanConfidence Confidence(JourneyPlanRequest request)
    {
        if (!request.Snapshot.TelemetryAvailable)
        {
            return JourneyPlanConfidence.BasedOnLastSavedState;
        }

        return request.Snapshot.Gaps.Any(gap =>
            gap.State == ActivityGapState.Unresolved &&
            gap.Reason == ActivityGapReason.ForwardTimeJump)
                ? JourneyPlanConfidence.BasedOnIncompleteHistory
                : JourneyPlanConfidence.VerifiedByCurrentRuleModel;
    }

    private static List<JourneyPlanWarning> Warnings(
        JourneyPlanRequest request,
        JourneyPlanConfidence confidence)
    {
        var result = new List<JourneyPlanWarning>();
        if (confidence == JourneyPlanConfidence.BasedOnLastSavedState)
        {
            result.Add(new(
                JourneyPlanWarningCode.LastSavedState,
                JourneyPlanWarningSeverity.Caution));
        }
        else if (confidence == JourneyPlanConfidence.BasedOnIncompleteHistory)
        {
            result.Add(new(
                JourneyPlanWarningCode.IncompleteHistory,
                JourneyPlanWarningSeverity.Caution));
        }

        if (request.Snapshot.MultiManningActive)
        {
            result.Add(new(
                JourneyPlanWarningCode.MultiManningPlanningUnsupported,
                JourneyPlanWarningSeverity.Information));
        }

        return result;
    }

    private static bool TryAddDailyRest(
        JourneyPlanRequest request,
        JourneyPlanningState state,
        int duration,
        MutableUsage usage)
    {
        if (state.CurrentGameMinute + duration > state.DailyRestCompletionDeadline)
        {
            return false;
        }

        if (!TryAdd(
                request,
                state,
                JourneyPlanSegmentType.DailyRest,
                duration,
                JourneyPlanSegmentReason.DailyRestDeadline,
                DriverActivity.BreakOrRest,
                usesException: duration == ReducedDailyRest))
        {
            return false;
        }

        if (state.DailyDrivingMinutes > NormalDailyDrivingLimit)
        {
            state.DailyExtensionsUsed++;
        }
        if (duration == ReducedDailyRest)
        {
            state.ReducedDailyRestsUsed++;
            usage.ReducedDailyRestsUsed++;
        }
        state.ContinuousDrivingMinutes = 0;
        state.DailyDrivingMinutes = 0;
        state.DailyRestCompletionDeadline = checked(
            state.CurrentGameMinute +
            (state.MultiManningActive ? 1_800 : 1_440));
        return true;
    }

    private static bool AddRest(
        JourneyPlanRequest request,
        JourneyPlanningState state,
        int duration,
        JourneyPlanSegmentType type,
        JourneyPlanSegmentReason reason,
        bool usesException)
    {
        if (!TryAdd(
                request,
                state,
                type,
                duration,
                reason,
                DriverActivity.BreakOrRest,
                usesException))
        {
            return false;
        }
        ApplyRestEffects(state, duration);
        return true;
    }

    private static void ApplyRestEffects(JourneyPlanningState state, int duration)
    {
        if (duration >= 45)
        {
            state.ContinuousDrivingMinutes = 0;
        }
        if (duration >= ReducedDailyRest)
        {
            if (state.DailyDrivingMinutes > NormalDailyDrivingLimit)
            {
                state.DailyExtensionsUsed++;
            }
            state.DailyDrivingMinutes = 0;
            state.DailyRestCompletionDeadline = checked(
                state.CurrentGameMinute +
                (state.MultiManningActive ? 1_800 : 1_440));
        }
        if (duration >= RegularWeeklyRest ||
            (duration >= ReducedWeeklyRest && state.ReducedWeeklyRestSupported))
        {
            ResetAfterWeeklyRest(state);
        }
    }

    private static void ResetAfterWeeklyRest(JourneyPlanningState state)
    {
        state.ReducedDailyRestsUsed = 0;
        state.WeeklyRestStartDeadline = checked(state.CurrentGameMinute + 8_640);
    }

    private static bool TryAdd(
        JourneyPlanRequest request,
        JourneyPlanningState state,
        JourneyPlanSegmentType type,
        int duration,
        JourneyPlanSegmentReason reason,
        DriverActivity activity,
        bool usesException = false)
    {
        if (duration <= 0 ||
            state.Segments.Count >= request.Limits.MaximumSegments ||
            state.CurrentGameMinute - request.Snapshot.StartGameMinute + duration >
            request.Limits.MaximumElapsedMinutes)
        {
            return false;
        }

        var start = state.CurrentGameMinute;
        state.CurrentGameMinute = checked(start + duration);
        state.Segments.Add(new(
            type,
            request.Snapshot.DriverSlot,
            start,
            state.CurrentGameMinute,
            duration,
            reason,
            activity,
            usesException,
            null));
        return true;
    }

    private static bool LimitReached(
        JourneyPlanRequest request,
        JourneyPlanningState state) =>
        state.Segments.Count >= request.Limits.MaximumSegments ||
        state.VisitedStates >= request.Limits.MaximumVisitedStates ||
        state.CurrentGameMinute - request.Snapshot.StartGameMinute >=
        request.Limits.MaximumElapsedMinutes;

    private static int ChooseDailyRestDuration(JourneyPlanningState state)
    {
        Span<int> candidates = stackalloc int[2];
        var count = 0;
        if (state.ReducedDailyRestsUsed < 3)
        {
            candidates[count++] = ReducedDailyRest;
        }
        candidates[count++] = RegularDailyRest;

        var selected = candidates[0];
        for (var index = 1; index < count; index++)
        {
            if (candidates[index] < selected)
            {
                selected = candidates[index];
            }
        }
        return selected;
    }

    private static int ChooseDailyDrivingLimit(JourneyPlanningState state)
    {
        Span<int> candidates = stackalloc int[2];
        var count = 0;
        candidates[count++] = NormalDailyDrivingLimit;
        if (state.DailyExtensionsUsed < 2)
        {
            candidates[count++] = ExtendedDailyDrivingLimit;
        }

        var selected = candidates[0];
        for (var index = 1; index < count; index++)
        {
            if (candidates[index] > selected)
            {
                selected = candidates[index];
            }
        }
        return selected;
    }

    private static JourneyPlanningStateKey StateKey(JourneyPlanningState state) => new(
        state.CurrentGameMinute,
        state.RemainingDriveMinutes,
        state.ContinuousDrivingMinutes,
        state.DailyDrivingMinutes,
        state.WeeklyDrivingMinutes,
        state.PreviousWeekDrivingMinutes,
        state.DailyExtensionsUsed,
        state.ReducedDailyRestsUsed,
        state.DailyRestCompletionDeadline,
        state.WeeklyRestStartDeadline);

    private static long NextWeekBoundary(long minute, int offsetDays)
    {
        var offsetMinutes = checked((long)offsetDays * 1_440);
        var calibrated = checked(minute + offsetMinutes);
        var quotient = Math.DivRem(calibrated, RegulatoryWeek, out var remainder);
        if (remainder < 0)
        {
            quotient--;
        }
        return checked(((quotient + 1) * RegulatoryWeek) - offsetMinutes);
    }

    private static JourneyPlanResult Terminal(
        JourneyPlanRequest request,
        JourneyPlanningState state,
        JourneyPlanStatus status,
        JourneyPlanConfidence confidence,
        MutableUsage usage,
        IReadOnlyList<JourneyPlanWarning> warnings)
    {
        var elapsed = checked((int)(state.CurrentGameMinute - request.Snapshot.StartGameMinute));
        return new(
            status,
            confidence,
            request.Snapshot.StartGameMinute,
            state.ArrivalGameMinute,
            null,
            elapsed,
            request.DeliveryWindowMinutes - elapsed,
            state.Segments.ToArray(),
            warnings.ToArray(),
            usage.ToContract(),
            request.Snapshot.Identity);
    }

    private static JourneyPlanResult Empty(
        JourneyPlanRequest request,
        JourneyPlanStatus status) => new(
        status,
        JourneyPlanConfidence.VerifiedByCurrentRuleModel,
        request.Snapshot.StartGameMinute,
        null,
        null,
        0,
        request.DeliveryWindowMinutes,
        [],
        [],
        JourneyPlanUsageSummary.Empty,
        request.Snapshot.Identity);

    private sealed class MutableUsage
    {
        internal int DailyDrivingExtensionsUsed { get; set; }
        internal int ReducedDailyRestsUsed { get; set; }
        internal bool UsedReducedWeeklyRest { get; set; }
        internal bool UsedRegularWeeklyRest { get; set; }
        internal int RecognizedCompensationObligationMinutes { get; set; }
        internal bool UsedExistingFifteenMinuteBreak { get; set; }
        internal bool UsedThirtyHourWindow { get; set; }
        internal bool UsedCalendarWait { get; set; }
        internal bool ReachedWeeklyDrivingLimit { get; set; }
        internal bool ReachedBiweeklyDrivingLimit { get; set; }

        internal JourneyPlanUsageSummary ToContract() => new(
            DailyDrivingExtensionsUsed,
            ReducedDailyRestsUsed,
            UsedReducedWeeklyRest,
            UsedRegularWeeklyRest,
            RecognizedCompensationObligationMinutes,
            UsedExistingFifteenMinuteBreak,
            UsedThirtyHourWindow,
            UsedCalendarWait,
            ReachedWeeklyDrivingLimit,
            ReachedBiweeklyDrivingLimit);
    }
}

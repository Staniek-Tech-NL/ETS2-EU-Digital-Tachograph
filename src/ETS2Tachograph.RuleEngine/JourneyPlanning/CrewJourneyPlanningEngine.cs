using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.RuleEngine.JourneyPlanning;

public sealed class CrewJourneyPlanningEngine
{
    private const int ContinuousDrivingLimit = 270;
    private const int QualifiedBreak = 45;
    private const int NormalDailyDrivingLimit = 540;
    private const int ExtendedDailyDrivingLimit = 600;
    private const int WeeklyDrivingLimit = 3_360;
    private const int BiweeklyDrivingLimit = 5_400;
    private const int ReducedDailyRest = 540;
    private const int RegularDailyRest = 660;
    private const int MaximumReducedDailyRests = 3;
    private const int RegularWeeklyRest = 2_700;
    private const int WeeklyRestWindow = 8_640;
    private const int MultiManningDailyWindow = 1_800;

    public CrewJourneyPlanResult Plan(CrewJourneyPlanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);

        var start = request.Snapshot.StartGameMinute;
        var slot1 = DriverState.From(
            request.Snapshot.Slot1,
            start,
            request.Snapshot.WeekEpochOffsetDays);
        var slot2 = DriverState.From(
            request.Snapshot.Slot2,
            start,
            request.Snapshot.WeekEpochOffsetDays);
        if (request.Mode != JourneyPlanningMode.MultiManningCrew ||
            !request.Snapshot.MultiManningActive)
        {
            return Result(
                JourneyPlanStatus.UnsupportedScenario,
                request,
                start,
                arrival: null,
                [],
                slot1,
                slot2,
                [new JourneyPlanWarning(
                    JourneyPlanWarningCode.MultiManningPlanningUnsupported,
                    JourneyPlanWarningSeverity.Limitation,
                    "MultiManningCrew requires two confirmed inserted cards.")]);
        }

        if (AllGaps(request).Any(gap =>
                gap.State == ActivityGapState.Unresolved &&
                gap.Reason == ActivityGapReason.CardRemoved))
        {
            return Result(
                JourneyPlanStatus.BlockedByGap,
                request,
                start,
                arrival: null,
                [],
                slot1,
                slot2);
        }

        var now = start;
        var remaining = request.RemainingDriveMinutes;
        var currentSlot = request.InitialDrivingSlot;
        var segments = new List<CrewJourneyPlanSegment>();
        var visited = 0;
        var currentWeekIndex = WeekIndex(
            now,
            request.Snapshot.WeekEpochOffsetDays);

        while (remaining > 0)
        {
            if (++visited > request.Limits.MaximumVisitedStates ||
                segments.Count >= request.Limits.MaximumSegments ||
                now - start >= request.Limits.MaximumElapsedMinutes)
            {
                return Result(
                    JourneyPlanStatus.CalculationLimitReached,
                    request,
                    now,
                    arrival: null,
                    segments,
                    slot1,
                    slot2);
            }

            var dailyRestDuration = ChooseDailyRestDuration(slot1, slot2);
            var earliestRestDeadline = Math.Min(
                slot1.DailyRestDeadline,
                slot2.DailyRestDeadline);
            if (now + dailyRestDuration > earliestRestDeadline)
                return Result(
                    JourneyPlanStatus.NoLegalContinuation,
                    request,
                    now,
                    arrival: null,
                    segments,
                    slot1,
                    slot2);

            var earliestWeeklyRestDeadline = Math.Min(
                slot1.WeeklyRestDeadline,
                slot2.WeeklyRestDeadline);
            if (now >= earliestWeeklyRestDeadline)
            {
                if (!FitsLimits(request, start, now, RegularWeeklyRest, segments.Count))
                    return Result(
                        JourneyPlanStatus.CalculationLimitReached,
                        request,
                        now,
                        arrival: null,
                        segments,
                        slot1,
                        slot2);
                AddWeeklyRest(
                    segments,
                    ref now,
                    JourneyPlanSegmentReason.WeeklyRestRequirement,
                    slot1,
                    slot2);
                AdvanceWeeks(
                    ref currentWeekIndex,
                    now,
                    request.Snapshot.WeekEpochOffsetDays,
                    slot1,
                    slot2);
                continue;
            }

            if (now >= earliestRestDeadline - dailyRestDuration)
            {
                var useWeeklyRest =
                    now + dailyRestDuration > earliestWeeklyRestDeadline;
                var restDuration = useWeeklyRest
                    ? RegularWeeklyRest
                    : dailyRestDuration;
                if (!FitsLimits(request, start, now, restDuration, segments.Count))
                    return Result(
                        JourneyPlanStatus.CalculationLimitReached,
                        request,
                        now,
                        arrival: null,
                        segments,
                        slot1,
                        slot2);
                if (useWeeklyRest)
                    AddWeeklyRest(
                        segments,
                        ref now,
                        JourneyPlanSegmentReason.WeeklyRestRequirement,
                        slot1,
                        slot2);
                else
                    AddStationaryRest(
                        segments,
                        ref now,
                        dailyRestDuration,
                        JourneyPlanSegmentReason.DailyRestDeadline,
                        slot1,
                        slot2);
                AdvanceWeeks(
                    ref currentWeekIndex,
                    now,
                    request.Snapshot.WeekEpochOffsetDays,
                    slot1,
                    slot2);
                continue;
            }

            var current = currentSlot == 1 ? slot1 : slot2;
            var other = currentSlot == 1 ? slot2 : slot1;
            var capacity = CrewDrivingCapacity(
                current,
                slot1,
                slot2,
                now);
            if (capacity <= 0)
            {
                var otherCapacity = CrewDrivingCapacity(
                    other,
                    slot1,
                    slot2,
                    now);
                if (otherCapacity > 0)
                {
                    currentSlot = other.Slot;
                    continue;
                }

                if (NeedsContinuousBreak(current) || NeedsContinuousBreak(other))
                {
                    var breakDuration = BreakCompletionDuration(current, other);
                    if (!FitsLimits(request, start, now, breakDuration, segments.Count))
                        return Result(
                            JourneyPlanStatus.CalculationLimitReached,
                            request,
                            now,
                            arrival: null,
                            segments,
                            slot1,
                            slot2);
                    AddStationaryBreak(segments, ref now, breakDuration, slot1, slot2);
                    AdvanceWeeks(
                        ref currentWeekIndex,
                        now,
                        request.Snapshot.WeekEpochOffsetDays,
                        slot1,
                        slot2);
                    currentSlot = SelectDriver(slot1, slot2, currentSlot, now);
                    continue;
                }

                if (WeeklyCapacity(slot1) <= 0 && WeeklyCapacity(slot2) <= 0)
                {
                    var calendarWait = CalendarWaitDuration(
                        now,
                        request.Snapshot.WeekEpochOffsetDays);
                    if (!FitsLimits(request, start, now, calendarWait, segments.Count))
                        return Result(
                            JourneyPlanStatus.CalculationLimitReached,
                            request,
                            now,
                            arrival: null,
                            segments,
                            slot1,
                            slot2);
                    AddCalendarWait(request, segments, ref now, slot1, slot2);
                    AdvanceWeeks(
                        ref currentWeekIndex,
                        now,
                        request.Snapshot.WeekEpochOffsetDays,
                        slot1,
                        slot2);
                    currentSlot = SelectDriver(slot1, slot2, currentSlot, now);
                    continue;
                }

                dailyRestDuration = ChooseDailyRestDuration(slot1, slot2);
                earliestRestDeadline = Math.Min(
                    slot1.DailyRestDeadline,
                    slot2.DailyRestDeadline);
                if (now + dailyRestDuration > earliestRestDeadline)
                    return Result(
                        JourneyPlanStatus.NoLegalContinuation,
                        request,
                        now,
                        arrival: null,
                        segments,
                        slot1,
                        slot2);
                var useWeeklyRest =
                    now + dailyRestDuration > earliestWeeklyRestDeadline;
                var restDuration = useWeeklyRest
                    ? RegularWeeklyRest
                    : dailyRestDuration;
                if (!FitsLimits(request, start, now, restDuration, segments.Count))
                    return Result(
                        JourneyPlanStatus.CalculationLimitReached,
                        request,
                        now,
                        arrival: null,
                        segments,
                        slot1,
                        slot2);
                if (useWeeklyRest)
                    AddWeeklyRest(
                        segments,
                        ref now,
                        JourneyPlanSegmentReason.WeeklyRestRequirement,
                        slot1,
                        slot2);
                else
                    AddStationaryRest(
                        segments,
                        ref now,
                        dailyRestDuration,
                        JourneyPlanSegmentReason.DailyDrivingLimit,
                        slot1,
                        slot2);
                AdvanceWeeks(
                    ref currentWeekIndex,
                    now,
                    request.Snapshot.WeekEpochOffsetDays,
                    slot1,
                    slot2);
                currentSlot = SelectDriver(slot1, slot2, currentSlot, now);
                continue;
            }

            var duration = (int)Math.Min(remaining, capacity);
            if (!FitsLimits(request, start, now, duration, segments.Count))
                return Result(
                    JourneyPlanStatus.CalculationLimitReached,
                    request,
                    now,
                    arrival: null,
                    segments,
                    slot1,
                    slot2);
            var passengerQualified = QualifyPassengerBreak(other, duration);
            var end = checked(now + duration);
            var previousDrivingSlot = segments
                .LastOrDefault(segment => segment.DrivingSlot is not null)?
                .DrivingSlot;
            AddOrMergeDriveSegment(segments, new CrewJourneyPlanSegment(
                now,
                end,
                currentSlot,
                currentSlot == 1 ? DriverActivity.Driving : DriverActivity.Availability,
                currentSlot == 2 ? DriverActivity.Driving : DriverActivity.Availability,
                Slot1BreakQualifiedInMotion: currentSlot == 2 && passengerQualified,
                Slot2BreakQualifiedInMotion: currentSlot == 1 && passengerQualified,
                previousDrivingSlot is not null && previousDrivingSlot != currentSlot
                    ? JourneyPlanSegmentReason.CrewDriverChange
                    : JourneyPlanSegmentReason.RemainingRouteDrive));

            ApplyDriving(current, duration);
            now = end;
            AdvanceWeeks(
                ref currentWeekIndex,
                now,
                request.Snapshot.WeekEpochOffsetDays,
                slot1,
                slot2);
            remaining -= duration;
            if (remaining > 0 &&
                CrewDrivingCapacity(current, slot1, slot2, now) <= 0)
                currentSlot = other.Slot;
        }

        var arrival = now;
        if (request.OperationalBufferMinutes > 0)
        {
            var dailyRestDuration = ChooseDailyRestDuration(slot1, slot2);
            var earliestRestDeadline = Math.Min(
                slot1.DailyRestDeadline,
                slot2.DailyRestDeadline);
            var earliestWeeklyRestDeadline = Math.Min(
                slot1.WeeklyRestDeadline,
                slot2.WeeklyRestDeadline);
            if (now + request.OperationalBufferMinutes >
                earliestWeeklyRestDeadline)
            {
                if (!FitsLimits(request, start, now, RegularWeeklyRest, segments.Count))
                    return Result(
                        JourneyPlanStatus.CalculationLimitReached,
                        request,
                        now,
                        arrival: null,
                        segments,
                        slot1,
                        slot2);
                AddWeeklyRest(
                    segments,
                    ref now,
                    JourneyPlanSegmentReason.WeeklyRestRequirement,
                    slot1,
                    slot2);
                AdvanceWeeks(
                    ref currentWeekIndex,
                    now,
                    request.Snapshot.WeekEpochOffsetDays,
                    slot1,
                    slot2);
                earliestRestDeadline = Math.Min(
                    slot1.DailyRestDeadline,
                    slot2.DailyRestDeadline);
            }
            if (now + request.OperationalBufferMinutes + dailyRestDuration >
                earliestRestDeadline)
            {
                if (now + dailyRestDuration > earliestRestDeadline)
                    return Result(
                        JourneyPlanStatus.NoLegalContinuation,
                        request,
                        now,
                        arrival: null,
                        segments,
                        slot1,
                        slot2);
                if (!FitsLimits(request, start, now, dailyRestDuration, segments.Count))
                    return Result(
                        JourneyPlanStatus.CalculationLimitReached,
                        request,
                        now,
                        arrival: null,
                        segments,
                        slot1,
                        slot2);
                AddStationaryRest(
                    segments,
                    ref now,
                    dailyRestDuration,
                    JourneyPlanSegmentReason.DailyRestDeadline,
                    slot1,
                    slot2);
                AdvanceWeeks(
                    ref currentWeekIndex,
                    now,
                    request.Snapshot.WeekEpochOffsetDays,
                    slot1,
                    slot2);
            }
            if (!FitsLimits(
                    request,
                    start,
                    now,
                    request.OperationalBufferMinutes,
                    segments.Count))
                return Result(
                    JourneyPlanStatus.CalculationLimitReached,
                    request,
                    now,
                    arrival: null,
                    segments,
                    slot1,
                    slot2);
            var end = checked(now + request.OperationalBufferMinutes);
            segments.Add(new CrewJourneyPlanSegment(
                now,
                end,
                DrivingSlot: null,
                DriverActivity.OtherWork,
                DriverActivity.OtherWork,
                Slot1BreakQualifiedInMotion: false,
                Slot2BreakQualifiedInMotion: false,
                JourneyPlanSegmentReason.OperationalBufferAfterArrival));
            slot1.BreakMinutes = 0;
            slot2.BreakMinutes = 0;
            now = end;
            AdvanceWeeks(
                ref currentWeekIndex,
                now,
                request.Snapshot.WeekEpochOffsetDays,
                slot1,
                slot2);
        }

        var elapsed = checked((int)(now - start));
        var status = elapsed <= request.DeliveryWindowMinutes
            ? JourneyPlanStatus.MeetsDeadline
            : JourneyPlanStatus.MissesDeadline;
        return Result(status, request, now, arrival, segments, slot1, slot2);
    }

    private static long DrivingCapacity(DriverState driver)
    {
        var dailyLimit = driver.DailyExtensionsUsed < 2
            ? ExtendedDailyDrivingLimit
            : NormalDailyDrivingLimit;
        return new long[]
        {
            ContinuousDrivingLimit - driver.ContinuousDriving,
            dailyLimit - driver.DailyDriving,
            WeeklyDrivingLimit - driver.WeeklyDriving,
            BiweeklyDrivingLimit - driver.WeeklyDriving - driver.PreviousWeekDriving
        }.Min();
    }

    private static long CrewDrivingCapacity(
        DriverState driver,
        DriverState slot1,
        DriverState slot2,
        long now)
    {
        var dailyRestDuration = ChooseDailyRestDuration(slot1, slot2);
        var minutesUntilLatestRestStart = Math.Min(
            slot1.DailyRestDeadline,
            slot2.DailyRestDeadline) - dailyRestDuration - now;
        var minutesUntilWeeklyRest = Math.Min(
            slot1.WeeklyRestDeadline,
            slot2.WeeklyRestDeadline) - now;
        var minutesUntilWeekBoundary = CalendarWaitDuration(
            now,
            slot1.WeekEpochOffsetDays);
        return new long[]
        {
            DrivingCapacity(driver),
            minutesUntilLatestRestStart,
            minutesUntilWeeklyRest,
            minutesUntilWeekBoundary
        }.Min();
    }

    private static bool NeedsContinuousBreak(DriverState driver) =>
        driver.ContinuousDriving >= ContinuousDrivingLimit;

    private static long WeeklyCapacity(DriverState driver) => Math.Min(
        WeeklyDrivingLimit - driver.WeeklyDriving,
        BiweeklyDrivingLimit - driver.WeeklyDriving - driver.PreviousWeekDriving);

    private static int BreakCompletionDuration(DriverState first, DriverState second)
    {
        var candidates = new List<long>();
        if (NeedsContinuousBreak(first))
            candidates.Add(QualifiedBreak - Math.Min(QualifiedBreak, first.BreakMinutes));
        if (NeedsContinuousBreak(second))
            candidates.Add(QualifiedBreak - Math.Min(QualifiedBreak, second.BreakMinutes));
        return checked((int)Math.Max(1, candidates.Min()));
    }

    private static bool QualifyPassengerBreak(DriverState passenger, int duration)
    {
        var minutesNeeded = Math.Max(0, QualifiedBreak - passenger.BreakMinutes);
        if (duration < minutesNeeded)
        {
            passenger.BreakMinutes += duration;
            return false;
        }

        passenger.ContinuousDriving = 0;
        passenger.BreakMinutes = duration == minutesNeeded
            ? QualifiedBreak
            : 0;
        return true;
    }

    private static void ApplyDriving(DriverState driver, int duration)
    {
        driver.BreakMinutes = 0;
        driver.ContinuousDriving += duration;
        driver.DailyDriving += duration;
        driver.WeeklyDriving += duration;
    }

    private static void AddOrMergeDriveSegment(
        IList<CrewJourneyPlanSegment> segments,
        CrewJourneyPlanSegment incoming)
    {
        if (segments.Count == 0)
        {
            segments.Add(incoming);
            return;
        }

        var previous = segments[^1];
        if (previous.EndGameMinute == incoming.StartGameMinute &&
            previous.DrivingSlot == incoming.DrivingSlot &&
            previous.Slot1Activity == incoming.Slot1Activity &&
            previous.Slot2Activity == incoming.Slot2Activity &&
            previous.Slot1BreakQualifiedInMotion ==
            incoming.Slot1BreakQualifiedInMotion &&
            previous.Slot2BreakQualifiedInMotion ==
            incoming.Slot2BreakQualifiedInMotion &&
            previous.Reason == incoming.Reason)
        {
            segments[^1] = previous with { EndGameMinute = incoming.EndGameMinute };
            return;
        }

        segments.Add(incoming);
    }

    private static void AddStationaryBreak(
        ICollection<CrewJourneyPlanSegment> segments,
        ref long now,
        int duration,
        DriverState slot1,
        DriverState slot2)
    {
        var end = checked(now + duration);
        segments.Add(new CrewJourneyPlanSegment(
            now,
            end,
            DrivingSlot: null,
            DriverActivity.BreakOrRest,
            DriverActivity.BreakOrRest,
            Slot1BreakQualifiedInMotion: false,
            Slot2BreakQualifiedInMotion: false,
            JourneyPlanSegmentReason.ContinuousDrivingBreak));
        ApplyStationaryBreak(slot1, duration);
        ApplyStationaryBreak(slot2, duration);
        now = end;
    }

    private static void ApplyStationaryBreak(DriverState driver, int duration)
    {
        driver.BreakMinutes = Math.Min(
            QualifiedBreak,
            driver.BreakMinutes + duration);
        if (driver.BreakMinutes >= QualifiedBreak)
            driver.ContinuousDriving = 0;
    }

    private static void AddStationaryRest(
        ICollection<CrewJourneyPlanSegment> segments,
        ref long now,
        int duration,
        JourneyPlanSegmentReason reason,
        DriverState slot1,
        DriverState slot2)
    {
        var end = checked(now + duration);
        segments.Add(new CrewJourneyPlanSegment(
            now,
            end,
            DrivingSlot: null,
            DriverActivity.BreakOrRest,
            DriverActivity.BreakOrRest,
            Slot1BreakQualifiedInMotion: false,
            Slot2BreakQualifiedInMotion: false,
            reason));
        ResetAfterDailyRest(slot1, end, duration);
        ResetAfterDailyRest(slot2, end, duration);
        now = end;
    }

    private static void AddWeeklyRest(
        ICollection<CrewJourneyPlanSegment> segments,
        ref long now,
        JourneyPlanSegmentReason reason,
        DriverState slot1,
        DriverState slot2)
    {
        var end = checked(now + RegularWeeklyRest);
        segments.Add(new CrewJourneyPlanSegment(
            now,
            end,
            DrivingSlot: null,
            DriverActivity.BreakOrRest,
            DriverActivity.BreakOrRest,
            Slot1BreakQualifiedInMotion: false,
            Slot2BreakQualifiedInMotion: false,
            reason));
        ApplyWeeklyRest(slot1, end);
        ApplyWeeklyRest(slot2, end);
        now = end;
    }

    private static void ApplyWeeklyRest(DriverState driver, long restEnd)
    {
        ResetAfterDailyRest(driver, restEnd, RegularDailyRest);
        driver.ReducedDailyRestsUsed = 0;
        driver.WeeklyRestDeadline = checked(restEnd + WeeklyRestWindow);
    }

    private static void ResetAfterDailyRest(
        DriverState driver,
        long restEnd,
        int duration)
    {
        if (driver.DailyDriving > NormalDailyDrivingLimit)
            driver.DailyExtensionsUsed++;
        if (duration < RegularDailyRest)
            driver.ReducedDailyRestsUsed++;
        driver.ContinuousDriving = 0;
        driver.DailyDriving = 0;
        driver.BreakMinutes = QualifiedBreak;
        driver.DailyRestDeadline = checked(restEnd + MultiManningDailyWindow);
    }

    private static int ChooseDailyRestDuration(
        DriverState slot1,
        DriverState slot2) =>
        slot1.ReducedDailyRestsUsed >= MaximumReducedDailyRests ||
        slot2.ReducedDailyRestsUsed >= MaximumReducedDailyRests
            ? RegularDailyRest
            : ReducedDailyRest;

    private static void AddCalendarWait(
        CrewJourneyPlanRequest request,
        ICollection<CrewJourneyPlanSegment> segments,
        ref long now,
        DriverState slot1,
        DriverState slot2)
    {
        var duration = CalendarWaitDuration(
            now,
            request.Snapshot.WeekEpochOffsetDays);
        var end = checked(now + duration);
        var reason = slot1.WeeklyDriving >= WeeklyDrivingLimit ||
                     slot2.WeeklyDriving >= WeeklyDrivingLimit
            ? JourneyPlanSegmentReason.WaitForNewRegulatoryWeek
            : JourneyPlanSegmentReason.WaitForBiweeklyCapacity;
        segments.Add(new CrewJourneyPlanSegment(
            now,
            end,
            DrivingSlot: null,
            DriverActivity.BreakOrRest,
            DriverActivity.BreakOrRest,
            Slot1BreakQualifiedInMotion: false,
            Slot2BreakQualifiedInMotion: false,
            reason));
        ApplyCalendarRest(slot1, end, duration);
        ApplyCalendarRest(slot2, end, duration);
        now = end;
    }

    private static void ApplyCalendarRest(
        DriverState driver,
        long restEnd,
        int restDuration)
    {
        if (restDuration >= RegularWeeklyRest)
        {
            ApplyWeeklyRest(driver, restEnd);
            return;
        }

        if (restDuration >= RegularDailyRest)
        {
            ResetAfterDailyRest(driver, restEnd, RegularDailyRest);
            if (restDuration >= 24 * 60)
                driver.ReducedDailyRestsUsed = 0;
            return;
        }

        if (restDuration >= ReducedDailyRest &&
            driver.ReducedDailyRestsUsed < MaximumReducedDailyRests)
        {
            ResetAfterDailyRest(driver, restEnd, ReducedDailyRest);
            return;
        }

        driver.BreakMinutes = Math.Min(
            QualifiedBreak,
            driver.BreakMinutes + restDuration);
        if (driver.BreakMinutes >= QualifiedBreak)
            driver.ContinuousDriving = 0;
    }

    private static int CalendarWaitDuration(long now, int offsetDays)
    {
        var boundary = GameWeek
            .From(new GameTime(now), offsetDays)
            .GetBounds()
            .EndGameMinuteExclusive;
        return checked((int)(boundary - now));
    }

    private static long WeekIndex(long minute, int offsetDays) =>
        GameWeek.From(new GameTime(minute), offsetDays).Index;

    private static void AdvanceWeeks(
        ref long currentWeekIndex,
        long now,
        int offsetDays,
        DriverState slot1,
        DriverState slot2)
    {
        var target = WeekIndex(now, offsetDays);
        while (currentWeekIndex < target)
        {
            AdvanceDriverWeek(slot1);
            AdvanceDriverWeek(slot2);
            currentWeekIndex++;
        }
    }

    private static void AdvanceDriverWeek(DriverState driver)
    {
        driver.PreviousWeekDriving = driver.WeeklyDriving;
        driver.WeeklyDriving = 0;
        driver.DailyExtensionsUsed = 0;
    }

    private static int SelectDriver(
        DriverState slot1,
        DriverState slot2,
        int preferred,
        long now)
    {
        var preferredDriver = preferred == 1 ? slot1 : slot2;
        if (CrewDrivingCapacity(preferredDriver, slot1, slot2, now) > 0)
            return preferred;
        return preferred == 1 ? 2 : 1;
    }

    private static CrewJourneyPlanResult Result(
        JourneyPlanStatus status,
        CrewJourneyPlanRequest request,
        long completion,
        long? arrival,
        IReadOnlyList<CrewJourneyPlanSegment> segments,
        DriverState slot1,
        DriverState slot2,
        IReadOnlyList<JourneyPlanWarning>? warnings = null)
    {
        var elapsed = checked((int)(completion - request.Snapshot.StartGameMinute));
        var confidence = Confidence(request);
        return new CrewJourneyPlanResult(
            status,
            confidence,
            request.Snapshot.StartGameMinute,
            arrival,
            arrival is null ? null : completion,
            elapsed,
            request.DeliveryWindowMinutes - elapsed,
            segments,
            warnings ?? Warnings(confidence),
            slot1.Summary(completion),
            slot2.Summary(completion));
    }

    private static JourneyPlanConfidence Confidence(
        CrewJourneyPlanRequest request)
    {
        if (!request.Snapshot.TelemetryAvailable)
            return JourneyPlanConfidence.BasedOnLastSavedState;

        return AllGaps(request).Any(gap =>
            gap.State == ActivityGapState.Unresolved &&
            gap.Reason == ActivityGapReason.ForwardTimeJump)
                ? JourneyPlanConfidence.BasedOnIncompleteHistory
                : JourneyPlanConfidence.VerifiedByCurrentRuleModel;
    }

    private static IReadOnlyList<JourneyPlanWarning> Warnings(
        JourneyPlanConfidence confidence) => confidence switch
    {
        JourneyPlanConfidence.BasedOnLastSavedState =>
        [
            new JourneyPlanWarning(
                JourneyPlanWarningCode.LastSavedState,
                JourneyPlanWarningSeverity.Caution)
        ],
        JourneyPlanConfidence.BasedOnIncompleteHistory =>
        [
            new JourneyPlanWarning(
                JourneyPlanWarningCode.IncompleteHistory,
                JourneyPlanWarningSeverity.Caution)
        ],
        _ => []
    };

    private static IEnumerable<ActivityGap> AllGaps(
        CrewJourneyPlanRequest request) =>
        request.Snapshot.Slot1.Gaps.Concat(request.Snapshot.Slot2.Gaps);

    private static void Validate(CrewJourneyPlanRequest request)
    {
        if (request.InitialDrivingSlot is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(request.InitialDrivingSlot));
        if (request.RemainingDriveMinutes < 0 ||
            request.DeliveryWindowMinutes < 0 ||
            request.OperationalBufferMinutes < 0)
            throw new ArgumentOutOfRangeException(nameof(request));
        if (request.Limits.MaximumSegments <= 0 ||
            request.Limits.MaximumElapsedMinutes <= 0 ||
            request.Limits.MaximumVisitedStates <= 0 ||
            request.Snapshot.WeekEpochOffsetDays is < -6 or > 6)
            throw new ArgumentOutOfRangeException(nameof(request.Limits));
        if (request.Snapshot.Slot1.DriverSlot != 1 ||
            request.Snapshot.Slot2.DriverSlot != 2)
            throw new ArgumentException("Crew snapshot must contain slots S1 and S2.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Snapshot.Slot1.DriverCardId) ||
            string.IsNullOrWhiteSpace(request.Snapshot.Slot2.DriverCardId) ||
            string.Equals(
                request.Snapshot.Slot1.DriverCardId,
                request.Snapshot.Slot2.DriverCardId,
                StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Crew snapshot must contain two different cards.", nameof(request));
    }

    private static bool FitsLimits(
        CrewJourneyPlanRequest request,
        long start,
        long now,
        int duration,
        int segmentCount) =>
        duration > 0 &&
        segmentCount < request.Limits.MaximumSegments &&
        now - start + duration <= request.Limits.MaximumElapsedMinutes;

    private sealed class DriverState
    {
        internal required int Slot { get; init; }
        internal required long ContinuousDriving { get; set; }
        internal required long DailyDriving { get; set; }
        internal required long WeeklyDriving { get; set; }
        internal required long PreviousWeekDriving { get; set; }
        internal required long BreakMinutes { get; set; }
        internal required int DailyExtensionsUsed { get; set; }
        internal required int ReducedDailyRestsUsed { get; set; }
        internal required long DailyRestDeadline { get; set; }
        internal required long WeeklyRestDeadline { get; set; }
        internal required int WeekEpochOffsetDays { get; init; }

        internal static DriverState From(
            CrewDriverPlanningSnapshot snapshot,
            long startGameMinute,
            int weekEpochOffsetDays)
        {
            var state = snapshot.Evaluation.State;
            return new DriverState
            {
                Slot = snapshot.DriverSlot,
                ContinuousDriving = state.ContinuousDrivingMinutes,
                DailyDriving = state.DailyDrivingMinutes,
                WeeklyDriving = state.WeeklyDrivingMinutes,
                PreviousWeekDriving = state.PreviousWeekDrivingMinutes,
                BreakMinutes = Math.Min(
                    QualifiedBreak,
                    state.CurrentContinuousBreakMinutes),
                DailyExtensionsUsed = state.DailyExtensionsUsedThisWeek,
                ReducedDailyRestsUsed = state.ReducedDailyRestsSinceWeeklyRest,
                DailyRestDeadline = checked(
                    startGameMinute +
                    state.MinutesUntilDailyRestDeadline),
                WeeklyRestDeadline = checked(
                    startGameMinute +
                    state.MinutesUntilWeeklyRestDeadline),
                WeekEpochOffsetDays = weekEpochOffsetDays
            };
        }

        internal CrewDriverPlanSummary Summary(long completion) => new(
            Slot,
            ContinuousDriving,
            DailyDriving,
            WeeklyDriving,
            PreviousWeekDriving,
            BreakMinutes,
            DailyRestDeadline - completion,
            WeeklyRestDeadline - completion,
            DailyExtensionsUsed,
            ReducedDailyRestsUsed);
    }
}

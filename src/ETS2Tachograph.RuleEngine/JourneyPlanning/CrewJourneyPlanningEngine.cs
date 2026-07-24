using ETS2Tachograph.Core.Enums;

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
    private const int MultiManningDailyWindow = 1_800;

    public CrewJourneyPlanResult Plan(CrewJourneyPlanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);

        var start = request.Snapshot.StartGameMinute;
        var slot1 = DriverState.From(request.Snapshot.Slot1, start);
        var slot2 = DriverState.From(request.Snapshot.Slot2, start);
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

        var now = start;
        var remaining = request.RemainingDriveMinutes;
        var currentSlot = request.InitialDrivingSlot;
        var segments = new List<CrewJourneyPlanSegment>();
        var visited = 0;

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

            if (slot1.DailyRestDeadline <= now || slot2.DailyRestDeadline <= now)
            {
                AddStationaryRest(
                    segments,
                    ref now,
                    ReducedDailyRest,
                    JourneyPlanSegmentReason.DailyRestDeadline,
                    slot1,
                    slot2);
                continue;
            }

            var current = currentSlot == 1 ? slot1 : slot2;
            var other = currentSlot == 1 ? slot2 : slot1;
            var capacity = DrivingCapacity(current, now);
            if (capacity <= 0)
            {
                var otherCapacity = DrivingCapacity(other, now);
                if (otherCapacity > 0)
                {
                    currentSlot = other.Slot;
                    continue;
                }

                if (NeedsContinuousBreak(current) || NeedsContinuousBreak(other))
                {
                    var breakDuration = BreakCompletionDuration(current, other);
                    AddStationaryBreak(segments, ref now, breakDuration, slot1, slot2);
                    currentSlot = SelectDriver(slot1, slot2, currentSlot, now);
                    continue;
                }

                if (WeeklyCapacity(slot1) <= 0 && WeeklyCapacity(slot2) <= 0)
                {
                    AddCalendarWait(request, segments, ref now, slot1, slot2);
                    currentSlot = SelectDriver(slot1, slot2, currentSlot, now);
                    continue;
                }

                AddStationaryRest(
                    segments,
                    ref now,
                    ReducedDailyRest,
                    JourneyPlanSegmentReason.DailyDrivingLimit,
                    slot1,
                    slot2);
                currentSlot = SelectDriver(slot1, slot2, currentSlot, now);
                continue;
            }

            var duration = (int)Math.Min(remaining, capacity);
            var passengerQualified = QualifyPassengerBreak(other, duration);
            var end = checked(now + duration);
            var previousDrivingSlot = segments
                .LastOrDefault(segment => segment.DrivingSlot is not null)?
                .DrivingSlot;
            segments.Add(new CrewJourneyPlanSegment(
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
            remaining -= duration;
            if (remaining > 0 && DrivingCapacity(current, now) <= 0)
                currentSlot = other.Slot;
        }

        var arrival = now;
        if (request.OperationalBufferMinutes > 0)
        {
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
            now = end;
        }

        var elapsed = checked((int)(now - start));
        var status = elapsed <= request.DeliveryWindowMinutes
            ? JourneyPlanStatus.MeetsDeadline
            : JourneyPlanStatus.MissesDeadline;
        return Result(status, request, now, arrival, segments, slot1, slot2);
    }

    private static long DrivingCapacity(DriverState driver, long now)
    {
        var dailyLimit = driver.DailyExtensionsUsed < 2
            ? ExtendedDailyDrivingLimit
            : NormalDailyDrivingLimit;
        return new long[]
        {
            ContinuousDrivingLimit - driver.ContinuousDriving,
            dailyLimit - driver.DailyDriving,
            WeeklyDrivingLimit - driver.WeeklyDriving,
            BiweeklyDrivingLimit - driver.WeeklyDriving - driver.PreviousWeekDriving,
            driver.DailyRestDeadline - now
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
        passenger.BreakMinutes = Math.Min(
            QualifiedBreak,
            passenger.BreakMinutes + duration);
        if (passenger.BreakMinutes < QualifiedBreak)
            return false;

        passenger.ContinuousDriving = 0;
        return true;
    }

    private static void ApplyDriving(DriverState driver, int duration)
    {
        driver.BreakMinutes = 0;
        driver.ContinuousDriving += duration;
        driver.DailyDriving += duration;
        driver.WeeklyDriving += duration;
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
        ResetAfterDailyRest(slot1, end);
        ResetAfterDailyRest(slot2, end);
        now = end;
    }

    private static void ResetAfterDailyRest(DriverState driver, long restEnd)
    {
        if (driver.DailyDriving > NormalDailyDrivingLimit)
            driver.DailyExtensionsUsed++;
        driver.ContinuousDriving = 0;
        driver.DailyDriving = 0;
        driver.BreakMinutes = QualifiedBreak;
        driver.DailyRestDeadline = checked(restEnd + MultiManningDailyWindow);
    }

    private static void AddCalendarWait(
        CrewJourneyPlanRequest request,
        ICollection<CrewJourneyPlanSegment> segments,
        ref long now,
        DriverState slot1,
        DriverState slot2)
    {
        const int weekMinutes = 7 * 24 * 60;
        var offset = request.Snapshot.WeekEpochOffsetDays * 24L * 60;
        var relative = now - offset;
        var weekIndex = Math.DivRem(relative, weekMinutes, out var remainder);
        if (remainder < 0)
            weekIndex--;
        var end = checked(offset + ((weekIndex + 1) * weekMinutes));
        if (end <= now)
            end = checked(now + weekMinutes);
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
        StartNewWeek(slot1, end);
        StartNewWeek(slot2, end);
        now = end;
    }

    private static void StartNewWeek(DriverState driver, long restEnd)
    {
        driver.PreviousWeekDriving = driver.WeeklyDriving;
        driver.WeeklyDriving = 0;
        ResetAfterDailyRest(driver, restEnd);
    }

    private static int SelectDriver(
        DriverState slot1,
        DriverState slot2,
        int preferred,
        long now)
    {
        var preferredDriver = preferred == 1 ? slot1 : slot2;
        if (DrivingCapacity(preferredDriver, now) > 0)
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
        return new CrewJourneyPlanResult(
            status,
            request.Snapshot.TelemetryAvailable
                ? JourneyPlanConfidence.VerifiedByCurrentRuleModel
                : JourneyPlanConfidence.BasedOnLastSavedState,
            request.Snapshot.StartGameMinute,
            arrival,
            arrival is null ? null : completion,
            elapsed,
            request.DeliveryWindowMinutes - elapsed,
            segments,
            warnings ?? [],
            slot1.Summary(completion),
            slot2.Summary(completion));
    }

    private static void Validate(CrewJourneyPlanRequest request)
    {
        if (request.InitialDrivingSlot is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(request.InitialDrivingSlot));
        if (request.RemainingDriveMinutes < 0 ||
            request.DeliveryWindowMinutes < 0 ||
            request.OperationalBufferMinutes < 0)
            throw new ArgumentOutOfRangeException(nameof(request));
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

    private sealed class DriverState
    {
        internal required int Slot { get; init; }
        internal required long ContinuousDriving { get; set; }
        internal required long DailyDriving { get; set; }
        internal required long WeeklyDriving { get; set; }
        internal required long PreviousWeekDriving { get; set; }
        internal required long BreakMinutes { get; set; }
        internal required int DailyExtensionsUsed { get; set; }
        internal required long DailyRestDeadline { get; set; }

        internal static DriverState From(
            CrewDriverPlanningSnapshot snapshot,
            long startGameMinute)
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
                DailyRestDeadline = checked(
                    startGameMinute +
                    snapshot.Evaluation.State.MinutesUntilDailyRestDeadline)
            };
        }

        internal CrewDriverPlanSummary Summary(long completion) => new(
            Slot,
            ContinuousDriving,
            DailyDriving,
            WeeklyDriving,
            PreviousWeekDriving,
            BreakMinutes,
            DailyRestDeadline - completion);
    }
}

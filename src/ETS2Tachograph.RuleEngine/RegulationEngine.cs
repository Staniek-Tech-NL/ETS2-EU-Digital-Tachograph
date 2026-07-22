using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Rules;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.RuleEngine.Internal;
using ETS2Tachograph.RuleEngine.Rules;

namespace ETS2Tachograph.RuleEngine;

/// <summary>Pure projection and rule evaluation for freight transport under Regulation 561/2006.</summary>
public sealed class RegulationEngine
{
    private const long ContinuousDrivingLimit = 270;
    private const long NormalDailyDrivingLimit = 540;
    private readonly IReadOnlyList<IStateRegulationRule> _rules;

    public RegulationEngine(IEnumerable<IStateRegulationRule>? rules = null)
    {
        _rules = rules?.ToList() ??
        [
            new DrivingLimitsRule(),
            new BreakRule(),
            new DailyRestRule(),
            new WeeklyRestRule()
        ];
    }

    public RegulationEvaluation Evaluate(
        RuleContext context,
        RegulationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        options ??= new RegulationOptions();

        var history = context.History.OrderBy(record => record.Start).ToList();
        var invalidManualEntry = history.FirstOrDefault(record =>
            record.Source == ActivitySource.ManualEntry &&
            record.Activity is not (
                DriverActivity.BreakOrRest or
                DriverActivity.OtherWork or
                DriverActivity.Availability));
        if (invalidManualEntry is not null)
            throw new InvalidOperationException(
                $"Activity {invalidManualEntry.Activity} is not allowed for a manual entry.");
        var runs = HistoryAnalysis.Runs(history, context.Now);
        var currentWeek = GameWeek.From(context.Now, options.WeekEpochOffsetDays);
        var previousWeek = new GameWeek(currentWeek.Index - 1);
        var currentBounds = HistoryAnalysis.WeekBounds(currentWeek, options.WeekEpochOffsetDays);
        var previousBounds = HistoryAnalysis.WeekBounds(previousWeek, options.WeekEpochOffsetDays);

        var continuousDriving = ContinuousDriving(runs);
        var qualifiedRestRuns = QualifiedDailyRestRuns(runs);
        var qualifiedRestSet = qualifiedRestRuns.ToHashSet();
        var qualifiedRests = qualifiedRestRuns.Select(ClassifyRest).ToList();
        var dailyPeriods = DailyDrivingPeriods(runs, qualifiedRestSet);
        var dailyDriving = dailyPeriods.Count == 0 ? 0 : dailyPeriods[^1].DrivingMinutes;
        var extensions = dailyPeriods.Count(period =>
            period.DrivingMinutes > NormalDailyDrivingLimit &&
            GameWeek.From(period.End, options.WeekEpochOffsetDays) == currentWeek);
        var weeklyDriving = HistoryAnalysis.DrivingOverlap(history, currentBounds.Start, currentBounds.End);
        var previousWeekDriving = HistoryAnalysis.DrivingOverlap(history, previousBounds.Start, previousBounds.End);

        var lastDailyRest = qualifiedRestRuns.LastOrDefault();
        var firstMinute = history.Count == 0 ? context.Now : history[0].Start;
        var dailyAnchor = lastDailyRest?.EndExclusive ?? firstMinute;
        var dailyWindow = options.MultiManning ? 1_800 : 1_440;

        var lastWeeklyRest = qualifiedRestRuns.LastOrDefault(run => run.DurationMinutes >= 1_440);
        var weeklyAnchor = lastWeeklyRest?.EndExclusive ?? firstMinute;
        var reducedDailyRests = CountReducedDailyRestsSince(
            qualifiedRestRuns,
            lastWeeklyRest?.EndExclusive);
        var compensations = ProjectCompensations(
            qualifiedRestRuns,
            context.Now,
            options.WeekEpochOffsetDays);
        var weeklyPatternInvalid = WeeklyPatternInvalid(
            history,
            qualifiedRestRuns,
            currentWeek,
            options.WeekEpochOffsetDays);

        var state = new RegulationState
        {
            ContinuousDrivingMinutes = continuousDriving,
            DailyDrivingMinutes = dailyDriving,
            DailyWorkMinutes = DailyWorkSince(runs, dailyAnchor),
            WeeklyDrivingMinutes = weeklyDriving,
            PreviousWeekDrivingMinutes = previousWeekDriving,
            DailyExtensionsUsedThisWeek = extensions,
            ReducedDailyRestsSinceWeeklyRest = reducedDailyRests,
            MinutesUntilBreak = ContinuousDrivingLimit - continuousDriving,
            MinutesUntilDailyRestDeadline = dailyWindow - (context.Now - dailyAnchor),
            MinutesUntilWeeklyRestDeadline = 8_640 - (context.Now - weeklyAnchor),
            LastDailyRestResetAt = lastDailyRest?.EndExclusive
        };

        var ruleInput = new RegulationRuleInput(
            state,
            context.Now,
            compensations,
            weeklyPatternInvalid);
        var violations = _rules.SelectMany(rule => rule.Evaluate(ruleInput)).ToList();

        return new RegulationEvaluation(state, violations, compensations)
        {
            QualifiedRests = qualifiedRests
        };
    }

    private static long ContinuousDriving(IReadOnlyList<ActivityRun> runs)
    {
        long driving = 0;
        var firstSplitBreakTaken = false;

        foreach (var run in runs)
        {
            if (run.Activity == DriverActivity.Driving)
            {
                driving += run.DurationMinutes;
                continue;
            }

            if (run.Activity != DriverActivity.BreakOrRest)
            {
                continue;
            }

            if (run.DurationMinutes >= 45 ||
                (firstSplitBreakTaken && run.DurationMinutes >= 30))
            {
                driving = 0;
                firstSplitBreakTaken = false;
            }
            else if (run.DurationMinutes >= 15)
            {
                firstSplitBreakTaken = true;
            }
        }

        return driving;
    }

    private static IReadOnlyList<DailyDrivingPeriod> DailyDrivingPeriods(
        IReadOnlyList<ActivityRun> runs,
        IReadOnlySet<ActivityRun> qualifiedRestRuns)
    {
        var periods = new List<DailyDrivingPeriod>();
        long driving = 0;
        var start = runs.Count == 0 ? new GameTime(0) : runs[0].Start;

        foreach (var run in runs)
        {
            if (run.Activity == DriverActivity.Driving)
            {
                driving += run.DurationMinutes;
            }

            if (qualifiedRestRuns.Contains(run))
            {
                periods.Add(new DailyDrivingPeriod(start, run.EndExclusive, driving));
                driving = 0;
                start = run.EndExclusive;
            }
        }

        if (runs.Count > 0 && (periods.Count == 0 || start < runs[^1].EndExclusive))
        {
            periods.Add(new DailyDrivingPeriod(start, runs[^1].EndExclusive, driving));
        }

        return periods;
    }

    private static int CountReducedDailyRestsSince(
        IReadOnlyList<ActivityRun> runs,
        GameTime? weeklyRestEnd) => runs.Count(run =>
            run.Activity == DriverActivity.BreakOrRest &&
            run.DurationMinutes is >= 540 and < 660 &&
            (weeklyRestEnd is null || run.Start >= weeklyRestEnd.Value));

    private static IReadOnlyList<ActivityRun> QualifiedDailyRestRuns(
        IReadOnlyList<ActivityRun> runs)
    {
        var measured = runs.Where(run =>
            run.SourceGapId is null &&
            run.Activity == DriverActivity.BreakOrRest &&
            run.DurationMinutes >= 540);

        // Resolved manual rest can remain continuous with directly adjacent
        // measured rest. The merged run retains SourceGapId for audit. Only the
        // longest uninterrupted block associated with a gap can qualify;
        // OtherWork, Availability and real holes already split the runs above.
        var fromResolvedGaps = runs
            .Where(run =>
                run.SourceGapId is not null &&
                run.Activity == DriverActivity.BreakOrRest)
            .GroupBy(run => run.SourceGapId!.Value)
            .Select(group => group
                .OrderByDescending(run => run.DurationMinutes)
                .ThenByDescending(run => run.EndExclusive)
                .First())
            .Where(run => run.DurationMinutes >= 540);

        return measured
            .Concat(fromResolvedGaps)
            .OrderBy(run => run.Start)
            .ToList();
    }

    private static QualifiedRestPeriod ClassifyRest(ActivityRun run) => new(
        run.Start,
        run.EndExclusive,
        run.SourceGapId,
        run.DurationMinutes < 660
            ? DailyRestClassification.Reduced
            : DailyRestClassification.Regular,
        run.DurationMinutes switch
        {
            >= 2_700 => WeeklyRestClassification.Regular,
            >= 1_440 => WeeklyRestClassification.Reduced,
            _ => null
        });

    private static long DailyWorkSince(
        IReadOnlyList<ActivityRun> runs,
        GameTime dailyAnchor) => runs
        .Where(run => run.Activity is DriverActivity.Driving or DriverActivity.OtherWork)
        .Sum(run => Math.Max(
            0,
            run.EndExclusive.TotalMinutes -
            Math.Max(run.Start.TotalMinutes, dailyAnchor.TotalMinutes)));

    private static IReadOnlyList<WeeklyRestCompensation> ProjectCompensations(
        IReadOnlyList<ActivityRun> runs,
        GameTime now,
        int offsetDays)
    {
        var obligations = new List<WeeklyRestCompensation>();

        foreach (var run in runs.Where(run =>
                     run.Activity == DriverActivity.BreakOrRest &&
                     run.EndExclusive < now))
        {
            var hostMinimumMinutes = run.DurationMinutes switch
            {
                >= 2_700 => 2_700,
                >= 1_440 => 1_440,
                >= 540 => 540,
                _ => 0
            };
            var attachableMinutes = run.DurationMinutes - hostMinimumMinutes;
            if (attachableMinutes > 0 && obligations.Any(item => item.IsOpen))
            {
                SettleWholeObligations(
                    obligations,
                    run,
                    hostMinimumMinutes,
                    attachableMinutes);
            }

            if (run.DurationMinutes is >= 1_440 and < 2_700)
            {
                var reductionWeek = GameWeek.From(run.Start, offsetDays);
                var dueAtExclusive = new GameTime(checked(
                    ((reductionWeek.Index + 4) * GameWeek.MinutesPerWeek) -
                    ((long)offsetDays * GameWeek.MinutesPerDay)));
                var sourceRestBlockId = CompensationIdentity.RestBlockId(run);
                var originalOwedMinutes = 2_700 - run.DurationMinutes;
                obligations.Add(new WeeklyRestCompensation
                {
                    IdentitySchemeVersion = CompensationIdentity.SchemeVersion,
                    ObligationId = CompensationIdentity.ObligationId(
                        run.DriverCardId,
                        sourceRestBlockId,
                        reductionWeek),
                    DriverCardId = run.DriverCardId,
                    SourceRestBlockId = sourceRestBlockId,
                    SourceRestEndExclusive = run.EndExclusive,
                    OriginalOwedMinutes = originalOwedMinutes,
                    RemainingMinutes = originalOwedMinutes,
                    ReductionWeek = reductionWeek,
                    DueAtExclusive = dueAtExclusive,
                    Status = now < dueAtExclusive
                        ? WeeklyRestCompensationStatus.OpenOnTime
                        : WeeklyRestCompensationStatus.Overdue
                });
            }
        }

        for (var index = 0; index < obligations.Count; index++)
        {
            var obligation = obligations[index];
            if (!obligation.IsOpen)
                continue;

            obligations[index] = obligation with
            {
                Status = now < obligation.DueAtExclusive
                    ? WeeklyRestCompensationStatus.OpenOnTime
                    : WeeklyRestCompensationStatus.Overdue
            };
        }

        return obligations;
    }

    private static void SettleWholeObligations(
        List<WeeklyRestCompensation> obligations,
        ActivityRun paymentRun,
        long hostMinimumMinutes,
        long attachableMinutes)
    {
        var paymentRestBlockId = CompensationIdentity.RestBlockId(paymentRun);
        var paymentCursor = paymentRun.Start.AddMinutes(hostMinimumMinutes);
        var ordered = obligations
            .Select((obligation, index) => (obligation, index))
            .Where(item => item.obligation.IsOpen)
            .OrderBy(item => item.obligation.DueAtExclusive)
            .ThenBy(item => item.obligation.ReductionWeek.Index)
            .ThenBy(item => item.obligation.SourceRestEndExclusive)
            .ThenBy(item => item.obligation.ObligationId, StringComparer.Ordinal)
            .ToList();

        foreach (var (obligation, index) in ordered)
        {
            if (attachableMinutes < obligation.OriginalOwedMinutes)
                break;

            var paymentEnd = paymentCursor.AddMinutes(obligation.OriginalOwedMinutes);
            var status = paymentEnd < obligation.DueAtExclusive
                ? WeeklyRestCompensationStatus.PaidOnTime
                : WeeklyRestCompensationStatus.PaidLate;
            obligations[index] = obligation with
            {
                RemainingMinutes = 0,
                PaymentRestBlockId = paymentRestBlockId,
                PaymentRange = new CompensationMinuteRange(paymentCursor, paymentEnd),
                SettledAt = paymentEnd,
                Status = status
            };
            paymentCursor = paymentEnd;
            attachableMinutes -= obligation.OriginalOwedMinutes;
        }
    }

    private static bool WeeklyPatternInvalid(
        IReadOnlyList<ActivityRecord> history,
        IReadOnlyList<ActivityRun> runs,
        GameWeek currentWeek,
        int offsetDays)
    {
        if (history.Count == 0)
        {
            return false;
        }

        var firstWeek = new GameWeek(currentWeek.Index - 2);
        var windowStart = HistoryAnalysis.WeekBounds(firstWeek, offsetDays).Start;
        var windowEnd = HistoryAnalysis.WeekBounds(currentWeek, offsetDays).Start;
        if (history.Min(record => record.Start.TotalMinutes) > windowStart)
        {
            return false;
        }

        var weeklyRests = runs.Where(run =>
            run.Activity == DriverActivity.BreakOrRest &&
            run.DurationMinutes >= 1_440 &&
            run.Start.TotalMinutes >= windowStart &&
            run.Start.TotalMinutes < windowEnd).ToList();

        return weeklyRests.Count < 2 ||
            weeklyRests.All(run => run.DurationMinutes < 2_700);
    }

    private sealed record DailyDrivingPeriod(GameTime Start, GameTime End, long DrivingMinutes);

}

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
        RegulationOptions? options = null,
        IReadOnlyList<RestAllocationDecision>? restAllocationDecisions = null)
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
        var currentBreak =
            runs.Count > 0 &&
            runs[^1].Activity == DriverActivity.BreakOrRest &&
            runs[^1].EndExclusive == context.Now
                ? runs[^1].DurationMinutes
                : 0;
        var qualifiedRestRuns = QualifiedDailyRestRuns(runs);
        var qualifiedRestSet = qualifiedRestRuns.ToHashSet();
        var compensationProjection = ProjectCompensations(
            qualifiedRestRuns,
            context.Now,
            options.WeekEpochOffsetDays,
            restAllocationDecisions ?? []);
        var qualifiedRests = qualifiedRestRuns
            .Select(run => ClassifyRest(
                run,
                WeeklyClassification(run, context.Now, compensationProjection.Allocations)))
            .ToList();
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

        var lastWeeklyRest = qualifiedRestRuns.LastOrDefault(run =>
            WeeklyClassification(run, context.Now, compensationProjection.Allocations) is not null);
        var weeklyAnchor = lastWeeklyRest?.EndExclusive ?? firstMinute;
        var reducedDailyRests = CountReducedDailyRestsSince(
            qualifiedRestRuns,
            lastWeeklyRest?.EndExclusive);
        var compensations = compensationProjection.Obligations;
        var weeklyPatternInvalid = WeeklyPatternInvalid(
            history,
            qualifiedRestRuns,
            currentWeek,
            options.WeekEpochOffsetDays,
            context.Now,
            compensationProjection.Allocations);

        var state = new RegulationState
        {
            ContinuousDrivingMinutes = continuousDriving,
            CurrentContinuousBreakMinutes = currentBreak,
            DailyDrivingMinutes = dailyDriving,
            DailyWorkMinutes = DailyWorkSince(runs, dailyAnchor),
            WeeklyDrivingMinutes = weeklyDriving,
            PreviousWeekDrivingMinutes = previousWeekDriving,
            DailyExtensionsUsedThisWeek = extensions,
            ReducedDailyRestsSinceWeeklyRest = reducedDailyRests,
            MinutesUntilBreak = ContinuousDrivingLimit - continuousDriving,
            MinutesUntilDailyRestDeadline = dailyWindow - (context.Now - dailyAnchor),
            MinutesUntilWeeklyRestDeadline = 8_640 - (context.Now - weeklyAnchor),
            LastDailyRestResetAt = lastDailyRest?.EndExclusive,
            PendingRestAllocation = compensationProjection.Allocations.Any(item => item.IsPending)
        };

        var ruleInput = new RegulationRuleInput(
            state,
            context.Now,
            compensations,
            weeklyPatternInvalid);
        var violations = _rules.SelectMany(rule => rule.Evaluate(ruleInput)).ToList();

        return new RegulationEvaluation(
            state,
            violations,
            compensations,
            compensationProjection.Allocations)
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

    private static QualifiedRestPeriod ClassifyRest(
        ActivityRun run,
        WeeklyRestClassification? weeklyClassification) => new(
        run.Start,
        run.EndExclusive,
        run.SourceGapId,
        run.DurationMinutes < 660
            ? DailyRestClassification.Reduced
            : DailyRestClassification.Regular,
        weeklyClassification);

    private static WeeklyRestClassification? WeeklyClassification(
        ActivityRun run,
        GameTime now,
        IReadOnlyList<RestAllocationProjection> allocations)
    {
        if (run.DurationMinutes < 1_440)
            return null;
        if (run.EndExclusive >= now)
        {
            return run.DurationMinutes >= 2_700
                ? WeeklyRestClassification.Regular
                : WeeklyRestClassification.Reduced;
        }

        var restBlockId = CompensationIdentity.RestBlockId(run);
        var allocation = allocations.FirstOrDefault(item =>
            string.Equals(item.RestBlockId, restBlockId, StringComparison.Ordinal));
        if (allocation?.SelectedCandidate?.SatisfiesWeeklyRestRequirement != true)
            return null;

        return allocation.SelectedCandidate.Purpose is
            RestAllocationPurpose.RegularWeeklyRestOnly or
            RestAllocationPurpose.RegularWeeklyRestWithCompensation
                ? WeeklyRestClassification.Regular
                : WeeklyRestClassification.Reduced;
    }

    private static long DailyWorkSince(
        IReadOnlyList<ActivityRun> runs,
        GameTime dailyAnchor) => runs
        .Where(run => run.Activity is DriverActivity.Driving or DriverActivity.OtherWork)
        .Sum(run => Math.Max(
            0,
            run.EndExclusive.TotalMinutes -
            Math.Max(run.Start.TotalMinutes, dailyAnchor.TotalMinutes)));

    private static CompensationProjection ProjectCompensations(
        IReadOnlyList<ActivityRun> runs,
        GameTime now,
        int offsetDays,
        IReadOnlyList<RestAllocationDecision> decisions)
    {
        var obligations = new List<WeeklyRestCompensation>();
        var allocations = new List<RestAllocationProjection>();

        foreach (var run in runs.Where(run =>
                     run.Activity == DriverActivity.BreakOrRest &&
                     run.EndExclusive < now))
        {
            if (run.DurationMinutes < 1_440)
            {
                var attachableMinutes = run.DurationMinutes - 540;
                if (attachableMinutes > 0 && obligations.Any(item => item.IsOpen))
                {
                    SettleWholeObligations(
                        obligations,
                        run,
                        540,
                        attachableMinutes);
                }

                continue;
            }

            var restBlockId = CompensationIdentity.RestBlockId(run);
            var candidates = BuildRestAllocationCandidates(
                obligations,
                run,
                restBlockId);
            var decision = decisions
                .Where(item =>
                    item.Status == RestAllocationDecisionStatus.Active &&
                    string.Equals(item.DriverCardId, run.DriverCardId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.RestBlockId, restBlockId, StringComparison.Ordinal))
                .OrderByDescending(item => item.DecidedAtUtc)
                .ThenByDescending(item => item.DecisionId)
                .FirstOrDefault();
            var selectedCandidate = decision is null
                ? candidates.Count == 1 ? candidates[0] : null
                : candidates.FirstOrDefault(item =>
                    string.Equals(item.CandidateId, decision.CandidateId, StringComparison.Ordinal));
            allocations.Add(new RestAllocationProjection(
                restBlockId,
                run.DriverCardId,
                run.Start,
                run.EndExclusive,
                candidates,
                decision,
                selectedCandidate));

            if (selectedCandidate is null)
                continue;

            if (selectedCandidate.ObligationIds.Count > 0)
            {
                SettleSelectedObligations(
                    obligations,
                    run,
                    selectedCandidate.HostMinimumMinutes,
                    selectedCandidate.ObligationIds);
            }

            if (selectedCandidate.NewDebtMinutes > 0)
            {
                obligations.Add(CreateObligation(
                    run,
                    restBlockId,
                    selectedCandidate.NewDebtMinutes,
                    now,
                    offsetDays));
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

        return new CompensationProjection(obligations, allocations);
    }

    private static IReadOnlyList<RestAllocationCandidate> BuildRestAllocationCandidates(
        IReadOnlyList<WeeklyRestCompensation> obligations,
        ActivityRun run,
        string restBlockId)
    {
        var candidates = new List<RestAllocationCandidate>();
        if (run.DurationMinutes < 2_700)
        {
            var dailyObligations = WholeObligationsThatFit(
                obligations,
                run.DurationMinutes - 540);
            if (dailyObligations.Count > 0)
            {
                candidates.Add(Candidate(
                    restBlockId,
                    RestAllocationPurpose.DailyRestWithCompensation,
                    540,
                    dailyObligations,
                    newDebtMinutes: 0,
                    satisfiesWeeklyRestRequirement: false));
            }

            candidates.Add(Candidate(
                restBlockId,
                RestAllocationPurpose.ReducedWeeklyRestOnly,
                1_440,
                [],
                checked((int)(2_700 - run.DurationMinutes)),
                satisfiesWeeklyRestRequirement: true));

            var weeklyObligations = WholeObligationsThatFit(
                obligations,
                run.DurationMinutes - 1_440);
            if (weeklyObligations.Count > 0)
            {
                candidates.Add(Candidate(
                    restBlockId,
                    RestAllocationPurpose.ReducedWeeklyRestWithCompensation,
                    1_440,
                    weeklyObligations,
                    newDebtMinutes: 1_260,
                    satisfiesWeeklyRestRequirement: true));
            }
        }
        else
        {
            candidates.Add(Candidate(
                restBlockId,
                RestAllocationPurpose.RegularWeeklyRestOnly,
                2_700,
                [],
                newDebtMinutes: 0,
                satisfiesWeeklyRestRequirement: true));
            var regularObligations = WholeObligationsThatFit(
                obligations,
                run.DurationMinutes - 2_700);
            if (regularObligations.Count > 0)
            {
                candidates.Add(Candidate(
                    restBlockId,
                    RestAllocationPurpose.RegularWeeklyRestWithCompensation,
                    2_700,
                    regularObligations,
                    newDebtMinutes: 0,
                    satisfiesWeeklyRestRequirement: true));
            }
        }

        return candidates;
    }

    private static RestAllocationCandidate Candidate(
        string restBlockId,
        RestAllocationPurpose purpose,
        int hostMinimumMinutes,
        IReadOnlyList<string> obligationIds,
        int newDebtMinutes,
        bool satisfiesWeeklyRestRequirement) => new(
            CompensationIdentity.RestAllocationCandidateId(
                restBlockId,
                purpose,
                hostMinimumMinutes,
                obligationIds,
                newDebtMinutes),
            restBlockId,
            purpose,
            hostMinimumMinutes,
            obligationIds,
            newDebtMinutes,
            satisfiesWeeklyRestRequirement);

    private static IReadOnlyList<string> WholeObligationsThatFit(
        IReadOnlyList<WeeklyRestCompensation> obligations,
        long attachableMinutes)
    {
        var result = new List<string>();
        foreach (var obligation in OrderedOpenObligations(obligations))
        {
            if (attachableMinutes < obligation.OriginalOwedMinutes)
                break;
            result.Add(obligation.ObligationId);
            attachableMinutes -= obligation.OriginalOwedMinutes;
        }

        return result;
    }

    private static WeeklyRestCompensation CreateObligation(
        ActivityRun run,
        string sourceRestBlockId,
        int originalOwedMinutes,
        GameTime now,
        int offsetDays)
    {
        var reductionWeek = GameWeek.From(run.Start, offsetDays);
        var dueAtExclusive = new GameTime(checked(
            ((reductionWeek.Index + 4) * GameWeek.MinutesPerWeek) -
            ((long)offsetDays * GameWeek.MinutesPerDay)));
        return new WeeklyRestCompensation
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
        };
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

    private static void SettleSelectedObligations(
        List<WeeklyRestCompensation> obligations,
        ActivityRun paymentRun,
        int hostMinimumMinutes,
        IReadOnlyList<string> selectedObligationIds)
    {
        var paymentRestBlockId = CompensationIdentity.RestBlockId(paymentRun);
        var paymentCursor = paymentRun.Start.AddMinutes(hostMinimumMinutes);
        foreach (var obligationId in selectedObligationIds)
        {
            var index = obligations.FindIndex(item =>
                item.IsOpen &&
                string.Equals(item.ObligationId, obligationId, StringComparison.Ordinal));
            if (index < 0)
                throw new InvalidOperationException(
                    $"Rest allocation references unavailable obligation {obligationId}.");

            var obligation = obligations[index];
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
        }
    }

    private static IEnumerable<WeeklyRestCompensation> OrderedOpenObligations(
        IEnumerable<WeeklyRestCompensation> obligations) => obligations
        .Where(item => item.IsOpen)
        .OrderBy(item => item.DueAtExclusive)
        .ThenBy(item => item.ReductionWeek.Index)
        .ThenBy(item => item.SourceRestEndExclusive)
        .ThenBy(item => item.ObligationId, StringComparer.Ordinal);

    private static bool WeeklyPatternInvalid(
        IReadOnlyList<ActivityRecord> history,
        IReadOnlyList<ActivityRun> runs,
        GameWeek currentWeek,
        int offsetDays,
        GameTime now,
        IReadOnlyList<RestAllocationProjection> allocations)
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
            WeeklyClassification(run, now, allocations) is not null &&
            run.Start.TotalMinutes >= windowStart &&
            run.Start.TotalMinutes < windowEnd).ToList();

        return weeklyRests.Count < 2 ||
            weeklyRests.All(run =>
                WeeklyClassification(run, now, allocations) != WeeklyRestClassification.Regular);
    }

    private sealed record DailyDrivingPeriod(GameTime Start, GameTime End, long DrivingMinutes);

    private sealed record CompensationProjection(
        IReadOnlyList<WeeklyRestCompensation> Obligations,
        IReadOnlyList<RestAllocationProjection> Allocations);

}

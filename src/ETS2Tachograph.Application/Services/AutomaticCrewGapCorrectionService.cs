using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Rules;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Application.Services;

public sealed record AutomaticCrewGapCorrectionResult(
    int InspectedGapCount,
    IReadOnlyList<Guid> ResolvedGapIds);

public sealed class AutomaticCrewGapCorrectionService(
    IActivityRepository activities,
    IManualEntryRepository resolutions,
    TimeProvider? timeProvider = null)
{
    private static readonly HashSet<DriverActivity> StableActivities =
    [
        DriverActivity.BreakOrRest,
        DriverActivity.OtherWork,
        DriverActivity.Availability
    ];

    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<AutomaticCrewGapCorrectionResult> CorrectAsync(
        IReadOnlyCollection<string> crewCardIds,
        CancellationToken cancellationToken = default)
    {
        var cards = crewCardIds
            .Where(card => !string.IsNullOrWhiteSpace(card))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (cards.Count < 2)
            return new AutomaticCrewGapCorrectionResult(0, []);

        var gaps = await activities.GetUnresolvedGapsAsync(
            cancellationToken: cancellationToken);
        var candidates = gaps
            .Where(gap =>
                gap.Reason == ActivityGapReason.ForwardTimeJump &&
                gap.EndExclusive is not null &&
                cards.Contains(gap.DriverCardId, StringComparer.OrdinalIgnoreCase))
            .OrderBy(gap => gap.Start)
            .ThenBy(gap => gap.Id)
            .ToList();
        var resolved = new List<Guid>();

        foreach (var gap in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var context = await resolutions.LoadGapContextAsync(gap.Id, cancellationToken);
            if (context is null || !context.IsCanonical || context.Gap.EndExclusive is null)
                continue;
            var gapEnd = context.Gap.EndExclusive.Value;

            var activity = StableActivityAcrossGap(context);
            if (activity is null)
                continue;
            if (!await AnotherCrewCardRestsAcrossAsync(
                    cards,
                    gap.DriverCardId,
                    gap.Start,
                    gapEnd,
                    cancellationToken))
                continue;

            var reconstructed = new ActivityRecord
            {
                Id = Guid.NewGuid(),
                DriverCardId = gap.DriverCardId,
                Activity = activity.Value,
                Start = gap.Start,
                EndExclusive = gapEnd,
                RecordedAtUtc = _timeProvider.GetUtcNow(),
                Source = ActivitySource.AutomaticCrewReconstruction,
                Condition = SpecialCondition.None,
                SourceGapId = gap.Id
            };
            var persistence = await resolutions.ApplyGapResolutionAsync(
                new ManualEntryResolutionWrite(
                    gap.Id,
                    gapEnd,
                    [reconstructed]),
                cancellationToken);
            if (persistence.Status is ManualEntryPersistenceStatus.Applied or
                ManualEntryPersistenceStatus.AlreadyApplied)
                resolved.Add(persistence.Gap.Id);
        }

        return new AutomaticCrewGapCorrectionResult(candidates.Count, resolved);
    }

    private static DriverActivity? StableActivityAcrossGap(ManualEntryGapContext context)
    {
        var before = context.CanonicalRecords
            .Where(record => record.EndExclusive == context.Gap.Start)
            .OrderByDescending(record => record.Start)
            .FirstOrDefault();
        var after = context.CanonicalRecords
            .Where(record => record.Start == context.Gap.EndExclusive)
            .OrderBy(record => record.EndExclusive)
            .FirstOrDefault();
        return before is not null &&
               after is not null &&
               before.Activity == after.Activity &&
               StableActivities.Contains(before.Activity)
            ? before.Activity
            : null;
    }

    private async Task<bool> AnotherCrewCardRestsAcrossAsync(
        IReadOnlyList<string> cards,
        string gapCardId,
        GameTime start,
        GameTime endExclusive,
        CancellationToken cancellationToken)
    {
        foreach (var card in cards.Where(card =>
                     !string.Equals(card, gapCardId, StringComparison.OrdinalIgnoreCase)))
        {
            var history = await activities.LoadDriverHistoryAsync(
                card,
                start,
                endExclusive,
                cancellationToken);
            var cursor = start;
            foreach (var record in history
                         .Where(record => record.Activity == DriverActivity.BreakOrRest)
                         .OrderBy(record => record.Start))
            {
                if (record.Start > cursor)
                    break;
                if (record.EndExclusive > cursor)
                    cursor = record.EndExclusive;
                if (cursor >= endExclusive)
                    return true;
            }
        }

        return false;
    }
}

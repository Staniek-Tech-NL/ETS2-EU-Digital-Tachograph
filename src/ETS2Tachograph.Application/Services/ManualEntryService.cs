using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Rules;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.RuleEngine;

namespace ETS2Tachograph.Application.Services;

public sealed class ManualEntryService(
    IManualEntryRepository repository,
    IManualEntryDiagnostics? diagnostics = null,
    TimeProvider? timeProvider = null,
    RegulationEngine? regulationEngine = null)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly RegulationEngine _regulationEngine = regulationEngine ?? new RegulationEngine();

    public Task<ResolveGapResult> ResolveGap(
        Guid gapId,
        IReadOnlyList<ManualEntrySegment> activitySegments,
        GameTime resolvedAtGameMinute,
        CancellationToken cancellationToken = default) =>
        ResolveGapAsync(gapId, activitySegments, resolvedAtGameMinute, cancellationToken);

    public async Task<ResolveGapResult> ResolveGapAsync(
        Guid gapId,
        IReadOnlyList<ManualEntrySegment> activitySegments,
        GameTime resolvedAtGameMinute,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activitySegments);
        var context = await repository.LoadGapContextAsync(gapId, cancellationToken) ??
            throw new ManualEntryValidationException(
                ManualEntryError.GapNotFound,
                $"Activity gap {gapId} was not found.");
        if (!context.IsCanonical)
            throw new ManualEntryValidationException(
                ManualEntryError.GapNotCanonical,
                "Only a gap from the canonical game-time branch can be resolved.");

        var ordered = ManualEntryValidator.Validate(
            context.Gap,
            activitySegments,
            context.CanonicalRecords);
        var recordedAtUtc = _timeProvider.GetUtcNow();
        var records = ordered.Select(segment => new ActivityRecord
        {
            Id = Guid.NewGuid(),
            DriverCardId = context.Gap.DriverCardId,
            Activity = segment.Activity,
            Start = new GameTime(segment.FromGameMinute),
            EndExclusive = new GameTime(segment.ToGameMinuteExclusive),
            RecordedAtUtc = recordedAtUtc,
            Source = ActivitySource.ManualEntry,
            Condition = SpecialCondition.None,
            SourceGapId = context.Gap.Id
        }).ToList();

        var persisted = await repository.ApplyGapResolutionAsync(
            new ManualEntryResolutionWrite(context.Gap.Id, resolvedAtGameMinute, records),
            cancellationToken);
        if (persisted.Status == ManualEntryPersistenceStatus.Conflict)
        {
            diagnostics?.RecordResolutionConflict(
                context.Gap.Id,
                persisted.Segments,
                records);
            throw new ManualEntryValidationException(
                ManualEntryError.ResolutionConflict,
                "The gap was already resolved with different manual-entry segments.");
        }

        // Reload the canonical branch after the atomic write. The reset belongs
        // to the end of the qualifying rest block, not to the time of this click.
        var recalculationContext = await repository.LoadGapContextAsync(
            persisted.Gap.Id,
            cancellationToken) ?? throw new ManualEntryValidationException(
                ManualEntryError.GapNotFound,
                $"Activity gap {persisted.Gap.Id} disappeared after it was resolved.");
        var evaluation = _regulationEngine.Evaluate(new RuleContext(
            resolvedAtGameMinute,
            recalculationContext.CanonicalRecords));

        return new ResolveGapResult(
            persisted.Status == ManualEntryPersistenceStatus.Applied
                ? ResolveGapStatus.Resolved
                : ResolveGapStatus.AlreadyResolved,
            persisted.Gap,
            persisted.Segments,
            evaluation);
    }
}

public static class ManualEntryValidator
{
    private static readonly HashSet<DriverActivity> AllowedActivities =
    [
        DriverActivity.BreakOrRest,
        DriverActivity.OtherWork,
        DriverActivity.Availability
    ];

    public static IReadOnlyList<ManualEntrySegment> Validate(
        ActivityGap gap,
        IReadOnlyList<ManualEntrySegment> segments,
        IReadOnlyList<ActivityRecord> canonicalRecords)
    {
        ArgumentNullException.ThrowIfNull(gap);
        ArgumentNullException.ThrowIfNull(segments);
        ArgumentNullException.ThrowIfNull(canonicalRecords);

        // Activity type is deliberately checked first, before any persistence or
        // coverage work, so Driving/OUT/Unknown can never enter as manual data.
        var invalidActivity = segments.FirstOrDefault(segment =>
            !AllowedActivities.Contains(segment.Activity));
        if (invalidActivity is not null)
            throw new ManualEntryValidationException(
                ManualEntryError.InvalidActivity,
                $"Activity {invalidActivity.Activity} is not allowed in a manual entry.");
        if (gap.EndExclusive is null)
            throw new ManualEntryValidationException(
                ManualEntryError.GapStillOpen,
                "An open activity gap cannot be resolved.");
        if (segments.Count == 0)
            throw new ManualEntryValidationException(
                ManualEntryError.IncompleteCoverage,
                "Manual segments must cover the complete activity gap.");

        var ordered = segments
            .OrderBy(segment => segment.FromGameMinute)
            .ThenBy(segment => segment.ToGameMinuteExclusive)
            .ToList();
        foreach (var segment in ordered)
        {
            if (segment.ToGameMinuteExclusive <= segment.FromGameMinute)
                throw new ManualEntryValidationException(
                    ManualEntryError.InvalidSegment,
                    "Every manual segment must have a positive duration.");
            if (segment.FromGameMinute < gap.Start.TotalMinutes ||
                segment.ToGameMinuteExclusive > gap.EndExclusive.Value.TotalMinutes)
                throw new ManualEntryValidationException(
                    ManualEntryError.OutsideGap,
                    "A manual segment extends outside the source gap.");
        }

        var cursor = gap.Start.TotalMinutes;
        foreach (var segment in ordered)
        {
            if (segment.FromGameMinute < cursor)
                throw new ManualEntryValidationException(
                    ManualEntryError.OverlappingSegments,
                    "Manual-entry segments cannot overlap.");
            if (segment.FromGameMinute > cursor)
                throw new ManualEntryValidationException(
                    ManualEntryError.IncompleteCoverage,
                    "Manual-entry segments leave an uncovered minute.");
            cursor = segment.ToGameMinuteExclusive;
        }
        if (cursor != gap.EndExclusive.Value.TotalMinutes)
            throw new ManualEntryValidationException(
                ManualEntryError.IncompleteCoverage,
                "Manual-entry segments do not cover the complete activity gap.");

        var collision = canonicalRecords.FirstOrDefault(record =>
            record.SourceGapId != gap.Id &&
            record.EndExclusive > gap.Start &&
            record.Start < gap.EndExclusive.Value);
        if (collision is not null)
            throw new ManualEntryValidationException(
                ManualEntryError.HistoryCollision,
                $"Manual entry collides with activity record {collision.Id}.");

        return ordered;
    }
}

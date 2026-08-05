using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Application.Services;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace ETS2Tachograph.Infrastructure.Persistence;

public sealed partial class ActivityRepository
{
    public async Task<ManualEntryGapContext?> LoadGapContextAsync(
        Guid gapId,
        CancellationToken cancellationToken = default)
    {
        var stored = await context.ActivityGaps.AsNoTracking()
            .Include(gap => gap.ActivitySession)
            .SingleOrDefaultAsync(gap => gap.Id == gapId, cancellationToken);
        if (stored is null)
            return null;

        var sessions = await LoadGapSessionsAsync(
            stored.DriverCardId,
            cancellationToken);
        var canonicalGaps = CanonicalizeGaps(sessions);
        var rawGaps = sessions.SelectMany(session => session.Gaps).ToList();
        var rawById = rawGaps.ToDictionary(gap => gap.Id);
        var canonicalGap = canonicalGaps.SingleOrDefault(gap => gap.Id == gapId) ??
                           canonicalGaps.SingleOrDefault(gap =>
                               IsProjectionDescendantOf(gap, gapId, rawById));
        var sourceGap = canonicalGap is not null && rawById.TryGetValue(canonicalGap.Id, out var effectiveSource)
            ? effectiveSource
            : Map(stored, stored.ActivitySession.SessionIndex);
        var effectiveGapId = canonicalGap?.Id ?? gapId;
        var existingResolutionRecords = (await context.ActivityRecords.AsNoTracking()
                .Where(record => record.SourceGapId == effectiveGapId)
                .OrderBy(record => record.StartGameMinute)
                .ToListAsync(cancellationToken))
            .Select(record => Map(record, stored.DriverCardId))
            .ToList();
        var canonicalRecords = await LoadDriverHistoryAsync(
            stored.DriverCardId,
            cancellationToken: cancellationToken);
        return new ManualEntryGapContext(
            canonicalGap ?? sourceGap,
            canonicalGap is not null,
            canonicalGap is not null &&
            canonicalGap.Start == sourceGap.Start &&
            canonicalGap.EndExclusive == sourceGap.EndExclusive,
            canonicalRecords,
            existingResolutionRecords);
    }

    private static bool IsProjectionDescendantOf(
        ActivityGap candidate,
        Guid sourceGapId,
        IReadOnlyDictionary<Guid, ActivityGap> rawById)
    {
        var parentId = candidate.ProjectionSourceGapId;
        var visited = new HashSet<Guid>();
        while (parentId is not null && visited.Add(parentId.Value))
        {
            if (parentId.Value == sourceGapId)
                return true;
            parentId = rawById.TryGetValue(parentId.Value, out var parent)
                ? parent.ProjectionSourceGapId
                : null;
        }

        return false;
    }

    public async Task<ManualEntryPersistenceResult> ApplyGapResolutionAsync(
        ManualEntryResolutionWrite write,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var gapEntity = await context.ActivityGaps
            .Include(gap => gap.ActivitySession)
            .SingleOrDefaultAsync(gap => gap.Id == write.GapId, cancellationToken) ??
            throw new ManualEntryValidationException(
                ManualEntryError.GapNotFound,
                $"Activity gap {write.GapId} was not found.");
        var existingEntities = await context.ActivityRecords.AsNoTracking()
            .Where(record => record.SourceGapId == write.GapId)
            .OrderBy(record => record.StartGameMinute)
            .ToListAsync(cancellationToken);
        var existing = existingEntities
            .Select(record => Map(record, gapEntity.DriverCardId))
            .ToList();

        if (gapEntity.State == ActivityGapState.Resolved)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new ManualEntryPersistenceResult(
                ManualSegmentsEqual(existing, write.Segments)
                    ? ManualEntryPersistenceStatus.AlreadyApplied
                    : ManualEntryPersistenceStatus.Conflict,
                Map(gapEntity, gapEntity.ActivitySession.SessionIndex),
                existing);
        }

        var gapContext = await LoadGapContextAsync(write.GapId, cancellationToken) ??
            throw new ManualEntryValidationException(
                ManualEntryError.GapNotFound,
                $"Activity gap {write.GapId} was not found.");
        if (!gapContext.IsCanonical)
            throw new ManualEntryValidationException(
                ManualEntryError.GapNotCanonical,
                "Only a canonical activity gap can be resolved.");
        if (write.Segments.Any(record =>
                !string.Equals(record.DriverCardId, gapEntity.DriverCardId, StringComparison.OrdinalIgnoreCase) ||
                record.Source is not (ActivitySource.ManualEntry or
                    ActivitySource.AutomaticCrewReconstruction) ||
                record.SourceGapId != write.GapId ||
                record.Condition != SpecialCondition.None))
            throw new InvalidOperationException("Invalid gap-resolution persistence payload.");

        ManualEntryValidator.Validate(
            gapContext.Gap,
            write.Segments.Select(record => new ManualEntrySegment(
                record.Start.TotalMinutes,
                record.EndExclusive.TotalMinutes,
                record.Activity)).ToList(),
            gapContext.CanonicalRecords);

        var targetGapEntity = gapEntity;
        var targetSession = gapEntity.ActivitySession;
        IReadOnlyList<ActivityRecord> persistedSegments = write.Segments;
        if (!gapContext.ProjectionMatchesSource)
        {
            targetSession = await context.ActivitySessions
                .Where(session => session.DriverCardId == gapEntity.DriverCardId)
                .OrderByDescending(session => session.SessionIndex)
                .FirstAsync(cancellationToken);
            targetGapEntity = new ActivityGapEntity
            {
                Id = Guid.NewGuid(),
                DriverCardId = gapEntity.DriverCardId,
                ActivitySessionId = targetSession.Id,
                Slot = gapContext.Gap.Slot,
                StartGameMinute = gapContext.Gap.Start.TotalMinutes,
                EndGameMinuteExclusive = gapContext.Gap.EndExclusive!.Value.TotalMinutes,
                Reason = gapContext.Gap.Reason,
                State = ActivityGapState.Resolved,
                ResolvedAtGameMinute = write.ResolvedAt.TotalMinutes,
                ProjectionSourceGapId = gapEntity.Id
            };
            context.ActivityGaps.Add(targetGapEntity);
            persistedSegments = write.Segments
                .Select(record => record with { SourceGapId = targetGapEntity.Id })
                .ToList();
        }

        context.ActivityRecords.AddRange(persistedSegments.Select(record =>
            new ActivityRecordEntity
            {
                Id = record.Id,
                ActivitySessionId = targetSession.Id,
                Activity = record.Activity,
                StartGameMinute = record.Start.TotalMinutes,
                EndGameMinuteExclusive = record.EndExclusive.TotalMinutes,
                RecordedAtUtc = record.RecordedAtUtc,
                Source = record.Source,
                Condition = record.Condition,
                SourceGapId = record.SourceGapId
            }));
        if (gapContext.ProjectionMatchesSource)
        {
            gapEntity.State = ActivityGapState.Resolved;
            gapEntity.ResolvedAtGameMinute = write.ResolvedAt.TotalMinutes;
        }
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ManualEntryPersistenceResult(
            ManualEntryPersistenceStatus.Applied,
            Map(targetGapEntity, targetSession.SessionIndex),
            persistedSegments);
    }

    private static bool ManualSegmentsEqual(
        IReadOnlyList<ActivityRecord> first,
        IReadOnlyList<ActivityRecord> second)
    {
        var orderedFirst = first.OrderBy(record => record.Start).ToList();
        var orderedSecond = second.OrderBy(record => record.Start).ToList();
        return orderedFirst.Count == orderedSecond.Count &&
               orderedFirst.Zip(orderedSecond).All(pair =>
                   pair.First.Start == pair.Second.Start &&
                   pair.First.EndExclusive == pair.Second.EndExclusive &&
                   pair.First.Activity == pair.Second.Activity &&
                    pair.First.Source == pair.Second.Source &&
                    (pair.First.Source is ActivitySource.ManualEntry or
                        ActivitySource.AutomaticCrewReconstruction) &&
                   pair.First.SourceGapId == pair.Second.SourceGapId &&
                   pair.First.Condition == pair.Second.Condition);
    }

}

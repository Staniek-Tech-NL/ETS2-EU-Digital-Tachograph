using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;
using Microsoft.EntityFrameworkCore;

namespace ETS2Tachograph.Infrastructure.Persistence;

public sealed partial class ActivityRepository
{
    public async Task AppendAsync(
        string driverCardId, int sessionIndex, IReadOnlyList<ActivityRecord> records,
        CancellationToken cancellationToken = default)
    {
        if (records.Count == 0)
            return;
        var startedAt = await context.ActivitySessions
            .Where(x => x.DriverCardId == driverCardId && x.SessionIndex == sessionIndex)
            .Select(x => x.StartedAtGameMinute)
            .SingleAsync(cancellationToken);
        await ApplySessionWritesAsync(
            [new ActivitySessionWrite(driverCardId, sessionIndex, new GameTime(startedAt), records)],
            cancellationToken);
    }

    public async Task ApplySessionWritesAsync(
        IReadOnlyList<ActivitySessionWrite> writes,
        CancellationToken cancellationToken = default)
    {
        if (writes.Count == 0)
            return;

        var merged = new Dictionary<(string CardId, int SessionIndex), ActivitySessionWrite>();
        foreach (var write in writes)
        {
            var key = (write.DriverCardId, write.SessionIndex);
            if (!merged.TryGetValue(key, out var current))
            {
                merged.Add(key, write);
                continue;
            }

            if (current.StartedAt != write.StartedAt)
                throw new InvalidOperationException(
                    $"Session {write.SessionIndex} for card {write.DriverCardId} has inconsistent branch anchors.");

            merged[key] = current with
            {
                Records = current.Records.Concat(write.Records).ToList(),
                Gaps = (current.Gaps ?? []).Concat(write.Gaps ?? []).ToList(),
                RemovedGapIds = (current.RemovedGapIds ?? [])
                    .Concat(write.RemovedGapIds ?? [])
                    .Distinct()
                    .ToList()
            };
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var write in merged.Values)
            {
                var session = context.ActivitySessions.Local.FirstOrDefault(x =>
                        x.DriverCardId == write.DriverCardId &&
                        x.SessionIndex == write.SessionIndex) ??
                    await context.ActivitySessions.SingleOrDefaultAsync(x =>
                            x.DriverCardId == write.DriverCardId &&
                            x.SessionIndex == write.SessionIndex,
                        cancellationToken);
                if (session is null)
                {
                    await InvalidateWarmProjectionForNewBranchAsync(
                        write.DriverCardId,
                        write.StartedAt.TotalMinutes,
                        cancellationToken);
                    session = new ActivitySessionEntity
                    {
                        Id = Guid.NewGuid(),
                        DriverCardId = write.DriverCardId,
                        SessionIndex = write.SessionIndex,
                        StartedAtGameMinute = write.StartedAt.TotalMinutes,
                        CreatedAtUtc = DateTimeOffset.UtcNow
                    };
                    context.ActivitySessions.Add(session);
                }

                if (session.StartedAtGameMinute != write.StartedAt.TotalMinutes)
                    throw new InvalidOperationException(
                        $"Session {write.SessionIndex} for card {write.DriverCardId} has a different persisted branch anchor.");

                var incomingByMinute = NormalizeIncoming(write);
                var incomingMinutes = incomingByMinute.Keys.ToArray();
                var existingByMinute = incomingMinutes.Length == 0
                    ? new Dictionary<long, ActivityRecordEntity>()
                    : (await context.ActivityRecords
                            .Where(x => x.ActivitySessionId == session.Id &&
                                        incomingMinutes.Contains(x.StartGameMinute))
                            .ToListAsync(cancellationToken))
                        .ToDictionary(x => x.StartGameMinute);

                foreach (var (minute, record) in incomingByMinute)
                {
                    if (existingByMinute.TryGetValue(minute, out var existing))
                    {
                        if (!SameContent(existing, record))
                            diagnostics?.RecordConflict(
                                write.DriverCardId,
                                write.SessionIndex,
                                Map(existing, write.DriverCardId),
                                record);
                        continue;
                    }

                    context.ActivityRecords.Add(new ActivityRecordEntity
                    {
                        Id = record.Id,
                        ActivitySessionId = session.Id,
                        Activity = record.Activity,
                        StartGameMinute = record.Start.TotalMinutes,
                        EndGameMinuteExclusive = record.EndExclusive.TotalMinutes,
                        RecordedAtUtc = record.RecordedAtUtc,
                        Source = record.Source,
                        Condition = record.Condition,
                        SourceGapId = record.SourceGapId
                    });
                }

                var removedGapIds = (write.RemovedGapIds ?? []).Distinct().ToArray();
                if (removedGapIds.Length > 0)
                {
                    var removedGaps = await context.ActivityGaps
                        .Where(x => x.ActivitySessionId == session.Id &&
                                    removedGapIds.Contains(x.Id))
                        .ToListAsync(cancellationToken);
                    context.ActivityGaps.RemoveRange(removedGaps);
                }

                var incomingGapsByMinute = NormalizeIncomingGaps(write);
                if (incomingGapsByMinute.Values.Any(gap => removedGapIds.Contains(gap.Id)))
                    throw new InvalidOperationException(
                        $"The same activity gap cannot be removed and upserted in session {write.SessionIndex}.");
                var incomingGapMinutes = incomingGapsByMinute.Keys.ToArray();
                var existingGapsByMinute = incomingGapMinutes.Length == 0
                    ? new Dictionary<long, ActivityGapEntity>()
                    : (await context.ActivityGaps
                            .Where(x => x.ActivitySessionId == session.Id &&
                                        incomingGapMinutes.Contains(x.StartGameMinute))
                            .ToListAsync(cancellationToken))
                        .ToDictionary(x => x.StartGameMinute);

                foreach (var (minute, gap) in incomingGapsByMinute)
                {
                    if (existingGapsByMinute.TryGetValue(minute, out var existing))
                    {
                        if (SameContent(existing, gap))
                            continue;
                        if (CanCloseOpenGap(existing, gap))
                        {
                            existing.EndGameMinuteExclusive = gap.EndExclusive!.Value.TotalMinutes;
                            continue;
                        }
                        throw new InvalidOperationException(
                            $"Activity gap conflict in session {write.SessionIndex} for card {write.DriverCardId} at minute {minute}.");
                    }

                    context.ActivityGaps.Add(new ActivityGapEntity
                    {
                        Id = gap.Id,
                        DriverCardId = write.DriverCardId,
                        ActivitySessionId = session.Id,
                        Slot = gap.Slot,
                        StartGameMinute = gap.Start.TotalMinutes,
                        EndGameMinuteExclusive = gap.EndExclusive?.TotalMinutes,
                        Reason = gap.Reason,
                        State = gap.State,
                        ResolvedAtGameMinute = gap.ResolvedAt?.TotalMinutes,
                        ProjectionSourceGapId = gap.ProjectionSourceGapId
                    });
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task InvalidateWarmProjectionForNewBranchAsync(
        string driverCardId,
        long branchAnchorGameMinute,
        CancellationToken cancellationToken)
    {
        var highWaterMark = await context.ActivityRetentionStates
            .Where(state => state.DriverCardId == driverCardId)
            .Select(state => (long?)state.HighWaterMarkGameMinute)
            .SingleOrDefaultAsync(cancellationToken);
        if (highWaterMark is null)
            return;

        var warmThreshold =
            highWaterMark.Value - ActivityRetentionPolicy.HotWindowMinutes;
        if (branchAnchorGameMinute >= warmThreshold)
            return;

        var removedWarmBlocks = await context.WarmActivityBlocks
            .Where(block => block.DriverCardId == driverCardId)
            .ExecuteDeleteAsync(cancellationToken);
        var sessionIds = (await context.ActivitySessions
                .Where(session => session.DriverCardId == driverCardId)
                .Select(session => session.Id)
                .ToListAsync(cancellationToken))
            .ToHashSet();
        var restoredRawRecords = sessionIds.Count == 0
            ? 0
            : await context.ActivityRecords
                .Where(record =>
                    record.IsArchivedToWarm &&
                    sessionIds.Contains(record.ActivitySessionId))
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        record => record.IsArchivedToWarm,
                        false),
                    cancellationToken);

        // ExecuteDelete/ExecuteUpdate bypass the long-lived EF change tracker.
        // Synchronize tracked derived entities so a later SaveChanges or archive
        // cannot revive the invalidated cache or keep stale archive flags in memory.
        foreach (var entry in context.ChangeTracker.Entries<WarmActivityBlockEntity>()
                     .Where(entry => entry.Entity.DriverCardId == driverCardId)
                     .ToList())
            entry.State = EntityState.Detached;
        foreach (var entry in context.ChangeTracker.Entries<ActivityRecordEntity>()
                     .Where(entry => sessionIds.Contains(entry.Entity.ActivitySessionId)))
        {
            entry.Entity.IsArchivedToWarm = false;
            entry.Property(record => record.IsArchivedToWarm).OriginalValue = false;
            entry.Property(record => record.IsArchivedToWarm).IsModified = false;
        }

        if (removedWarmBlocks > 0 || restoredRawRecords > 0)
            diagnostics?.RecordWarmProjectionInvalidated(
                driverCardId,
                branchAnchorGameMinute,
                warmThreshold,
                removedWarmBlocks,
                restoredRawRecords);
    }

    private Dictionary<long, ActivityRecord> NormalizeIncoming(ActivitySessionWrite write)
    {
        var result = new Dictionary<long, ActivityRecord>();
        foreach (var record in write.Records)
        {
            if (!string.Equals(record.DriverCardId, write.DriverCardId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Activity record {record.Id} belongs to another driver card.");

            var minute = record.Start.TotalMinutes;
            if (!result.TryGetValue(minute, out var first))
            {
                result.Add(minute, record);
                continue;
            }

            if (!SameContent(first, record))
                diagnostics?.RecordConflict(
                    write.DriverCardId,
                    write.SessionIndex,
                    first,
                    record);
        }

        return result;
    }

    private static Dictionary<long, ActivityGap> NormalizeIncomingGaps(ActivitySessionWrite write)
    {
        var result = new Dictionary<long, ActivityGap>();
        foreach (var gap in write.Gaps ?? [])
        {
            if (!string.Equals(gap.DriverCardId, write.DriverCardId, StringComparison.OrdinalIgnoreCase) ||
                gap.SessionIndex != write.SessionIndex)
                throw new InvalidOperationException(
                    $"Activity gap {gap.Id} belongs to another driver card or session.");
            if (gap.Slot is not (1 or 2))
                throw new InvalidOperationException($"Activity gap {gap.Id} has an invalid card slot.");
            if (gap.EndExclusive is not null && gap.EndExclusive.Value <= gap.Start)
                throw new InvalidOperationException($"Activity gap {gap.Id} has an invalid interval.");

            var minute = gap.Start.TotalMinutes;
            if (result.TryGetValue(minute, out var existing) && existing != gap)
                throw new InvalidOperationException(
                    $"Multiple different activity gaps start at minute {minute} in the same session.");
            result.TryAdd(minute, gap);
        }

        return result;
    }

    private static bool SameContent(ActivityRecord first, ActivityRecord second) =>
        first.Start == second.Start &&
        first.EndExclusive == second.EndExclusive &&
        first.Activity == second.Activity &&
        first.Source == second.Source &&
        first.Condition == second.Condition &&
        first.SourceGapId == second.SourceGapId;

    private static bool SameContent(ActivityRecordEntity existing, ActivityRecord incoming) =>
        existing.StartGameMinute == incoming.Start.TotalMinutes &&
        existing.EndGameMinuteExclusive == incoming.EndExclusive.TotalMinutes &&
        existing.Activity == incoming.Activity &&
        existing.Source == incoming.Source &&
        existing.Condition == incoming.Condition &&
        existing.SourceGapId == incoming.SourceGapId;

    private static bool SameContent(ActivityGapEntity existing, ActivityGap incoming) =>
        existing.DriverCardId == incoming.DriverCardId &&
        existing.Slot == incoming.Slot &&
        existing.StartGameMinute == incoming.Start.TotalMinutes &&
        existing.EndGameMinuteExclusive == incoming.EndExclusive?.TotalMinutes &&
        existing.Reason == incoming.Reason &&
        existing.State == incoming.State &&
        existing.ResolvedAtGameMinute == incoming.ResolvedAt?.TotalMinutes &&
        existing.ProjectionSourceGapId == incoming.ProjectionSourceGapId;

    private static bool CanCloseOpenGap(ActivityGapEntity existing, ActivityGap incoming) =>
        existing.Id == incoming.Id &&
        string.Equals(existing.DriverCardId, incoming.DriverCardId, StringComparison.OrdinalIgnoreCase) &&
        existing.Slot == incoming.Slot &&
        existing.StartGameMinute == incoming.Start.TotalMinutes &&
        existing.EndGameMinuteExclusive is null &&
        incoming.EndExclusive is not null &&
        incoming.EndExclusive.Value > incoming.Start &&
        existing.Reason == incoming.Reason &&
        existing.State == ActivityGapState.Unresolved &&
        incoming.State == ActivityGapState.Unresolved &&
        existing.ResolvedAtGameMinute is null &&
        incoming.ResolvedAt is null &&
        existing.ProjectionSourceGapId == incoming.ProjectionSourceGapId;
}

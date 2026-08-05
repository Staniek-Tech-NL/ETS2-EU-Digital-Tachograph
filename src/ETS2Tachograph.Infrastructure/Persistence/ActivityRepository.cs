using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Application.Services;
using Microsoft.EntityFrameworkCore;

namespace ETS2Tachograph.Infrastructure.Persistence;

public sealed partial class ActivityRepository :
    IActivityRepository,
    IActivityRetentionRepository,
    IManualEntryRepository
{
    private readonly TachographDbContext context;
    private readonly IActivityPersistenceDiagnostics? diagnostics;

    public ActivityRepository(
        TachographDbContext context,
        IActivityPersistenceDiagnostics? diagnostics = null)
    {
        this.context = context;
        this.diagnostics = diagnostics;
    }

    public async Task EnsureSessionAsync(
        string driverCardId, int sessionIndex, GameTime startedAt,
        CancellationToken cancellationToken = default)
    {
        var exists = await context.ActivitySessions.AnyAsync(
            x => x.DriverCardId == driverCardId && x.SessionIndex == sessionIndex,
            cancellationToken);
        if (!exists)
        {
            await using var transaction =
                await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await InvalidateWarmProjectionForNewBranchAsync(
                    driverCardId,
                    startedAt.TotalMinutes,
                    cancellationToken);
                context.ActivitySessions.Add(new ActivitySessionEntity
                {
                    Id = Guid.NewGuid(),
                    DriverCardId = driverCardId,
                    SessionIndex = sessionIndex,
                    StartedAtGameMinute = startedAt.TotalMinutes,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }

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

    public async Task<IReadOnlyList<ActivityRecord>> LoadDriverHistoryAsync(
        string driverCardId, GameTime? from = null, GameTime? toExclusive = null,
        CancellationToken cancellationToken = default)
    {
        var fromMinute = from?.TotalMinutes ?? long.MinValue;
        var toMinute = toExclusive?.TotalMinutes ?? long.MaxValue;
        var highWaterMark = await context.ActivityRetentionStates.AsNoTracking()
            .Where(x => x.DriverCardId == driverCardId)
            .Select(x => (long?)x.HighWaterMarkGameMinute)
            .SingleOrDefaultAsync(cancellationToken);
        var warmThreshold = (highWaterMark ?? 0) - ActivityRetentionPolicy.HotWindowMinutes;

        var combined = (await context.WarmActivityBlocks.AsNoTracking()
                .Where(x => x.DriverCardId == driverCardId &&
                            x.EndGameMinuteExclusive > fromMinute &&
                            x.StartGameMinute < toMinute)
                .OrderBy(x => x.StartGameMinute)
                .ToListAsync(cancellationToken))
            .Select(MapWarm)
            .ToList();
        var hasWarmProjection = combined.Count > 0;

        var sessionInfos = await context.ActivitySessions.AsNoTracking()
            .Where(x => x.DriverCardId == driverCardId)
            .OrderBy(x => x.SessionIndex)
            .Select(x => new SessionInfo(
                x.Id,
                x.SessionIndex,
                x.StartedAtGameMinute))
            .ToListAsync(cancellationToken);
        var hotRows = await context.ActivityRecords.AsNoTracking()
            .Where(x => x.ActivitySession.DriverCardId == driverCardId &&
                        !x.IsArchivedToWarm)
            .OrderBy(x => x.StartGameMinute)
            .ToListAsync(cancellationToken);
        var rowsBySession = hotRows
            .GroupBy(x => x.ActivitySessionId)
            .ToDictionary(x => x.Key, x => x.OrderBy(row => row.StartGameMinute).ToList());

        for (var index = 0; index < sessionInfos.Count; index++)
        {
            var session = sessionInfos[index];
            rowsBySession.TryGetValue(session.Id, out var sessionRows);
            sessionRows ??= [];

            if (index > 0)
            {
                // The canonical warm projection already contains every historical
                // branch operation. Replay the branch only against the hot tail;
                // otherwise an empty historical session can delete valid warm blocks.
                var truncateAt = hasWarmProjection
                    ? Math.Max(session.StartedAtGameMinute, warmThreshold)
                    : session.StartedAtGameMinute;
                TruncateAfter(combined, new GameTime(truncateAt));
            }

            combined.AddRange(sessionRows.Select(x => Map(x, driverCardId)));
        }

        var projected = combined
            .Where(x => x.EndExclusive.TotalMinutes > fromMinute &&
                        x.Start.TotalMinutes < toMinute)
            .OrderBy(x => x.Start)
            .ToList();
        try
        {
            EnsureNoOverlap(projected);
            return projected;
        }
        catch (InvalidCanonicalHistoryException exception)
        {
            diagnostics?.RecordCanonicalProjectionFallback(
                driverCardId,
                exception.Previous,
                exception.Current);
            return await LoadRawDriverHistoryAsync(
                driverCardId,
                from,
                toExclusive,
                cancellationToken);
        }
    }

    public async Task<IReadOnlyList<ActivityRecord>> LoadRawDriverHistoryAsync(
        string driverCardId, GameTime? from = null, GameTime? toExclusive = null,
        CancellationToken cancellationToken = default)
    {
        var sessions = await LoadMaterializedSessionsAsync(driverCardId, cancellationToken);
        var canonical = Canonicalize(sessions);
        var fromMinute = from?.TotalMinutes ?? long.MinValue;
        var toMinute = toExclusive?.TotalMinutes ?? long.MaxValue;
        return canonical
            .Where(x => x.EndExclusive.TotalMinutes > fromMinute &&
                        x.Start.TotalMinutes < toMinute)
            .ToList();
    }

    public async Task<IReadOnlyList<ActivityGap>> LoadDriverGapsAsync(
        string driverCardId, GameTime? from = null, GameTime? toExclusive = null,
        CancellationToken cancellationToken = default)
    {
        var sessions = await LoadGapSessionsAsync(driverCardId, cancellationToken);
        var canonical = CanonicalizeGaps(sessions);
        var fromMinute = from?.TotalMinutes ?? long.MinValue;
        var toMinute = toExclusive?.TotalMinutes ?? long.MaxValue;
        return canonical
            .Where(gap => (gap.EndExclusive?.TotalMinutes ?? long.MaxValue) > fromMinute &&
                          gap.Start.TotalMinutes < toMinute)
            .ToList();
    }

    public async Task<IReadOnlyList<ActivityGap>> GetUnresolvedGapsAsync(
        string? driverCardId = null,
        GameTime? fromGameMinute = null,
        GameTime? toGameMinute = null,
        CancellationToken cancellationToken = default)
        => await GetCanonicalGapsAsync(
            driverCardId,
            fromGameMinute,
            toGameMinute,
            includeResolved: false,
            cancellationToken);

    public async Task<IReadOnlyList<ActivityGap>> GetCanonicalGapsAsync(
        string? driverCardId,
        GameTime? fromGameMinute,
        GameTime? toGameMinute,
        bool includeResolved,
        CancellationToken cancellationToken = default)
    {
        var cardIds = driverCardId is null
            ? await context.ActivityGaps.AsNoTracking()
                .Select(gap => gap.DriverCardId)
                .Distinct()
                .ToListAsync(cancellationToken)
            : [driverCardId];
        var fromMinute = fromGameMinute?.TotalMinutes ?? long.MinValue;
        var toMinute = toGameMinute?.TotalMinutes ?? long.MaxValue;
        var result = new List<ActivityGap>();

        foreach (var cardId in cardIds)
        {
            var sessions = await LoadGapSessionsAsync(cardId, cancellationToken);
            result.AddRange(CanonicalizeGaps(sessions).Where(gap =>
                (gap.State == ActivityGapState.Unresolved ||
                 includeResolved && gap.State == ActivityGapState.Resolved) &&
                (gap.EndExclusive?.TotalMinutes ?? long.MaxValue) > fromMinute &&
                gap.Start.TotalMinutes < toMinute));
        }

        return result
            .OrderBy(gap => gap.Start)
            .ThenBy(gap => gap.DriverCardId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(gap => gap.Slot)
            .ToList();
    }

    public async Task<IReadOnlyList<StoredActivitySession>> LoadSessionsAsync(
        string driverCardId, CancellationToken cancellationToken = default)
    {
        var sessions = await context.ActivitySessions.AsNoTracking()
            .Include(x => x.Records.Where(record => !record.IsArchivedToWarm))
            .Include(x => x.Gaps)
            .Where(x => x.DriverCardId == driverCardId).OrderBy(x => x.SessionIndex)
            .ToListAsync(cancellationToken);
        return MapSessions(driverCardId, sessions);
    }

    public async Task<IReadOnlyList<StoredActivitySession>> LoadRestorationSessionsAsync(
        string driverCardId,
        CancellationToken cancellationToken = default)
    {
        var sessions = (await LoadSessionsAsync(driverCardId, cancellationToken)).ToList();
        var warmRecords = (await context.WarmActivityBlocks.AsNoTracking()
                .Where(x => x.DriverCardId == driverCardId)
                .OrderBy(x => x.StartGameMinute)
                .ToListAsync(cancellationToken))
            .Select(MapWarm)
            .ToList();
        if (warmRecords.Count == 0)
            return sessions;

        if (sessions.Count == 0)
        {
            return
            [
                new StoredActivitySession(
                    0,
                    warmRecords[0].Start,
                    warmRecords)
            ];
        }

        var highWaterMark = await context.ActivityRetentionStates.AsNoTracking()
            .Where(x => x.DriverCardId == driverCardId)
            .Select(x => (long?)x.HighWaterMarkGameMinute)
            .SingleOrDefaultAsync(cancellationToken);
        var warmThreshold = (highWaterMark ?? 0) -
                            ActivityRetentionPolicy.HotWindowMinutes;
        var warmHostIndex = sessions.FindLastIndex(session =>
            session.StartedAt.TotalMinutes <= warmThreshold);
        if (warmHostIndex < 0)
            warmHostIndex = 0;

        for (var index = 0; index < warmHostIndex; index++)
        {
            sessions[index] = sessions[index] with
            {
                Records = []
            };
        }

        var warmHost = sessions[warmHostIndex];
        sessions[warmHostIndex] = warmHost with
        {
            Records = warmRecords
                .Concat(warmHost.Records)
                .OrderBy(record => record.Start)
                .ToList()
        };
        return sessions;
    }

    public async Task<IReadOnlyList<StoredActivitySession>> LoadRawSessionsAsync(
        string driverCardId, CancellationToken cancellationToken = default)
    {
        var sessions = await context.ActivitySessions.AsNoTracking()
            .Include(x => x.Records)
            .Include(x => x.Gaps)
            .Where(x => x.DriverCardId == driverCardId).OrderBy(x => x.SessionIndex)
            .ToListAsync(cancellationToken);
        return MapSessions(driverCardId, sessions);
    }

    public async Task<ActivityRetentionResult> ArchiveWarmAsync(
        string driverCardId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var sessions = await context.ActivitySessions.Include(x => x.Records)
            .Include(x => x.Gaps)
            .Where(x => x.DriverCardId == driverCardId)
            .OrderBy(x => x.SessionIndex)
            .ToListAsync(cancellationToken);
        var state = await context.ActivityRetentionStates
            .SingleOrDefaultAsync(x => x.DriverCardId == driverCardId, cancellationToken);
        var maximumStoredMinute = sessions
            .SelectMany(x => x.Records)
            .Select(x => (long?)x.EndGameMinuteExclusive)
            .Max() ?? 0;
        // Existing databases are bootstrapped from stored game minutes once. After that,
        // telemetry observations are authoritative, so a backward-jump flush cannot move
        // the anchor forward by an artificial extra minute.
        var highWaterMark = state?.HighWaterMarkGameMinute ?? maximumStoredMinute;
        var warmThreshold = highWaterMark - ActivityRetentionPolicy.HotWindowMinutes;
        var coldThreshold = highWaterMark - ActivityRetentionPolicy.ColdWindowMinutes;

        var materialized = sessions.Select(session => new MaterializedSession(
            session.SessionIndex,
            new GameTime(session.StartedAtGameMinute),
            session.Records.OrderBy(x => x.StartGameMinute)
                .Select(x => Map(x, driverCardId))
                .ToList(),
            session.Gaps.OrderBy(x => x.StartGameMinute)
                .Select(x => Map(x, session.SessionIndex))
                .ToList())).ToList();
        var canonical = Canonicalize(materialized);
        var desiredBlocks = BuildWarmBlocks(canonical
            .Where(x => x.EndExclusive.TotalMinutes <= warmThreshold)
            .ToList(), driverCardId);
        var existingBlocks = await context.WarmActivityBlocks
            .Where(x => x.DriverCardId == driverCardId)
            .OrderBy(x => x.StartGameMinute)
            .ToListAsync(cancellationToken);

        if (!WarmBlocksEqual(existingBlocks, desiredBlocks))
        {
            context.WarmActivityBlocks.RemoveRange(existingBlocks);
            context.WarmActivityBlocks.AddRange(desiredBlocks);
        }

        foreach (var row in sessions.SelectMany(x => x.Records)
                     .Where(x => x.EndGameMinuteExclusive <= warmThreshold))
            row.IsArchivedToWarm = true;

        if (state is null)
        {
            state = new ActivityRetentionStateEntity
            {
                DriverCardId = driverCardId,
                HighWaterMarkGameMinute = highWaterMark
            };
            context.ActivityRetentionStates.Add(state);
        }
        else
        {
            state.HighWaterMarkGameMinute = highWaterMark;
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new ActivityRetentionResult(
            driverCardId,
            highWaterMark,
            warmThreshold,
            coldThreshold,
            desiredBlocks.Sum(x => x.DurationMinutes),
            desiredBlocks.Count);
    }

    public async Task<long> ObserveGameTimeAsync(
        string driverCardId,
        GameTime gameTime,
        CancellationToken cancellationToken = default)
    {
        var state = await context.ActivityRetentionStates
            .SingleOrDefaultAsync(x => x.DriverCardId == driverCardId, cancellationToken);
        if (state is null)
        {
            state = new ActivityRetentionStateEntity
            {
                DriverCardId = driverCardId,
                HighWaterMarkGameMinute = gameTime.TotalMinutes
            };
            context.ActivityRetentionStates.Add(state);
            await context.SaveChangesAsync(cancellationToken);
        }
        else if (gameTime.TotalMinutes > state.HighWaterMarkGameMinute)
        {
            state.HighWaterMarkGameMinute = gameTime.TotalMinutes;
            await context.SaveChangesAsync(cancellationToken);
        }

        return state.HighWaterMarkGameMinute;
    }

    public Task<long?> GetHighWaterMarkAsync(
        string driverCardId,
        CancellationToken cancellationToken = default) =>
        context.ActivityRetentionStates.AsNoTracking()
            .Where(x => x.DriverCardId == driverCardId)
            .Select(x => (long?)x.HighWaterMarkGameMinute)
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<IReadOnlyList<MaterializedSession>> LoadMaterializedSessionsAsync(
        string driverCardId,
        CancellationToken cancellationToken)
    {
        var sessions = await context.ActivitySessions.AsNoTracking()
            .Include(x => x.Records)
            .Include(x => x.Gaps)
            .Where(x => x.DriverCardId == driverCardId)
            .OrderBy(x => x.SessionIndex)
            .ToListAsync(cancellationToken);
        return sessions.Select(session => new MaterializedSession(
            session.SessionIndex,
            new GameTime(session.StartedAtGameMinute),
            session.Records.OrderBy(x => x.StartGameMinute).Select(x => new ActivityRecord
            {
                Id = x.Id,
                DriverCardId = driverCardId,
                Activity = x.Activity,
                Start = new GameTime(x.StartGameMinute),
                EndExclusive = new GameTime(x.EndGameMinuteExclusive),
                RecordedAtUtc = x.RecordedAtUtc,
                Source = x.Source,
                Condition = x.Condition,
                SourceGapId = x.SourceGapId
            }).ToList(),
            session.Gaps.OrderBy(x => x.StartGameMinute)
                .Select(x => Map(x, session.SessionIndex)).ToList())).ToList();
    }

    private async Task<IReadOnlyList<MaterializedSession>> LoadGapSessionsAsync(
        string driverCardId,
        CancellationToken cancellationToken)
    {
        var sessions = await context.ActivitySessions.AsNoTracking()
            .Include(session => session.Gaps)
            .Where(session => session.DriverCardId == driverCardId)
            .OrderBy(session => session.SessionIndex)
            .ToListAsync(cancellationToken);
        return sessions.Select(session => new MaterializedSession(
            session.SessionIndex,
            new GameTime(session.StartedAtGameMinute),
            [],
            session.Gaps.OrderBy(gap => gap.StartGameMinute)
                .Select(gap => Map(gap, session.SessionIndex))
                .ToList())).ToList();
    }

    private static IReadOnlyList<StoredActivitySession> MapSessions(
        string driverCardId,
        IReadOnlyList<ActivitySessionEntity> sessions) => sessions.Select(session =>
            new StoredActivitySession(
                session.SessionIndex,
                new GameTime(session.StartedAtGameMinute),
                session.Records.OrderBy(x => x.StartGameMinute)
                    .Select(x => Map(x, driverCardId))
                    .ToList(),
                session.Gaps.OrderBy(x => x.StartGameMinute)
                    .Select(x => Map(x, session.SessionIndex))
                    .ToList())).ToList();

    private static List<ActivityRecord> Canonicalize(
        IReadOnlyList<MaterializedSession> sessions)
    {
        var canonical = new List<ActivityRecord>();
        for (var index = 0; index < sessions.Count; index++)
        {
            var session = sessions[index];
            var branchStart = session.StartedAt;
            if (index > 0)
                TruncateAfter(canonical, branchStart);
            // From the anchor upwards the new session already owns the timeline, because
            // TruncateAfter cleared it. Below the anchor it may only fill minutes nobody
            // covers: a manual entry resolves gaps, it does not amend recorded telemetry.
            foreach (var record in session.Records.OrderBy(x => x.Start))
            {
                foreach (var fragment in SubtractCoveredRanges(record, canonical))
                    InsertByStart(canonical, fragment);
            }
        }

        EnsureNoOverlap(canonical);
        return canonical;
    }

    /// <summary>
    /// Splits <paramref name="incoming"/> into the parts no canonical record covers yet.
    /// Intervals are half-open, so a fragment ending where the next one starts is adjacent,
    /// not overlapping. Returns nothing when the record is already fully covered, and more
    /// than one fragment when coverage falls inside it.
    /// </summary>
    private static IEnumerable<ActivityRecord> SubtractCoveredRanges(
        ActivityRecord incoming,
        IReadOnlyList<ActivityRecord> canonicalRecords)
    {
        var end = incoming.EndExclusive.TotalMinutes;
        var cursor = incoming.Start.TotalMinutes;
        var index = FirstRecordEndingAfter(canonicalRecords, cursor);
        while (index < canonicalRecords.Count)
        {
            var cover = canonicalRecords[index++];
            if (cover.Start.TotalMinutes >= end)
                break;
            if (cover.Start.TotalMinutes > cursor)
                yield return incoming with
                {
                    Start = new GameTime(cursor),
                    EndExclusive = new GameTime(Math.Min(cover.Start.TotalMinutes, end))
                };
            cursor = Math.Max(cursor, cover.EndExclusive.TotalMinutes);
            if (cursor >= end)
                yield break;
        }

        if (cursor < end)
            yield return incoming with
            {
                Start = new GameTime(cursor),
                EndExclusive = new GameTime(end)
            };
    }

    private static int FirstRecordEndingAfter(
        IReadOnlyList<ActivityRecord> records,
        long minute)
    {
        var low = 0;
        var high = records.Count;
        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            if (records[middle].EndExclusive.TotalMinutes <= minute)
                low = middle + 1;
            else
                high = middle;
        }

        return low;
    }

    private static void InsertByStart(
        List<ActivityRecord> records,
        ActivityRecord record)
    {
        var low = 0;
        var high = records.Count;
        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            if (records[middle].Start < record.Start)
                low = middle + 1;
            else
                high = middle;
        }

        records.Insert(low, record);
    }

    private static void EnsureNoOverlap(IReadOnlyList<ActivityRecord> ordered)
    {
        for (var index = 1; index < ordered.Count; index++)
            if (ordered[index].Start < ordered[index - 1].EndExclusive)
                throw new InvalidCanonicalHistoryException(
                    ordered[index].DriverCardId,
                    ordered[index - 1],
                    ordered[index]);
    }

    private static List<ActivityGap> CanonicalizeGaps(
        IReadOnlyList<MaterializedSession> sessions)
    {
        var canonical = new List<ActivityGap>();
        for (var index = 0; index < sessions.Count; index++)
        {
            var session = sessions[index];
            if (index > 0)
                TruncateGapsAfter(canonical, session.StartedAt);
            foreach (var gap in session.Gaps.OrderBy(x => x.Start))
            {
                if (gap.ProjectionSourceGapId is { } sourceGapId)
                    canonical.RemoveAll(existing => existing.Id == sourceGapId);
                canonical.Add(gap);
            }
        }

        return canonical.OrderBy(x => x.Start).ToList();
    }

    private static void TruncateAfter(List<ActivityRecord> records, GameTime branchStart)
    {
        for (var index = records.Count - 1; index >= 0; index--)
        {
            var record = records[index];
            if (record.Start >= branchStart)
            {
                records.RemoveAt(index);
                continue;
            }
            if (record.EndExclusive > branchStart)
                records[index] = record with { EndExclusive = branchStart };
        }
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

    private static List<WarmActivityBlockEntity> BuildWarmBlocks(
        IReadOnlyList<ActivityRecord> records,
        string driverCardId)
    {
        var ordered = records.OrderBy(x => x.Start).ToList();
        EnsureNoOverlap(ordered);

        var blocks = new List<WarmActivityBlockEntity>();
        foreach (var record in ordered)
        {
            if (blocks.Count > 0 &&
                blocks[^1].Activity == record.Activity &&
                blocks[^1].SourceGapId == record.SourceGapId &&
                (blocks[^1].Condition == record.Condition ||
                 (blocks[^1].Condition != SpecialCondition.CrewBreakInMotion &&
                  record.Condition != SpecialCondition.CrewBreakInMotion)) &&
                blocks[^1].EndGameMinuteExclusive == record.Start.TotalMinutes)
            {
                var block = blocks[^1];
                block.EndGameMinuteExclusive = record.EndExclusive.TotalMinutes;
                block.DurationMinutes = block.EndGameMinuteExclusive - block.StartGameMinute;
                if (block.Source != record.Source)
                    block.Source = ActivitySource.Mixed;
                if (block.Condition != record.Condition)
                    block.Condition = SpecialCondition.Mixed;
                continue;
            }

            blocks.Add(new WarmActivityBlockEntity
            {
                Id = Guid.NewGuid(),
                DriverCardId = driverCardId,
                StartGameMinute = record.Start.TotalMinutes,
                EndGameMinuteExclusive = record.EndExclusive.TotalMinutes,
                DurationMinutes = record.DurationMinutes,
                Activity = record.Activity,
                Source = record.Source,
                Condition = record.Condition,
                SourceGapId = record.SourceGapId
            });
        }
        return blocks;
    }

    private static bool WarmBlocksEqual(
        IReadOnlyList<WarmActivityBlockEntity> existing,
        IReadOnlyList<WarmActivityBlockEntity> desired) =>
        existing.Count == desired.Count && existing.Zip(desired).All(pair =>
            pair.First.StartGameMinute == pair.Second.StartGameMinute &&
            pair.First.EndGameMinuteExclusive == pair.Second.EndGameMinuteExclusive &&
            pair.First.DurationMinutes == pair.Second.DurationMinutes &&
            pair.First.Activity == pair.Second.Activity &&
            pair.First.Source == pair.Second.Source &&
            pair.First.Condition == pair.Second.Condition &&
            pair.First.SourceGapId == pair.Second.SourceGapId);

    private static ActivityRecord Map(ActivityRecordEntity x, string driverCardId) => new()
    {
        Id = x.Id,
        DriverCardId = driverCardId,
        Activity = x.Activity,
        Start = new GameTime(x.StartGameMinute),
        EndExclusive = new GameTime(x.EndGameMinuteExclusive),
        RecordedAtUtc = x.RecordedAtUtc,
        Source = x.Source,
        Condition = x.Condition,
        SourceGapId = x.SourceGapId
    };

    private static ActivityRecord MapWarm(WarmActivityBlockEntity x) => new()
    {
        Id = x.Id,
        DriverCardId = x.DriverCardId,
        Activity = x.Activity,
        Start = new GameTime(x.StartGameMinute),
        EndExclusive = new GameTime(x.EndGameMinuteExclusive),
        RecordedAtUtc = DateTimeOffset.UnixEpoch,
        Source = x.Source,
        Condition = x.Condition,
        SourceGapId = x.SourceGapId
    };

    private static ActivityGap Map(ActivityGapEntity x, int sessionIndex) => new()
    {
        Id = x.Id,
        DriverCardId = x.DriverCardId,
        Slot = x.Slot,
        SessionIndex = sessionIndex,
        Start = new GameTime(x.StartGameMinute),
        EndExclusive = x.EndGameMinuteExclusive is null
            ? null
            : new GameTime(x.EndGameMinuteExclusive.Value),
        Reason = x.Reason,
        State = x.State,
        ResolvedAt = x.ResolvedAtGameMinute is null
            ? null
            : new GameTime(x.ResolvedAtGameMinute.Value),
        ProjectionSourceGapId = x.ProjectionSourceGapId
    };

    private sealed record SessionInfo(
        Guid Id,
        int SessionIndex,
        long StartedAtGameMinute);

    private sealed record MaterializedSession(
        int SessionIndex,
        GameTime StartedAt,
        IReadOnlyList<ActivityRecord> Records,
        IReadOnlyList<ActivityGap> Gaps);
}

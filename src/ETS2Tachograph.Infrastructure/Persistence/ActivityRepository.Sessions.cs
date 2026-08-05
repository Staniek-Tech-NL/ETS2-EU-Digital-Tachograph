using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Core.Time;
using Microsoft.EntityFrameworkCore;

namespace ETS2Tachograph.Infrastructure.Persistence;

public sealed partial class ActivityRepository
{
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

}

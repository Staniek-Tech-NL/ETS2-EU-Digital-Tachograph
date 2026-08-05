using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;
using Microsoft.EntityFrameworkCore;

namespace ETS2Tachograph.Infrastructure.Persistence;

public sealed partial class ActivityRepository
{
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

}

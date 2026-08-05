using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Time;
using Microsoft.EntityFrameworkCore;

namespace ETS2Tachograph.Infrastructure.Persistence;

public sealed partial class ActivityRepository
{
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

    private sealed record SessionInfo(
        Guid Id,
        int SessionIndex,
        long StartedAtGameMinute);
}

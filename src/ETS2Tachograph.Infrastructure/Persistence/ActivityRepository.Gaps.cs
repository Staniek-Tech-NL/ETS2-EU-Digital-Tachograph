using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;
using Microsoft.EntityFrameworkCore;

namespace ETS2Tachograph.Infrastructure.Persistence;

public sealed partial class ActivityRepository
{
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
}

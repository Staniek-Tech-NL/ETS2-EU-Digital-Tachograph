using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Application.Services;

public sealed class ActivityGapService(IActivityRepository repository)
{
    public async Task<IReadOnlyList<ActivityGapListItemDto>> GetUnresolvedGapsAsync(
        GameTime currentGameMinute,
        string? driverCardId = null,
        GameTime? fromGameMinute = null,
        GameTime? toGameMinute = null,
        CancellationToken cancellationToken = default)
    {
        var gaps = await repository.GetUnresolvedGapsAsync(
            driverCardId,
            fromGameMinute,
            toGameMinute,
            cancellationToken);

        return Map(gaps, currentGameMinute);
    }

    public async Task<IReadOnlyList<ActivityGapListItemDto>> GetCanonicalGapsAsync(
        GameTime currentGameMinute,
        string? driverCardId = null,
        GameTime? fromGameMinute = null,
        GameTime? toGameMinute = null,
        bool includeResolved = false,
        CancellationToken cancellationToken = default)
    {
        var gaps = await repository.GetCanonicalGapsAsync(
            driverCardId,
            fromGameMinute,
            toGameMinute,
            includeResolved,
            cancellationToken);

        return Map(gaps, currentGameMinute);
    }

    public async Task<ActivityGapListDto> GetListAsync(
        GameTime currentGameMinute,
        bool includeResolved,
        string? driverCardId = null,
        GameTime? fromGameMinute = null,
        GameTime? toGameMinute = null,
        CancellationToken cancellationToken = default)
    {
        var unresolved = await GetUnresolvedGapsAsync(
            currentGameMinute,
            driverCardId,
            fromGameMinute,
            toGameMinute,
            cancellationToken);
        var items = includeResolved
            ? await GetCanonicalGapsAsync(
                currentGameMinute,
                driverCardId,
                fromGameMinute,
                toGameMinute,
                includeResolved: true,
                cancellationToken)
            : unresolved;

        return new ActivityGapListDto(items, unresolved.Count);
    }

    private static IReadOnlyList<ActivityGapListItemDto> Map(
        IEnumerable<ActivityGap> gaps,
        GameTime currentGameMinute) => gaps
        .Select(gap =>
        {
            var end = gap.EndExclusive?.TotalMinutes;
            var effectiveEnd = end ?? currentGameMinute.TotalMinutes;
            return new ActivityGapListItemDto(
                gap.Id,
                gap.DriverCardId,
                gap.Slot,
                gap.Reason,
                gap.State,
                gap.Start.TotalMinutes,
                end,
                Math.Max(0, effectiveEnd - gap.Start.TotalMinutes),
                gap.ResolvedAt?.TotalMinutes);
        })
        .OrderByDescending(gap => gap.StartGameMinute)
        .ThenBy(gap => gap.DriverCardId, StringComparer.OrdinalIgnoreCase)
        .ThenBy(gap => gap.Slot)
        .ToList();
}

using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Application.Services;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Application.Tests;

public sealed class ActivityGapServiceTests
{
    [Fact]
    public async Task Open_gap_duration_uses_current_game_minute_and_is_marked_as_ongoing()
    {
        const long start = (124 * GameClockFormatter.MinutesPerDay) + (8 * 60) + 44;
        var gap = new ActivityGap
        {
            Id = Guid.NewGuid(),
            DriverCardId = "CARD-OPEN-GAP",
            Slot = 2,
            SessionIndex = 4,
            Start = new GameTime(start),
            EndExclusive = null,
            Reason = ActivityGapReason.CardRemoved,
            State = ActivityGapState.Unresolved
        };
        var service = new ActivityGapService(new GapRepository([gap]));

        var dto = Assert.Single(await service.GetUnresolvedGapsAsync(new GameTime(start + 76)));

        Assert.Equal("Dzień 125, 08:44", dto.StartGameTimeText);
        Assert.Equal("TRWA", dto.EndGameTimeText);
        Assert.Equal(76, dto.DurationMinutes);
        Assert.Equal("01:16", dto.DurationText);
        Assert.True(dto.IsOpen);
        Assert.False(dto.IsResolvable);
        Assert.Equal("Karta wyjęta", dto.ReasonText);
        Assert.Equal(ActivityGapState.Unresolved, dto.State);
        Assert.Equal("TRWA", dto.StateText);
        Assert.Equal("karta nadal wyjęta", dto.OngoingHelpText);
    }

    [Fact]
    public async Task Closed_unresolved_gap_has_fixed_duration_and_is_resolvable()
    {
        var gap = new ActivityGap
        {
            Id = Guid.NewGuid(),
            DriverCardId = "CARD-CLOSED-GAP",
            Slot = 1,
            SessionIndex = 0,
            Start = new GameTime(100),
            EndExclusive = new GameTime(145),
            Reason = ActivityGapReason.ForwardTimeJump,
            State = ActivityGapState.Unresolved
        };
        var service = new ActivityGapService(new GapRepository([gap]));

        var dto = Assert.Single(await service.GetUnresolvedGapsAsync(new GameTime(500)));

        Assert.Equal(45, dto.DurationMinutes);
        Assert.Equal("00:45", dto.DurationText);
        Assert.False(dto.IsOpen);
        Assert.True(dto.IsResolvable);
        Assert.Equal("Dzień 1, 02:25", dto.EndGameTimeText);
    }

    [Fact]
    public async Task Canonical_query_can_include_resolved_and_carries_resolution_metadata()
    {
        var unresolved = Gap(100, 145, ActivityGapState.Unresolved);
        var resolved = Gap(200, 260, ActivityGapState.Resolved) with
        {
            ResolvedAt = new GameTime(270)
        };
        var service = new ActivityGapService(new GapRepository([unresolved, resolved]));

        var unresolvedOnly = await service.GetUnresolvedGapsAsync(new GameTime(500));
        var canonicalWithoutResolved = await service.GetCanonicalGapsAsync(
            new GameTime(500),
            includeResolved: false);
        var canonicalWithResolved = await service.GetCanonicalGapsAsync(
            new GameTime(500),
            includeResolved: true);

        Assert.Equal(
            unresolvedOnly.Select(item => item.GapId),
            canonicalWithoutResolved.Select(item => item.GapId));
        Assert.Equal(2, canonicalWithResolved.Count);
        var dto = Assert.Single(canonicalWithResolved, item => item.GapId == resolved.Id);
        Assert.Equal(ActivityGapState.Resolved, dto.State);
        Assert.Equal(270, dto.ResolvedAtGameMinute);
        Assert.Equal("Dzień 1, 04:30", dto.ResolvedAtGameTimeText);
        Assert.StartsWith("ROZLICZONA · ", dto.StateText);
        Assert.False(dto.IsResolvable);
    }

    [Fact]
    public async Task List_keeps_unresolved_count_when_filter_changes_and_sorts_newest_first()
    {
        var oldestUnresolved = Gap(100, 145, ActivityGapState.Unresolved);
        var newestResolved = Gap(300, 360, ActivityGapState.Resolved) with
        {
            ResolvedAt = new GameTime(370)
        };
        var middleUnresolved = Gap(200, 245, ActivityGapState.Unresolved);
        var service = new ActivityGapService(new GapRepository(
            [oldestUnresolved, newestResolved, middleUnresolved]));

        var working = await service.GetListAsync(new GameTime(500), includeResolved: false);
        var audit = await service.GetListAsync(new GameTime(500), includeResolved: true);

        Assert.Equal(2, working.UnresolvedCount);
        Assert.Equal(2, audit.UnresolvedCount);
        Assert.Equal(
            [middleUnresolved.Id, oldestUnresolved.Id],
            working.Items.Select(item => item.GapId).ToArray());
        Assert.Equal(
            [newestResolved.Id, middleUnresolved.Id, oldestUnresolved.Id],
            audit.Items.Select(item => item.GapId).ToArray());
    }

    private static ActivityGap Gap(long from, long to, ActivityGapState state) => new()
    {
        Id = Guid.NewGuid(),
        DriverCardId = "CARD-LIST",
        Slot = 1,
        SessionIndex = 0,
        Start = new GameTime(from),
        EndExclusive = new GameTime(to),
        Reason = ActivityGapReason.CardRemoved,
        State = state
    };

    private sealed class GapRepository(IReadOnlyList<ActivityGap> gaps) : IActivityRepository
    {
        public Task EnsureSessionAsync(
            string driverCardId,
            int sessionIndex,
            GameTime startedAt,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task AppendAsync(
            string driverCardId,
            int sessionIndex,
            IReadOnlyList<ActivityRecord> records,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ApplySessionWritesAsync(
            IReadOnlyList<ActivitySessionWrite> writes,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<ActivityRecord>> LoadDriverHistoryAsync(
            string driverCardId,
            GameTime? from = null,
            GameTime? toExclusive = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ActivityRecord>>([]);

        public Task<IReadOnlyList<StoredActivitySession>> LoadSessionsAsync(
            string driverCardId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StoredActivitySession>>([]);

        public Task<IReadOnlyList<ActivityGap>> GetUnresolvedGapsAsync(
            string? driverCardId = null,
            GameTime? fromGameMinute = null,
            GameTime? toGameMinute = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Filter(driverCardId, fromGameMinute, toGameMinute, includeResolved: false));

        public Task<IReadOnlyList<ActivityGap>> GetCanonicalGapsAsync(
            string? driverCardId,
            GameTime? fromGameMinute,
            GameTime? toGameMinute,
            bool includeResolved,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Filter(driverCardId, fromGameMinute, toGameMinute, includeResolved));

        private IReadOnlyList<ActivityGap> Filter(
            string? driverCardId,
            GameTime? fromGameMinute,
            GameTime? toGameMinute,
            bool includeResolved)
        {
            var from = fromGameMinute?.TotalMinutes ?? long.MinValue;
            var to = toGameMinute?.TotalMinutes ?? long.MaxValue;
            return gaps.Where(gap =>
                    (driverCardId is null || string.Equals(
                        gap.DriverCardId,
                        driverCardId,
                        StringComparison.OrdinalIgnoreCase)) &&
                    (gap.State == ActivityGapState.Unresolved || includeResolved) &&
                    (gap.EndExclusive?.TotalMinutes ?? long.MaxValue) > from &&
                    gap.Start.TotalMinutes < to)
                .ToList();
        }
    }
}

using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Application.Services;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Application.Tests;

public sealed class AutomaticCrewGapCorrectionServiceTests
{
    [Fact]
    public async Task Corrects_both_reference_gaps_with_audited_stable_activity()
    {
        var dobosGap = Gap("Doboś", 2, 202_530, 202_545);
        var staniekGap = Gap("Staniek", 1, 202_736, 202_755);
        var repository = new Repository(
            [dobosGap, staniekGap],
            new Dictionary<string, IReadOnlyList<ActivityRecord>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Doboś"] =
                [
                    Record("Doboś", 202_529, 202_530, DriverActivity.OtherWork),
                    Record("Doboś", 202_545, 202_546, DriverActivity.OtherWork),
                    Record("Doboś", 202_710, 202_736, DriverActivity.BreakOrRest),
                    Record("Doboś", 202_736, 202_755, DriverActivity.BreakOrRest)
                ],
                ["Staniek"] =
                [
                    Record("Staniek", 202_530, 202_545, DriverActivity.BreakOrRest),
                    Record("Staniek", 202_545, 202_730, DriverActivity.BreakOrRest),
                    Record("Staniek", 202_735, 202_736, DriverActivity.Availability),
                    Record("Staniek", 202_755, 202_756, DriverActivity.Availability)
                ]
            });
        var service = new AutomaticCrewGapCorrectionService(
            repository,
            repository,
            new FixedTimeProvider(DateTimeOffset.Parse("2026-07-23T08:00:00+00:00")));

        var result = await service.CorrectAsync(["Staniek", "Doboś"]);

        Assert.Equal(2, result.InspectedGapCount);
        Assert.Equal(2, result.ResolvedGapIds.Count);
        Assert.Collection(
            repository.Writes.OrderBy(write => write.Segments[0].Start),
            write =>
            {
                var record = Assert.Single(write.Segments);
                Assert.Equal(DriverActivity.OtherWork, record.Activity);
                Assert.Equal(ActivitySource.AutomaticCrewReconstruction, record.Source);
                Assert.Equal(dobosGap.Id, record.SourceGapId);
            },
            write =>
            {
                var record = Assert.Single(write.Segments);
                Assert.Equal(DriverActivity.Availability, record.Activity);
                Assert.Equal(ActivitySource.AutomaticCrewReconstruction, record.Source);
                Assert.Equal(staniekGap.Id, record.SourceGapId);
            });
    }

    [Fact]
    public async Task Does_not_correct_when_activity_changes_across_gap()
    {
        var gap = Gap("Doboś", 2, 100, 120);
        var repository = new Repository(
            [gap],
            new Dictionary<string, IReadOnlyList<ActivityRecord>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Doboś"] =
                [
                    Record("Doboś", 99, 100, DriverActivity.OtherWork),
                    Record("Doboś", 120, 121, DriverActivity.Availability)
                ],
                ["Staniek"] = [Record("Staniek", 100, 120, DriverActivity.BreakOrRest)]
            });

        var result = await new AutomaticCrewGapCorrectionService(repository, repository)
            .CorrectAsync(["Staniek", "Doboś"]);

        Assert.Empty(result.ResolvedGapIds);
        Assert.Empty(repository.Writes);
    }

    private static ActivityGap Gap(string card, int slot, long start, long end) => new()
    {
        Id = Guid.NewGuid(),
        DriverCardId = card,
        Slot = slot,
        SessionIndex = 0,
        Start = new GameTime(start),
        EndExclusive = new GameTime(end),
        Reason = ActivityGapReason.ForwardTimeJump,
        State = ActivityGapState.Unresolved
    };

    private static ActivityRecord Record(
        string card,
        long start,
        long end,
        DriverActivity activity) => new()
    {
        Id = Guid.NewGuid(),
        DriverCardId = card,
        Start = new GameTime(start),
        EndExclusive = new GameTime(end),
        Activity = activity,
        Source = ActivitySource.Telemetry,
        RecordedAtUtc = DateTimeOffset.UtcNow
    };

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class Repository(
        IReadOnlyList<ActivityGap> gaps,
        IReadOnlyDictionary<string, IReadOnlyList<ActivityRecord>> histories) :
        IActivityRepository,
        IManualEntryRepository
    {
        public List<ManualEntryResolutionWrite> Writes { get; } = [];

        public Task<IReadOnlyList<ActivityGap>> GetUnresolvedGapsAsync(
            string? driverCardId = null,
            GameTime? fromGameMinute = null,
            GameTime? toGameMinute = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ActivityGap>>(gaps
                .Where(gap => driverCardId is null ||
                    string.Equals(gap.DriverCardId, driverCardId, StringComparison.OrdinalIgnoreCase))
                .ToList());

        public Task<ManualEntryGapContext?> LoadGapContextAsync(
            Guid gapId,
            CancellationToken cancellationToken = default)
        {
            var gap = gaps.SingleOrDefault(item => item.Id == gapId);
            if (gap is null)
                return Task.FromResult<ManualEntryGapContext?>(null);
            return Task.FromResult<ManualEntryGapContext?>(new(
                gap,
                true,
                true,
                histories[gap.DriverCardId],
                []));
        }

        public Task<ManualEntryPersistenceResult> ApplyGapResolutionAsync(
            ManualEntryResolutionWrite write,
            CancellationToken cancellationToken = default)
        {
            Writes.Add(write);
            return Task.FromResult(new ManualEntryPersistenceResult(
                ManualEntryPersistenceStatus.Applied,
                gaps.Single(gap => gap.Id == write.GapId) with
                {
                    State = ActivityGapState.Resolved,
                    ResolvedAt = write.ResolvedAt
                },
                write.Segments));
        }

        public Task<IReadOnlyList<ActivityRecord>> LoadDriverHistoryAsync(
            string driverCardId,
            GameTime? from = null,
            GameTime? toExclusive = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ActivityRecord>>(histories[driverCardId]
                .Where(record =>
                    (from is null || record.EndExclusive > from.Value) &&
                    (toExclusive is null || record.Start < toExclusive.Value))
                .ToList());

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

        public Task<IReadOnlyList<StoredActivitySession>> LoadSessionsAsync(
            string driverCardId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StoredActivitySession>>([]);
    }
}

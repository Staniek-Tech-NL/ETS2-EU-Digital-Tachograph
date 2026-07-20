using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Application.Services;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Application.Tests;

public sealed class ManualEntryServiceTests
{
    [Fact]
    public async Task Full_coverage_with_three_allowed_activities_resolves_gap()
    {
        var repository = new FakeManualEntryRepository(Gap());
        var service = Service(repository);

        var result = await service.ResolveGapAsync(
            repository.Gap.Id,
            [
                Segment(100, 120, DriverActivity.BreakOrRest),
                Segment(120, 140, DriverActivity.OtherWork),
                Segment(140, 160, DriverActivity.Availability)
            ],
            new GameTime(500));

        Assert.Equal(ResolveGapStatus.Resolved, result.Status);
        Assert.Equal(ActivityGapState.Resolved, result.Gap.State);
        Assert.Equal(new GameTime(500), result.Gap.ResolvedAt);
        Assert.All(result.Segments, record =>
        {
            Assert.Equal(ActivitySource.ManualEntry, record.Source);
            Assert.Equal(repository.Gap.Id, record.SourceGapId);
        });
    }

    [Fact]
    public async Task Resolve_gap_recalculates_daily_counters_at_retroactive_rest_stamp()
    {
        var gap = Gap() with
        {
            Start = new GameTime(60),
            EndExclusive = new GameTime(600)
        };
        ActivityRecord[] measuredHistory =
        [
            Record(0, 60, DriverActivity.Driving),
            Record(600, 660, DriverActivity.Driving)
        ];
        var repository = new FakeManualEntryRepository(gap, measuredHistory);

        var result = await Service(repository).ResolveGapAsync(
            gap.Id,
            [Segment(60, 600, DriverActivity.BreakOrRest)],
            new GameTime(660));

        Assert.Equal(new GameTime(600), result.Evaluation.State.LastDailyRestResetAt);
        Assert.Equal(60, result.Evaluation.State.DailyDrivingMinutes);
        Assert.Equal(60, result.Evaluation.State.DailyWorkMinutes);
        Assert.Contains(result.Evaluation.QualifiedRests, rest =>
            rest.SourceGapId == gap.Id && rest.DurationMinutes == 540);
    }

    [Theory]
    [InlineData(DriverActivity.Driving)]
    [InlineData(DriverActivity.OutOfScope)]
    [InlineData(DriverActivity.Unknown)]
    public async Task Unsupported_segment_activity_is_rejected_before_persistence(
        DriverActivity activity)
    {
        var repository = new FakeManualEntryRepository(Gap());
        var service = Service(repository);

        var exception = await Assert.ThrowsAsync<ManualEntryValidationException>(() =>
            service.ResolveGapAsync(
                repository.Gap.Id,
                [Segment(100, 160, activity)],
                new GameTime(500)));

        Assert.Equal(ManualEntryError.InvalidActivity, exception.Error);
        Assert.Equal(0, repository.AppliedWrites);
    }

    [Fact]
    public void Existing_activity_inside_gap_is_rejected_as_history_collision()
    {
        var gap = Gap();
        var existing = new ActivityRecord
        {
            Id = Guid.NewGuid(),
            DriverCardId = gap.DriverCardId,
            Activity = DriverActivity.OtherWork,
            Start = new GameTime(120),
            EndExclusive = new GameTime(121),
            RecordedAtUtc = DateTimeOffset.UtcNow,
            Source = ActivitySource.Telemetry
        };

        var exception = Assert.Throws<ManualEntryValidationException>(() =>
            ManualEntryValidator.Validate(
                gap,
                [Segment(100, 160, DriverActivity.BreakOrRest)],
                [existing]));

        Assert.Equal(ManualEntryError.HistoryCollision, exception.Error);
    }

    [Fact]
    public async Task Incomplete_coverage_is_rejected()
    {
        var repository = new FakeManualEntryRepository(Gap());

        var exception = await Assert.ThrowsAsync<ManualEntryValidationException>(() =>
            Service(repository).ResolveGapAsync(
                repository.Gap.Id,
                [
                    Segment(100, 120, DriverActivity.BreakOrRest),
                    Segment(130, 160, DriverActivity.OtherWork)
                ],
                new GameTime(500)));

        Assert.Equal(ManualEntryError.IncompleteCoverage, exception.Error);
        Assert.Equal(0, repository.AppliedWrites);
    }

    [Fact]
    public async Task Segment_outside_gap_is_rejected()
    {
        var repository = new FakeManualEntryRepository(Gap());

        var exception = await Assert.ThrowsAsync<ManualEntryValidationException>(() =>
            Service(repository).ResolveGapAsync(
                repository.Gap.Id,
                [Segment(99, 160, DriverActivity.BreakOrRest)],
                new GameTime(500)));

        Assert.Equal(ManualEntryError.OutsideGap, exception.Error);
    }

    [Fact]
    public async Task Overlapping_segments_are_rejected()
    {
        var repository = new FakeManualEntryRepository(Gap());

        var exception = await Assert.ThrowsAsync<ManualEntryValidationException>(() =>
            Service(repository).ResolveGapAsync(
                repository.Gap.Id,
                [
                    Segment(100, 130, DriverActivity.BreakOrRest),
                    Segment(120, 160, DriverActivity.Availability)
                ],
                new GameTime(500)));

        Assert.Equal(ManualEntryError.OverlappingSegments, exception.Error);
    }

    [Fact]
    public async Task Identical_second_resolution_is_rejected_idempotently_without_new_write()
    {
        var repository = new FakeManualEntryRepository(Gap());
        var service = Service(repository);
        ManualEntrySegment[] segments =
        [
            Segment(100, 130, DriverActivity.BreakOrRest),
            Segment(130, 160, DriverActivity.OtherWork)
        ];

        var first = await service.ResolveGapAsync(
            repository.Gap.Id, segments, new GameTime(500));
        var second = await service.ResolveGapAsync(
            repository.Gap.Id, segments, new GameTime(501));

        Assert.Equal(ResolveGapStatus.Resolved, first.Status);
        Assert.Equal(ResolveGapStatus.AlreadyResolved, second.Status);
        Assert.Equal(1, repository.AppliedWrites);
        Assert.Equal(first.Segments.Select(Semantic), second.Segments.Select(Semantic));
    }

    [Fact]
    public async Task Segment_duration_sum_equals_gap_length_to_the_minute()
    {
        var repository = new FakeManualEntryRepository(Gap());

        var result = await Service(repository).ResolveGapAsync(
            repository.Gap.Id,
            [
                Segment(100, 101, DriverActivity.BreakOrRest),
                Segment(101, 159, DriverActivity.OtherWork),
                Segment(159, 160, DriverActivity.Availability)
            ],
            new GameTime(500));

        Assert.Equal(repository.Gap.DurationMinutes, result.Segments.Sum(record => record.DurationMinutes));
    }

    [Fact]
    public async Task Different_second_resolution_is_logged_and_rejected_as_conflict()
    {
        var repository = new FakeManualEntryRepository(Gap());
        var diagnostics = new RecordingDiagnostics();
        var service = new ManualEntryService(repository, diagnostics, new FixedTimeProvider());
        await service.ResolveGapAsync(
            repository.Gap.Id,
            [Segment(100, 160, DriverActivity.BreakOrRest)],
            new GameTime(500));

        var exception = await Assert.ThrowsAsync<ManualEntryValidationException>(() =>
            service.ResolveGapAsync(
                repository.Gap.Id,
                [Segment(100, 160, DriverActivity.OtherWork)],
                new GameTime(501)));

        Assert.Equal(ManualEntryError.ResolutionConflict, exception.Error);
        Assert.Equal(repository.Gap.Id, Assert.Single(diagnostics.Conflicts));
    }

    private static ManualEntryService Service(FakeManualEntryRepository repository) =>
        new(repository, timeProvider: new FixedTimeProvider());

    private static ManualEntrySegment Segment(long from, long to, DriverActivity activity) =>
        new(from, to, activity);

    private static ActivityGap Gap() => new()
    {
        Id = Guid.NewGuid(),
        DriverCardId = "CARD-MANUAL",
        Slot = 1,
        SessionIndex = 0,
        Start = new GameTime(100),
        EndExclusive = new GameTime(160),
        Reason = ActivityGapReason.CardRemoved,
        State = ActivityGapState.Unresolved
    };

    private static ActivityRecord Record(long from, long to, DriverActivity activity) => new()
    {
        Id = Guid.NewGuid(),
        DriverCardId = "CARD-MANUAL",
        Activity = activity,
        Start = new GameTime(from),
        EndExclusive = new GameTime(to),
        RecordedAtUtc = DateTimeOffset.UtcNow,
        Source = ActivitySource.Telemetry
    };

    private static object Semantic(ActivityRecord record) => new
    {
        record.Start,
        record.EndExclusive,
        record.Activity,
        record.SourceGapId
    };

    private sealed class FakeManualEntryRepository(
        ActivityGap gap,
        IReadOnlyList<ActivityRecord>? records = null) : IManualEntryRepository
    {
        private IReadOnlyList<ActivityRecord> _records = records ?? [];
        private IReadOnlyList<ActivityRecord> _resolutionRecords = [];

        public ActivityGap Gap { get; private set; } = gap;
        public int AppliedWrites { get; private set; }

        public Task<ManualEntryGapContext?> LoadGapContextAsync(
            Guid gapId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ManualEntryGapContext?>(gapId == Gap.Id
                ? new ManualEntryGapContext(Gap, true, true, _records, _resolutionRecords)
                : null);

        public Task<ManualEntryPersistenceResult> ApplyGapResolutionAsync(
            ManualEntryResolutionWrite write,
            CancellationToken cancellationToken = default)
        {
            if (Gap.State == ActivityGapState.Resolved)
            {
                var same = _resolutionRecords.Count == write.Segments.Count &&
                    _resolutionRecords.OrderBy(record => record.Start)
                        .Zip(write.Segments.OrderBy(record => record.Start))
                        .All(pair => Semantic(pair.First).Equals(Semantic(pair.Second)));
                return Task.FromResult(new ManualEntryPersistenceResult(
                    same ? ManualEntryPersistenceStatus.AlreadyApplied : ManualEntryPersistenceStatus.Conflict,
                    Gap,
                    _resolutionRecords));
            }

            AppliedWrites++;
            _resolutionRecords = write.Segments.ToList();
            _records = _records
                .Where(record => record.SourceGapId != Gap.Id)
                .Concat(_resolutionRecords)
                .OrderBy(record => record.Start)
                .ToList();
            Gap = Gap with
            {
                State = ActivityGapState.Resolved,
                ResolvedAt = write.ResolvedAt
            };
            return Task.FromResult(new ManualEntryPersistenceResult(
                ManualEntryPersistenceStatus.Applied,
                Gap,
                _resolutionRecords));
        }
    }

    private sealed class RecordingDiagnostics : IManualEntryDiagnostics
    {
        public List<Guid> Conflicts { get; } = [];

        public void RecordResolutionConflict(
            Guid gapId,
            IReadOnlyList<ActivityRecord> existing,
            IReadOnlyList<ActivityRecord> incoming) => Conflicts.Add(gapId);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() =>
            new(2026, 7, 17, 13, 0, 0, TimeSpan.Zero);
    }
}

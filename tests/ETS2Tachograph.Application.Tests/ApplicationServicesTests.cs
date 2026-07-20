using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Application.Services;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Application.Tests;

public sealed class ApplicationServicesTests
{
    [Fact]
    public async Task Driver_service_validates_creates_and_activates_profile()
    {
        var repository = new FakeDriverRepository();
        var service = new DriverService(repository);
        var created = await service.CreateProfileAsync(new CreateDriverProfileDto(
            "Jan Kowalski", new DriverCardDto(
                "PL-123", "PL", new DateOnly(2026, 1, 1), new DateOnly(2031, 1, 1))));

        await service.SetActiveProfileAsync(created.Id);

        Assert.Equal(created.Id, (await service.GetActiveProfileAsync())!.Id);
    }

    [Fact]
    public async Task Tacho_export_and_import_round_trip_preserves_session()
    {
        var source = new FakeActivityRepository();
        await source.EnsureSessionAsync("PL-123", 2, new GameTime(100));
        await source.AppendAsync("PL-123", 2, [Record(100)]);
        await using var stream = new MemoryStream();
        await new ExportService(source).ExportSessionAsync("PL-123", stream);
        stream.Position = 0;
        var destination = new FakeActivityRepository();

        var imported = await new ImportService(destination).ImportSessionAsync(stream);

        Assert.Equal(1, imported);
        var session = Assert.Single(await destination.LoadSessionsAsync("PL-123"));
        Assert.Equal(2, session.SessionIndex);
        Assert.Equal(DriverActivity.Driving, Assert.Single(session.Records).Activity);
    }

    [Fact]
    public async Task Tacho_export_and_import_preserves_resolved_gap_audit_link()
    {
        var source = new FakeActivityRepository();
        var gap = new ActivityGap
        {
            Id = Guid.NewGuid(),
            DriverCardId = "PL-123",
            Slot = 1,
            SessionIndex = 0,
            Start = new GameTime(10),
            EndExclusive = new GameTime(20),
            Reason = ActivityGapReason.CardRemoved,
            State = ActivityGapState.Resolved,
            ResolvedAt = new GameTime(30)
        };
        var manual = Record(10) with
        {
            Activity = DriverActivity.BreakOrRest,
            EndExclusive = new GameTime(20),
            Source = ActivitySource.ManualEntry,
            SourceGapId = gap.Id
        };
        await source.ApplySessionWritesAsync(
        [
            new ActivitySessionWrite("PL-123", 0, new GameTime(10), [manual], [gap])
        ]);
        await using var stream = new MemoryStream();
        await new ExportService(source).ExportSessionAsync("PL-123", stream);
        stream.Position = 0;
        var destination = new FakeActivityRepository();

        await new ImportService(destination).ImportSessionAsync(stream);

        var session = Assert.Single(await destination.LoadSessionsAsync("PL-123"));
        var importedGap = Assert.Single(session.Gaps!);
        var importedRecord = Assert.Single(session.Records);
        Assert.Equal(ActivityGapState.Resolved, importedGap.State);
        Assert.Equal(new GameTime(30), importedGap.ResolvedAt);
        Assert.Equal(importedGap.Id, importedRecord.SourceGapId);
        Assert.Equal(ActivitySource.ManualEntry, importedRecord.Source);
    }

    private static ActivityRecord Record(long minute) => new()
    {
        Id = Guid.NewGuid(),
        DriverCardId = "PL-123",
        Activity = DriverActivity.Driving,
        Start = new GameTime(minute),
        EndExclusive = new GameTime(minute + 1),
        RecordedAtUtc = DateTimeOffset.UtcNow,
        Source = ActivitySource.Telemetry
    };

    private sealed class FakeActivityRepository : IActivityRepository
    {
        private readonly Dictionary<int, StoredActivitySession> _sessions = [];
        public Task EnsureSessionAsync(string driverCardId, int sessionIndex, GameTime startedAt, CancellationToken cancellationToken = default)
        { _sessions.TryAdd(sessionIndex, new StoredActivitySession(sessionIndex, startedAt, [])); return Task.CompletedTask; }
        public Task AppendAsync(string driverCardId, int sessionIndex, IReadOnlyList<ActivityRecord> records, CancellationToken cancellationToken = default)
        { var old = _sessions[sessionIndex]; _sessions[sessionIndex] = old with { Records = old.Records.Concat(records).DistinctBy(x => x.Id).ToList() }; return Task.CompletedTask; }
        public async Task ApplySessionWritesAsync(IReadOnlyList<ActivitySessionWrite> writes, CancellationToken cancellationToken = default)
        {
            foreach (var write in writes)
            {
                await EnsureSessionAsync(write.DriverCardId, write.SessionIndex, write.StartedAt, cancellationToken);
                await AppendAsync(write.DriverCardId, write.SessionIndex, write.Records, cancellationToken);
                if (write.Gaps is { Count: > 0 })
                {
                    var old = _sessions[write.SessionIndex];
                    _sessions[write.SessionIndex] = old with
                    {
                        Gaps = (old.Gaps ?? []).Concat(write.Gaps)
                            .DistinctBy(gap => gap.Id)
                            .ToList()
                    };
                }
            }
        }
        public Task<IReadOnlyList<ActivityRecord>> LoadDriverHistoryAsync(string driverCardId, GameTime? from = null, GameTime? toExclusive = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ActivityRecord>>(_sessions.Values.SelectMany(x => x.Records).ToList());
        public Task<IReadOnlyList<StoredActivitySession>> LoadSessionsAsync(string driverCardId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StoredActivitySession>>(_sessions.Values.ToList());
    }

    private sealed class FakeDriverRepository : IDriverRepository
    {
        private readonly List<DriverProfileDto> _items = [];
        public Task<IReadOnlyList<DriverProfileDto>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<DriverProfileDto>>(_items);
        public Task<DriverProfileDto?> GetActiveAsync(CancellationToken cancellationToken = default) => Task.FromResult(_items.SingleOrDefault(x => x.IsActive));
        public Task<DriverProfileDto> CreateAsync(CreateDriverProfileDto profile, CancellationToken cancellationToken = default)
        { var item = new DriverProfileDto(Guid.NewGuid(), profile.DisplayName, false, DateTimeOffset.UtcNow, [profile.Card]); _items.Add(item); return Task.FromResult(item); }
        public Task SetActiveAsync(Guid profileId, CancellationToken cancellationToken = default)
        { for (var i = 0; i < _items.Count; i++) _items[i] = _items[i] with { IsActive = _items[i].Id == profileId }; return Task.CompletedTask; }
    }
}

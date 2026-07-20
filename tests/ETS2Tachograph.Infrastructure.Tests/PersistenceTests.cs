using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.Infrastructure.Persistence;
using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Application.Services;
using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Core.Telemetry;
using ETS2Tachograph.Engine;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ETS2Tachograph.Infrastructure.Tests;

public sealed class PersistenceTests
{
    [Fact]
    public async Task Migrations_create_an_empty_activity_gaps_table()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TachographDbContext>().UseSqlite(connection).Options;
        await using var context = new TachographDbContext(options);

        await context.Database.MigrateAsync();

        Assert.Empty(await context.ActivityGaps.AsNoTracking().ToListAsync());
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task Unit_of_work_persists_driver_session_and_mapped_activity_atomically()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TachographDbContext>().UseSqlite(connection).Options;
        await using var context = new TachographDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var drivers = new DriverRepository(context);
        var activities = new ActivityRepository(context);
        var unitOfWork = new EfUnitOfWork(context);
        var profileId = Guid.NewGuid();

        await unitOfWork.ExecuteInTransactionAsync(_ =>
        {
            context.DriverProfiles.Add(new DriverProfileEntity
            {
                Id = profileId,
                DisplayName = "Kierowca testowy",
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                Cards = [new DriverCardEntity
                {
                    Id = "PL-TEST",
                    CountryCode = "PL",
                    ValidFrom = new DateOnly(2026, 1, 1),
                    ValidUntil = new DateOnly(2031, 1, 1)
                }]
            });
            return Task.CompletedTask;
        });

        await activities.EnsureSessionAsync("PL-TEST", 0, new GameTime(100));
        await activities.AppendAsync("PL-TEST", 0, [new ActivityRecord
        {
            Id = Guid.NewGuid(), DriverCardId = "PL-TEST", Activity = DriverActivity.Driving,
            Start = new GameTime(100), EndExclusive = new GameTime(101),
            RecordedAtUtc = DateTimeOffset.UtcNow, Source = ActivitySource.Telemetry
        }]);

        context.ChangeTracker.Clear();
        Assert.NotNull(await drivers.GetActiveAsync());
        Assert.Single(await activities.LoadDriverHistoryAsync("PL-TEST", new GameTime(0), new GameTime(200)));
    }

    [Fact]
    public async Task Tachograph_service_saves_every_closed_minute_and_loads_it_after_restart()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TachographDbContext>().UseSqlite(connection).Options;
        await using var context = new TachographDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.DriverProfiles.Add(new DriverProfileEntity
        {
            Id = Guid.NewGuid(),
            DisplayName = "Test",
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Cards = [new DriverCardEntity
            {
                Id = "PL-LIVE", CountryCode = "PL", ValidFrom = new DateOnly(2026, 1, 1),
                ValidUntil = new DateOnly(2031, 1, 1)
            }]
        });
        await context.SaveChangesAsync();
        var service = new TachographService(
            "PL-LIVE", new TachographEngine("PL-LIVE"), new ActivityRepository(context));
        var epoch = DateTimeOffset.UtcNow;

        await service.ProcessFrameAsync(new TelemetryFrame(new GameTime(0), epoch, 20, false));
        await service.ProcessFrameAsync(new TelemetryFrame(new GameTime(1), epoch.AddSeconds(1), 20, false));
        await service.ProcessFrameAsync(new TelemetryFrame(new GameTime(2), epoch.AddSeconds(2), 20, false));

        context.ChangeTracker.Clear();
        var restored = await service.LoadDriverHistoryAsync();
        Assert.NotEmpty(restored);
        Assert.All(restored, record => Assert.Equal(DriverActivity.Driving, record.Activity));
    }

    [Fact]
    public async Task Settings_are_persisted_in_sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TachographDbContext>().UseSqlite(connection).Options;
        await using var context = new TachographDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var repository = new SettingsRepository(context);

        await repository.SaveAsync(new ETS2Tachograph.Application.Dtos.SettingsDto(2.5, 1));
        context.ChangeTracker.Clear();
        var restored = await repository.LoadAsync();

        Assert.Equal(2.5, restored.DrivingSpeedThresholdKph);
        Assert.Equal(1, restored.WeekEpochOffsetDays);
    }

    [Fact]
    public async Task Logical_history_replaces_only_abandoned_future_after_clock_rollback()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TachographDbContext>().UseSqlite(connection).Options;
        await using var context = new TachographDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.DriverProfiles.Add(new DriverProfileEntity
        {
            Id = Guid.NewGuid(),
            DisplayName = "Kierowca testowy",
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Cards = [new DriverCardEntity
            {
                Id = "PL-BRANCH",
                CountryCode = "PL",
                ValidFrom = new DateOnly(2026, 1, 1),
                ValidUntil = new DateOnly(2031, 1, 1)
            }]
        });
        await context.SaveChangesAsync();
        var repository = new ActivityRepository(context);
        await repository.EnsureSessionAsync("PL-BRANCH", 0, new GameTime(0));
        await repository.AppendAsync("PL-BRANCH", 0,
        [
            Record("PL-BRANCH", 0, DriverActivity.Driving),
            Record("PL-BRANCH", 1, DriverActivity.Driving),
            Record("PL-BRANCH", 2, DriverActivity.Availability)
        ]);
        await repository.EnsureSessionAsync("PL-BRANCH", 1, new GameTime(2));
        await repository.AppendAsync("PL-BRANCH", 1,
        [
            Record("PL-BRANCH", 2, DriverActivity.OtherWork),
            Record("PL-BRANCH", 3, DriverActivity.Driving)
        ]);

        context.ChangeTracker.Clear();
        var history = await repository.LoadDriverHistoryAsync("PL-BRANCH");

        Assert.Equal(4, history.Count);
        var branchMinute = Assert.Single(history, x => x.Start == new GameTime(2));
        Assert.Equal(DriverActivity.OtherWork, branchMinute.Activity);
    }

    [Fact]
    public async Task Reload_rollback_persists_both_card_branches_without_unique_constraint_failure()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TachographDbContext>().UseSqlite(connection).Options;
        await using var context = new TachographDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.DriverProfiles.Add(new DriverProfileEntity
        {
            Id = Guid.NewGuid(),
            DisplayName = "Podwójna obsada",
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Cards =
            [
                new DriverCardEntity
                {
                    Id = "CARD-A", CountryCode = "PL",
                    ValidFrom = new DateOnly(2026, 1, 1), ValidUntil = new DateOnly(2031, 1, 1)
                },
                new DriverCardEntity
                {
                    Id = "CARD-B", CountryCode = "PL",
                    ValidFrom = new DateOnly(2026, 1, 1), ValidUntil = new DateOnly(2031, 1, 1)
                }
            ]
        });
        await context.SaveChangesAsync();
        var repository = new ActivityRepository(context);
        var crew = new CrewTachographEngine();
        var service = new CrewTachographService(crew, repository);
        await service.RegisterCardAsync("CARD-A");
        await service.RegisterCardAsync("CARD-B");
        service.InsertCard(TachographSlot.Driver, "CARD-A");
        service.InsertCard(TachographSlot.CoDriver, "CARD-B");
        var epoch = DateTimeOffset.UtcNow;

        await service.ProcessFrameAsync(Frame(100, 0, 30));
        await service.ProcessFrameAsync(Frame(101, 1, 30));
        await service.ProcessFrameAsync(Frame(99, 2, 0));
        await service.ProcessFrameAsync(Frame(100, 3, 0));
        await service.ProcessFrameAsync(Frame(101, 4, 0));
        await service.ProcessFrameAsync(Frame(102, 5, 0));

        context.ChangeTracker.Clear();
        foreach (var cardId in new[] { "CARD-A", "CARD-B" })
        {
            var sessions = await repository.LoadRawSessionsAsync(cardId);
            Assert.Equal([0, 1], sessions.Select(x => x.SessionIndex));
            Assert.Equal(new GameTime(100), sessions[0].StartedAt);
            Assert.Equal(new GameTime(99), sessions[1].StartedAt);
            Assert.Contains(sessions[0].Records, record => record.Start == new GameTime(100));
            Assert.Contains(sessions[1].Records, record => record.Start == new GameTime(100));
            Assert.All(sessions, session => Assert.Equal(
                session.Records.Count,
                session.Records.Select(record => record.Start).Distinct().Count()));

            var canonical = await repository.LoadDriverHistoryAsync(cardId);
            Assert.Single(canonical, record => record.Start == new GameTime(100));
        }

        TelemetryFrame Frame(long gameMinute, int recordedSecond, double speed) =>
            new(new GameTime(gameMinute), epoch.AddSeconds(recordedSecond), speed, GamePaused: false);
    }

    [Fact]
    public async Task Multi_card_session_writes_roll_back_as_one_transaction()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TachographDbContext>().UseSqlite(connection).Options;
        await using var context = new TachographDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.DriverProfiles.Add(new DriverProfileEntity
        {
            Id = Guid.NewGuid(),
            DisplayName = "Jedna karta",
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Cards = [new DriverCardEntity
            {
                Id = "CARD-A", CountryCode = "PL",
                ValidFrom = new DateOnly(2026, 1, 1), ValidUntil = new DateOnly(2031, 1, 1)
            }]
        });
        await context.SaveChangesAsync();
        var repository = new ActivityRepository(context);

        await Assert.ThrowsAsync<DbUpdateException>(() => repository.ApplySessionWritesAsync(
        [
            new ActivitySessionWrite(
                "CARD-A", 0, new GameTime(10),
                [Record("CARD-A", 10, DriverActivity.Driving)]),
            new ActivitySessionWrite("MISSING-CARD", 0, new GameTime(10), [])
        ]));

        context.ChangeTracker.Clear();
        Assert.Empty(await context.ActivitySessions.AsNoTracking().ToListAsync());
        Assert.Empty(await context.ActivityRecords.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Meaningful_session_and_minute_key_is_idempotent_and_reports_only_real_conflicts()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TachographDbContext>().UseSqlite(connection).Options;
        await using var context = new TachographDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.DriverProfiles.Add(new DriverProfileEntity
        {
            Id = Guid.NewGuid(),
            DisplayName = "Idempotentność",
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Cards = [new DriverCardEntity
            {
                Id = "CARD-IDEMPOTENT", CountryCode = "PL",
                ValidFrom = new DateOnly(2026, 1, 1), ValidUntil = new DateOnly(2031, 1, 1)
            }]
        });
        await context.SaveChangesAsync();
        var diagnostics = new RecordingPersistenceDiagnostics();
        var repository = new ActivityRepository(context, diagnostics);
        await repository.EnsureSessionAsync("CARD-IDEMPOTENT", 0, new GameTime(10));
        var original = Record("CARD-IDEMPOTENT", 10, DriverActivity.Driving);

        await repository.AppendAsync("CARD-IDEMPOTENT", 0, [original]);
        await repository.AppendAsync("CARD-IDEMPOTENT", 0,
            [original with { Id = Guid.NewGuid(), RecordedAtUtc = original.RecordedAtUtc.AddMinutes(5) }]);

        Assert.Empty(diagnostics.Conflicts);
        var afterReplay = Assert.Single(await repository.LoadRawSessionsAsync("CARD-IDEMPOTENT"));
        Assert.Equal(original.Id, Assert.Single(afterReplay.Records).Id);

        await repository.AppendAsync("CARD-IDEMPOTENT", 0,
        [
            original with
            {
                Id = Guid.NewGuid(),
                Activity = DriverActivity.OtherWork,
                RecordedAtUtc = original.RecordedAtUtc.AddMinutes(10)
            }
        ]);

        var conflict = Assert.Single(diagnostics.Conflicts);
        Assert.Equal("CARD-IDEMPOTENT", conflict.CardId);
        Assert.Equal(0, conflict.SessionIndex);
        Assert.Equal(new GameTime(10), conflict.Existing.Start);
        Assert.Equal(DriverActivity.Driving, conflict.Existing.Activity);
        Assert.Equal(DriverActivity.OtherWork, conflict.Incoming.Activity);
        var stored = Assert.Single((await repository.LoadRawSessionsAsync("CARD-IDEMPOTENT")).Single().Records);
        Assert.Equal(original.Id, stored.Id);
        Assert.Equal(DriverActivity.Driving, stored.Activity);
    }

    [Fact]
    public async Task Conflict_on_one_card_does_not_block_valid_write_for_the_other_card()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TachographDbContext>().UseSqlite(connection).Options;
        await using var context = new TachographDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.DriverProfiles.Add(new DriverProfileEntity
        {
            Id = Guid.NewGuid(),
            DisplayName = "Podwójny zapis",
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Cards =
            [
                new DriverCardEntity
                {
                    Id = "CARD-A", CountryCode = "PL",
                    ValidFrom = new DateOnly(2026, 1, 1), ValidUntil = new DateOnly(2031, 1, 1)
                },
                new DriverCardEntity
                {
                    Id = "CARD-B", CountryCode = "PL",
                    ValidFrom = new DateOnly(2026, 1, 1), ValidUntil = new DateOnly(2031, 1, 1)
                }
            ]
        });
        await context.SaveChangesAsync();
        var diagnostics = new RecordingPersistenceDiagnostics();
        var repository = new ActivityRepository(context, diagnostics);
        await repository.ApplySessionWritesAsync(
        [
            new ActivitySessionWrite(
                "CARD-A", 0, new GameTime(20),
                [Record("CARD-A", 20, DriverActivity.Driving)]),
            new ActivitySessionWrite("CARD-B", 0, new GameTime(20), [])
        ]);

        await repository.ApplySessionWritesAsync(
        [
            new ActivitySessionWrite(
                "CARD-A", 0, new GameTime(20),
                [Record("CARD-A", 20, DriverActivity.OtherWork)]),
            new ActivitySessionWrite(
                "CARD-B", 0, new GameTime(20),
                [Record("CARD-B", 20, DriverActivity.Availability)])
        ]);

        Assert.Single(diagnostics.Conflicts);
        Assert.Equal(
            DriverActivity.Driving,
            Assert.Single((await repository.LoadRawSessionsAsync("CARD-A")).Single().Records).Activity);
        Assert.Equal(
            DriverActivity.Availability,
            Assert.Single((await repository.LoadRawSessionsAsync("CARD-B")).Single().Records).Activity);
    }

    [Fact]
    public async Task World_generation_boundary_allows_same_game_minute_in_a_new_session()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TachographDbContext>().UseSqlite(connection).Options;
        await using var context = new TachographDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.DriverProfiles.Add(new DriverProfileEntity
        {
            Id = Guid.NewGuid(),
            DisplayName = "Wczytanie świata",
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Cards = [new DriverCardEntity
            {
                Id = "CARD-WORLD", CountryCode = "PL",
                ValidFrom = new DateOnly(2026, 1, 1), ValidUntil = new DateOnly(2031, 1, 1)
            }]
        });
        await context.SaveChangesAsync();
        var repository = new ActivityRepository(context);
        var crew = new CrewTachographEngine();
        var service = new CrewTachographService(crew, repository);
        await service.RegisterCardAsync("CARD-WORLD");
        service.InsertCard(TachographSlot.Driver, "CARD-WORLD");
        var epoch = DateTimeOffset.UtcNow;

        await service.ProcessFrameAsync(Frame(100, 0, 5));
        await service.ProcessFrameAsync(Frame(101, 1, 5));
        await service.ProcessFrameAsync(Frame(101, 2, 8));
        await service.ProcessFrameAsync(Frame(102, 3, 8));
        await service.ProcessFrameAsync(Frame(103, 4, 8));

        context.ChangeTracker.Clear();
        var sessions = await repository.LoadRawSessionsAsync("CARD-WORLD");
        Assert.Equal([0, 1], sessions.Select(x => x.SessionIndex));
        Assert.Equal(new GameTime(101), sessions[1].StartedAt);
        Assert.Contains(sessions[0].Records, record => record.Start == new GameTime(101));
        Assert.Contains(sessions[1].Records, record => record.Start == new GameTime(101));
        Assert.Single(
            await repository.LoadDriverHistoryAsync("CARD-WORLD"),
            record => record.Start == new GameTime(101));

        TelemetryFrame Frame(long gameMinute, int recordedSecond, uint worldGeneration) =>
            new(
                new GameTime(gameMinute),
                epoch.AddSeconds(recordedSecond),
                SpeedKph: 0,
                GamePaused: false,
                WorldGeneration: worldGeneration);
    }

    [Fact]
    public async Task Forward_time_jump_gap_is_persisted_as_a_first_class_entity()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TachographDbContext>().UseSqlite(connection).Options;
        await using var context = new TachographDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.DriverProfiles.Add(new DriverProfileEntity
        {
            Id = Guid.NewGuid(),
            DisplayName = "Jawna luka",
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Cards = [new DriverCardEntity
            {
                Id = "CARD-GAP", CountryCode = "PL",
                ValidFrom = new DateOnly(2026, 1, 1), ValidUntil = new DateOnly(2031, 1, 1)
            }]
        });
        await context.SaveChangesAsync();
        var repository = new ActivityRepository(context);
        var crew = new CrewTachographEngine();
        var service = new CrewTachographService(crew, repository);
        await service.RegisterCardAsync("CARD-GAP");
        service.InsertCard(TachographSlot.Driver, "CARD-GAP");
        var epoch = DateTimeOffset.UtcNow;

        await service.ProcessFrameAsync(new TelemetryFrame(new GameTime(10), epoch, 30, false));
        await service.ProcessFrameAsync(new TelemetryFrame(new GameTime(490), epoch.AddSeconds(1), 30, false));

        context.ChangeTracker.Clear();
        var gap = Assert.Single(await repository.LoadDriverGapsAsync("CARD-GAP"));
        Assert.Equal(new GameTime(11), gap.Start);
        Assert.Equal(new GameTime(490), gap.EndExclusive);
        Assert.Equal(ActivityGapReason.ForwardTimeJump, gap.Reason);
        Assert.Equal(1, gap.Slot);
        Assert.Single(await context.ActivityGaps.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Card_removed_gap_is_opened_and_closed_in_sqlite_by_card_events()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TachographDbContext>().UseSqlite(connection).Options;
        await using var context = new TachographDbContext(options);
        await context.Database.EnsureCreatedAsync();
        AddCard(context, "CARD-REMOVED");
        await context.SaveChangesAsync();
        var repository = new ActivityRepository(context);
        var service = new CrewTachographService(new CrewTachographEngine(), repository);
        await service.RegisterCardAsync("CARD-REMOVED");
        await service.InsertCardAsync(TachographSlot.Driver, "CARD-REMOVED");
        var epoch = DateTimeOffset.UtcNow;
        await service.ProcessFrameAsync(new TelemetryFrame(new GameTime(100), epoch, 0, false));

        await service.EjectCardAsync(TachographSlot.Driver, epoch.AddSeconds(1));

        context.ChangeTracker.Clear();
        var opened = Assert.Single(await context.ActivityGaps.AsNoTracking().ToListAsync());
        Assert.Equal(100, opened.StartGameMinute);
        Assert.Null(opened.EndGameMinuteExclusive);
        Assert.Equal(ActivityGapReason.CardRemoved, opened.Reason);

        await service.ProcessFrameAsync(new TelemetryFrame(
            new GameTime(500), epoch.AddSeconds(2), 0, false));
        await service.InsertCardAsync(TachographSlot.CoDriver, "CARD-REMOVED");

        context.ChangeTracker.Clear();
        var closed = Assert.Single(await repository.LoadDriverGapsAsync("CARD-REMOVED"));
        Assert.Equal(new GameTime(100), closed.Start);
        Assert.Equal(new GameTime(500), closed.EndExclusive);
        Assert.Equal(1, closed.Slot);
        Assert.Equal(ActivityGapState.Unresolved, closed.State);
    }

    [Fact]
    public async Task Same_minute_reinsertion_deletes_zero_length_gap_from_sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TachographDbContext>().UseSqlite(connection).Options;
        await using var context = new TachographDbContext(options);
        await context.Database.EnsureCreatedAsync();
        AddCard(context, "CARD-ZERO-GAP");
        await context.SaveChangesAsync();
        var repository = new ActivityRepository(context);
        var service = new CrewTachographService(new CrewTachographEngine(), repository);
        await service.RegisterCardAsync("CARD-ZERO-GAP");
        await service.InsertCardAsync(TachographSlot.Driver, "CARD-ZERO-GAP");
        var epoch = DateTimeOffset.UtcNow;
        await service.ProcessFrameAsync(new TelemetryFrame(new GameTime(100), epoch, 0, false));

        await service.EjectCardAsync(TachographSlot.Driver, epoch.AddSeconds(1));
        await service.InsertCardAsync(TachographSlot.CoDriver, "CARD-ZERO-GAP");

        context.ChangeTracker.Clear();
        Assert.Empty(await context.ActivityGaps.AsNoTracking().ToListAsync());
        Assert.Empty(await repository.LoadDriverGapsAsync("CARD-ZERO-GAP"));
    }

    [Fact]
    public async Task Resolve_gap_atomically_persists_manual_segments_and_resolution_audit()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TachographDbContext>().UseSqlite(connection).Options;
        await using var context = new TachographDbContext(options);
        await context.Database.EnsureCreatedAsync();
        AddCard(context, "CARD-MANUAL-SQLITE");
        await context.SaveChangesAsync();
        var repository = new ActivityRepository(context);
        var gap = new ActivityGap
        {
            Id = Guid.NewGuid(),
            DriverCardId = "CARD-MANUAL-SQLITE",
            Slot = 1,
            SessionIndex = 0,
            Start = new GameTime(100),
            EndExclusive = new GameTime(160),
            Reason = ActivityGapReason.CardRemoved,
            State = ActivityGapState.Unresolved
        };
        await repository.ApplySessionWritesAsync(
        [
            new ActivitySessionWrite(
                "CARD-MANUAL-SQLITE", 0, new GameTime(100), [], [gap])
        ]);
        var service = new ManualEntryService(repository);

        var resolved = await service.ResolveGapAsync(
            gap.Id,
            [
                new ManualEntrySegment(100, 120, DriverActivity.BreakOrRest),
                new ManualEntrySegment(120, 140, DriverActivity.OtherWork),
                new ManualEntrySegment(140, 160, DriverActivity.Availability)
            ],
            new GameTime(500));

        context.ChangeTracker.Clear();
        var storedGap = Assert.Single(await context.ActivityGaps.AsNoTracking().ToListAsync());
        Assert.Equal(ActivityGapState.Resolved, storedGap.State);
        Assert.Equal(500, storedGap.ResolvedAtGameMinute);
        var storedRecords = await context.ActivityRecords.AsNoTracking()
            .OrderBy(record => record.StartGameMinute)
            .ToListAsync();
        Assert.Equal(3, storedRecords.Count);
        Assert.All(storedRecords, record =>
        {
            Assert.Equal(ActivitySource.ManualEntry, record.Source);
            Assert.Equal(gap.Id, record.SourceGapId);
        });
        Assert.Equal(60, storedRecords.Sum(record =>
            record.EndGameMinuteExclusive - record.StartGameMinute));
        Assert.Equal(ActivityGapState.Resolved,
            Assert.Single(await repository.LoadDriverGapsAsync("CARD-MANUAL-SQLITE")).State);
        Assert.Equal(ResolveGapStatus.Resolved, resolved.Status);
    }

    [Fact]
    public async Task Sqlite_resolution_reloads_canonical_history_and_resets_day_at_gap_rest_end()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TachographDbContext>().UseSqlite(connection).Options;
        await using var context = new TachographDbContext(options);
        await context.Database.EnsureCreatedAsync();
        const string cardId = "CARD-MANUAL-RESET-SQLITE";
        AddCard(context, cardId);
        await context.SaveChangesAsync();
        var repository = new ActivityRepository(context);
        var gap = new ActivityGap
        {
            Id = Guid.NewGuid(),
            DriverCardId = cardId,
            Slot = 1,
            SessionIndex = 0,
            Start = new GameTime(60),
            EndExclusive = new GameTime(600),
            Reason = ActivityGapReason.CardRemoved,
            State = ActivityGapState.Unresolved
        };
        ActivityRecord[] measured =
        [
            new()
            {
                Id = Guid.NewGuid(), DriverCardId = cardId,
                Activity = DriverActivity.Driving,
                Start = new GameTime(0), EndExclusive = new GameTime(60),
                RecordedAtUtc = DateTimeOffset.UtcNow, Source = ActivitySource.Telemetry
            },
            new()
            {
                Id = Guid.NewGuid(), DriverCardId = cardId,
                Activity = DriverActivity.Driving,
                Start = new GameTime(600), EndExclusive = new GameTime(660),
                RecordedAtUtc = DateTimeOffset.UtcNow, Source = ActivitySource.Telemetry
            }
        ];
        await repository.ApplySessionWritesAsync(
        [
            new ActivitySessionWrite(cardId, 0, new GameTime(0), measured, [gap])
        ]);

        var result = await new ManualEntryService(repository).ResolveGapAsync(
            gap.Id,
            [new ManualEntrySegment(60, 600, DriverActivity.BreakOrRest)],
            new GameTime(660));

        Assert.Equal(new GameTime(600), result.Evaluation.State.LastDailyRestResetAt);
        Assert.Equal(60, result.Evaluation.State.DailyDrivingMinutes);
        Assert.Equal(60, result.Evaluation.State.DailyWorkMinutes);
        Assert.Contains(result.Evaluation.QualifiedRests, rest =>
            rest.SourceGapId == gap.Id && rest.DurationMinutes == 540);
    }

    [Fact]
    public async Task Repeating_identical_sqlite_resolution_is_idempotent()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TachographDbContext>().UseSqlite(connection).Options;
        await using var context = new TachographDbContext(options);
        await context.Database.EnsureCreatedAsync();
        AddCard(context, "CARD-MANUAL-IDEMPOTENT");
        await context.SaveChangesAsync();
        var repository = new ActivityRepository(context);
        var gap = new ActivityGap
        {
            Id = Guid.NewGuid(),
            DriverCardId = "CARD-MANUAL-IDEMPOTENT",
            Slot = 1,
            SessionIndex = 0,
            Start = new GameTime(10),
            EndExclusive = new GameTime(20),
            Reason = ActivityGapReason.ForwardTimeJump,
            State = ActivityGapState.Unresolved
        };
        await repository.ApplySessionWritesAsync(
        [
            new ActivitySessionWrite(
                "CARD-MANUAL-IDEMPOTENT", 0, new GameTime(10), [], [gap])
        ]);
        var service = new ManualEntryService(repository);
        ManualEntrySegment[] segments =
        [
            new(10, 20, DriverActivity.BreakOrRest)
        ];

        await service.ResolveGapAsync(gap.Id, segments, new GameTime(30));
        context.ChangeTracker.Clear();
        var replay = await service.ResolveGapAsync(gap.Id, segments, new GameTime(31));

        Assert.Equal(ResolveGapStatus.AlreadyResolved, replay.Status);
        Assert.Single(await context.ActivityRecords.AsNoTracking().ToListAsync());
        Assert.Single(await context.ActivityGaps.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Removed_card_rollback_keeps_old_raw_gap_and_one_new_canonical_gap()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TachographDbContext>().UseSqlite(connection).Options;
        await using var context = new TachographDbContext(options);
        await context.Database.EnsureCreatedAsync();
        AddCard(context, "CARD-REMOVED-BRANCH");
        await context.SaveChangesAsync();
        var repository = new ActivityRepository(context);
        var service = new CrewTachographService(new CrewTachographEngine(), repository);
        await service.RegisterCardAsync("CARD-REMOVED-BRANCH");
        await service.InsertCardAsync(TachographSlot.Driver, "CARD-REMOVED-BRANCH");
        var epoch = DateTimeOffset.UtcNow;
        await service.ProcessFrameAsync(new TelemetryFrame(new GameTime(100), epoch, 0, false));
        await service.EjectCardAsync(TachographSlot.Driver, epoch.AddSeconds(1));

        await service.ProcessFrameAsync(new TelemetryFrame(
            new GameTime(90), epoch.AddSeconds(2), 0, false));

        context.ChangeTracker.Clear();
        var canonical = Assert.Single(await repository.LoadDriverGapsAsync("CARD-REMOVED-BRANCH"));
        Assert.Equal(new GameTime(90), canonical.Start);
        Assert.Null(canonical.EndExclusive);
        var rawGaps = (await repository.LoadRawSessionsAsync("CARD-REMOVED-BRANCH"))
            .SelectMany(session => session.Gaps ?? [])
            .OrderBy(gap => gap.SessionIndex)
            .ToList();
        Assert.Equal(2, rawGaps.Count);
        Assert.Equal(new GameTime(100), rawGaps[0].Start);
        Assert.Null(rawGaps[0].EndExclusive);
        Assert.Equal(new GameTime(90), rawGaps[1].Start);
        Assert.Null(rawGaps[1].EndExclusive);
    }

    [Fact]
    public async Task New_branch_truncates_open_gap_in_canonical_projection_without_mutating_source_branch()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TachographDbContext>().UseSqlite(connection).Options;
        await using var context = new TachographDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.DriverProfiles.Add(new DriverProfileEntity
        {
            Id = Guid.NewGuid(),
            DisplayName = "Przycięcie luki",
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Cards = [new DriverCardEntity
            {
                Id = "CARD-GAP-BRANCH", CountryCode = "PL",
                ValidFrom = new DateOnly(2026, 1, 1), ValidUntil = new DateOnly(2031, 1, 1)
            }]
        });
        await context.SaveChangesAsync();
        var repository = new ActivityRepository(context);
        var openGap = new ActivityGap
        {
            Id = Guid.NewGuid(),
            DriverCardId = "CARD-GAP-BRANCH",
            Slot = 1,
            SessionIndex = 0,
            Start = new GameTime(11),
            EndExclusive = null,
            Reason = ActivityGapReason.CardRemoved,
            State = ActivityGapState.Unresolved
        };

        await repository.ApplySessionWritesAsync(
        [
            new ActivitySessionWrite(
                "CARD-GAP-BRANCH", 0, new GameTime(10),
                [Record("CARD-GAP-BRANCH", 10, DriverActivity.Driving)],
                [openGap]),
            new ActivitySessionWrite(
                "CARD-GAP-BRANCH", 1, new GameTime(50),
                [Record("CARD-GAP-BRANCH", 50, DriverActivity.OtherWork)])
        ]);

        context.ChangeTracker.Clear();
        var canonical = Assert.Single(await repository.LoadDriverGapsAsync("CARD-GAP-BRANCH"));
        Assert.Equal(new GameTime(11), canonical.Start);
        Assert.Equal(new GameTime(50), canonical.EndExclusive);
        Assert.Null(Assert.Single(await context.ActivityGaps.AsNoTracking().ToListAsync())
            .EndGameMinuteExclusive);
    }

    [Fact]
    public async Task Unresolved_gap_query_excludes_gap_from_abandoned_branch()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TachographDbContext>().UseSqlite(connection).Options;
        await using var context = new TachographDbContext(options);
        await context.Database.EnsureCreatedAsync();
        const string cardId = "CARD-UNRESOLVED-BRANCH";
        AddCard(context, cardId);
        await context.SaveChangesAsync();
        var repository = new ActivityRepository(context);
        var abandonedGap = new ActivityGap
        {
            Id = Guid.NewGuid(),
            DriverCardId = cardId,
            Slot = 1,
            SessionIndex = 0,
            Start = new GameTime(100),
            EndExclusive = new GameTime(200),
            Reason = ActivityGapReason.CardRemoved,
            State = ActivityGapState.Unresolved
        };

        await repository.ApplySessionWritesAsync(
        [
            new ActivitySessionWrite(cardId, 0, new GameTime(0), [], [abandonedGap]),
            new ActivitySessionWrite(cardId, 1, new GameTime(90), [])
        ]);

        Assert.Empty(await repository.GetUnresolvedGapsAsync(cardId));
        Assert.Empty(await repository.GetCanonicalGapsAsync(
            cardId,
            null,
            null,
            includeResolved: true));
        Assert.Single((await repository.LoadRawSessionsAsync(cardId))
            .SelectMany(session => session.Gaps ?? []));
    }

    [Fact]
    public async Task Unresolved_gap_query_filters_by_card_and_overlapping_range()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TachographDbContext>().UseSqlite(connection).Options;
        await using var context = new TachographDbContext(options);
        await context.Database.EnsureCreatedAsync();
        const string cardA = "CARD-GAP-FILTER-A";
        const string cardB = "CARD-GAP-FILTER-B";
        AddCard(context, cardA);
        AddCard(context, cardB);
        await context.SaveChangesAsync();
        var repository = new ActivityRepository(context);
        var gapA = Gap(cardA, 1, 0, 100, 200);
        var laterA = Gap(cardA, 1, 0, 300, null);
        var resolvedA = Gap(cardA, 1, 0, 180, 190) with
        {
            State = ActivityGapState.Resolved,
            ResolvedAt = new GameTime(220)
        };
        var gapB = Gap(cardB, 2, 0, 150, 250);

        await repository.ApplySessionWritesAsync(
        [
            new ActivitySessionWrite(cardA, 0, new GameTime(0), [], [gapA, laterA, resolvedA]),
            new ActivitySessionWrite(cardB, 0, new GameTime(0), [], [gapB])
        ]);

        var allCards = await repository.GetUnresolvedGapsAsync(
            fromGameMinute: new GameTime(180),
            toGameMinute: new GameTime(260));
        var oneCard = await repository.GetUnresolvedGapsAsync(
            cardA,
            new GameTime(180),
            new GameTime(260));
        var canonicalWithoutResolved = await repository.GetCanonicalGapsAsync(
            null,
            new GameTime(180),
            new GameTime(260),
            includeResolved: false);
        var canonicalWithResolved = await repository.GetCanonicalGapsAsync(
            null,
            new GameTime(180),
            new GameTime(260),
            includeResolved: true);

        Assert.Equal([gapA.Id, gapB.Id], allCards.Select(gap => gap.Id).ToArray());
        Assert.Equal(
            allCards.Select(gap => gap.Id),
            canonicalWithoutResolved.Select(gap => gap.Id));
        Assert.Equal(
            [gapA.Id, gapB.Id, resolvedA.Id],
            canonicalWithResolved.Select(gap => gap.Id).ToArray());
        Assert.Equal(gapA.Id, Assert.Single(oneCard).Id);
        Assert.DoesNotContain(allCards, gap => gap.Id == resolvedA.Id || gap.Id == laterA.Id);
    }

    [Fact]
    public async Task Projected_gap_resolution_materializes_current_branch_without_mutating_source_gap()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TachographDbContext>().UseSqlite(connection).Options;
        await using var context = new TachographDbContext(options);
        await context.Database.EnsureCreatedAsync();
        const string cardId = "CARD-PROJECTED-MANUAL";
        AddCard(context, cardId);
        await context.SaveChangesAsync();
        var repository = new ActivityRepository(context);
        var sourceGap = new ActivityGap
        {
            Id = Guid.NewGuid(),
            DriverCardId = cardId,
            Slot = 1,
            SessionIndex = 0,
            Start = new GameTime(11),
            EndExclusive = null,
            Reason = ActivityGapReason.CardRemoved,
            State = ActivityGapState.Unresolved
        };
        await repository.ApplySessionWritesAsync(
        [
            new ActivitySessionWrite(
                cardId, 0, new GameTime(10),
                [Record(cardId, 10, DriverActivity.Driving)], [sourceGap]),
            new ActivitySessionWrite(
                cardId, 1, new GameTime(50),
                [Record(cardId, 50, DriverActivity.OtherWork)])
        ]);

        var service = new ManualEntryService(repository);
        var resolved = await service.ResolveGapAsync(
            sourceGap.Id,
            [new ManualEntrySegment(11, 50, DriverActivity.BreakOrRest)],
            new GameTime(51));

        Assert.Equal(ResolveGapStatus.Resolved, resolved.Status);
        Assert.NotEqual(sourceGap.Id, resolved.Gap.Id);
        Assert.Equal(sourceGap.Id, resolved.Gap.ProjectionSourceGapId);
        Assert.Equal(1, resolved.Gap.SessionIndex);
        Assert.Equal(new GameTime(11), resolved.Gap.Start);
        Assert.Equal(new GameTime(50), resolved.Gap.EndExclusive);
        Assert.Equal(ActivityGapState.Resolved, resolved.Gap.State);
        Assert.All(resolved.Segments, record => Assert.Equal(resolved.Gap.Id, record.SourceGapId));

        context.ChangeTracker.Clear();
        var raw = (await repository.LoadRawSessionsAsync(cardId))
            .SelectMany(session => session.Gaps ?? [])
            .OrderBy(gap => gap.SessionIndex)
            .ToList();
        Assert.Equal(2, raw.Count);
        Assert.Equal(sourceGap.Id, raw[0].Id);
        Assert.Null(raw[0].EndExclusive);
        Assert.Equal(ActivityGapState.Unresolved, raw[0].State);
        Assert.Equal(resolved.Gap.Id, raw[1].Id);
        Assert.Equal(sourceGap.Id, raw[1].ProjectionSourceGapId);
        Assert.Equal(ActivityGapState.Resolved, raw[1].State);

        var canonical = Assert.Single(await repository.LoadDriverGapsAsync(cardId));
        Assert.Equal(resolved.Gap.Id, canonical.Id);
        Assert.Equal(ActivityGapState.Resolved, canonical.State);
        Assert.Empty(await repository.GetUnresolvedGapsAsync(cardId));
        var auditGap = Assert.Single(await repository.GetCanonicalGapsAsync(
            cardId,
            null,
            null,
            includeResolved: true));
        Assert.Equal(resolved.Gap.Id, auditGap.Id);
        Assert.Equal(ActivityGapState.Resolved, auditGap.State);
        Assert.Equal(new GameTime(51), auditGap.ResolvedAt);
        var replay = await service.ResolveGapAsync(
            sourceGap.Id,
            [new ManualEntrySegment(11, 50, DriverActivity.BreakOrRest)],
            new GameTime(52));
        Assert.Equal(ResolveGapStatus.AlreadyResolved, replay.Status);
        Assert.Single(await context.ActivityRecords.AsNoTracking()
            .Where(record => record.SourceGapId == resolved.Gap.Id)
            .ToListAsync());
    }

    private static ActivityRecord Record(string card, long minute, DriverActivity activity) => new()
    {
        Id = Guid.NewGuid(),
        DriverCardId = card,
        Activity = activity,
        Start = new GameTime(minute),
        EndExclusive = new GameTime(minute + 1),
        RecordedAtUtc = DateTimeOffset.UtcNow,
        Source = ActivitySource.Telemetry
    };

    private static ActivityGap Gap(
        string cardId,
        int slot,
        int sessionIndex,
        long start,
        long? end) => new()
    {
        Id = Guid.NewGuid(),
        DriverCardId = cardId,
        Slot = slot,
        SessionIndex = sessionIndex,
        Start = new GameTime(start),
        EndExclusive = end is null ? null : new GameTime(end.Value),
        Reason = ActivityGapReason.ForwardTimeJump,
        State = ActivityGapState.Unresolved
    };

    private static void AddCard(TachographDbContext context, string cardId) =>
        context.DriverProfiles.Add(new DriverProfileEntity
        {
            Id = Guid.NewGuid(),
            DisplayName = cardId,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Cards = [new DriverCardEntity
            {
                Id = cardId,
                CountryCode = "PL",
                ValidFrom = new DateOnly(2026, 1, 1),
                ValidUntil = new DateOnly(2031, 1, 1)
            }]
        });

    private sealed class RecordingPersistenceDiagnostics : IActivityPersistenceDiagnostics
    {
        public List<Conflict> Conflicts { get; } = [];

        public void RecordConflict(
            string driverCardId,
            int sessionIndex,
            ActivityRecord existing,
            ActivityRecord incoming) =>
            Conflicts.Add(new Conflict(driverCardId, sessionIndex, existing, incoming));

        public sealed record Conflict(
            string CardId,
            int SessionIndex,
            ActivityRecord Existing,
            ActivityRecord Incoming);
    }
}

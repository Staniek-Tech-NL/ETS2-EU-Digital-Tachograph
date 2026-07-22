using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ETS2Tachograph.Infrastructure.Tests;

public sealed class WeeklyRestCompensationSqliteRestartTests
{
    private const string DriverCardId = "PL-SQLITE-RESTART";

    [Fact]
    public async Task Sqlite_restart_recreates_identical_open_compensation_contract()
    {
        IReadOnlyList<ActivityRecord> history =
        [
            Record(0, 2_400, DriverActivity.BreakOrRest),
            Record(2_400, 2_401, DriverActivity.OtherWork)
        ];

        var (beforeRestart, afterRestart) = await PersistCloseReopenAndEvaluateAsync(
            history,
            new GameTime(2_401));

        Assert.Equal(300, beforeRestart.OriginalOwedMinutes);
        Assert.Equal(300, beforeRestart.RemainingMinutes);
        Assert.Equal(WeeklyRestCompensationStatusDto.OpenOnTime, beforeRestart.Status);
        Assert.Null(beforeRestart.PaymentRestBlockId);
        Assert.Null(beforeRestart.PaymentRange);
        Assert.Null(beforeRestart.SettledAtGameMinute);
        AssertCompleteContractEqual(beforeRestart, afterRestart);
    }

    [Fact]
    public async Task Sqlite_restart_recreates_identical_paid_compensation_contract()
    {
        IReadOnlyList<ActivityRecord> history =
        [
            Record(0, 2_400, DriverActivity.BreakOrRest),
            Record(2_400, 2_401, DriverActivity.OtherWork),
            Record(2_401, 3_241, DriverActivity.BreakOrRest),
            Record(3_241, 3_242, DriverActivity.OtherWork)
        ];

        var (beforeRestart, afterRestart) = await PersistCloseReopenAndEvaluateAsync(
            history,
            new GameTime(3_242));

        Assert.Equal(300, beforeRestart.OriginalOwedMinutes);
        Assert.Equal(0, beforeRestart.RemainingMinutes);
        Assert.Equal(WeeklyRestCompensationStatusDto.PaidOnTime, beforeRestart.Status);
        Assert.NotNull(beforeRestart.PaymentRestBlockId);
        Assert.Equal(2_941, beforeRestart.PaymentRange?.StartGameMinute);
        Assert.Equal(3_241, beforeRestart.PaymentRange?.EndGameMinuteExclusive);
        Assert.Equal(3_241, beforeRestart.SettledAtGameMinute);
        AssertCompleteContractEqual(beforeRestart, afterRestart);
    }

    private static async Task<(WeeklyRestCompensationDto Before, WeeklyRestCompensationDto After)>
        PersistCloseReopenAndEvaluateAsync(
            IReadOnlyList<ActivityRecord> history,
            GameTime now)
    {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"ets2-compensation-restart-{Guid.NewGuid():N}.db");
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Pooling = false
        }.ToString();

        try
        {
            WeeklyRestCompensationDto before;
            await using (var context = CreateContext(connectionString))
            {
                await context.Database.EnsureCreatedAsync();
                context.DriverProfiles.Add(new DriverProfileEntity
                {
                    Id = Guid.NewGuid(),
                    DisplayName = "SQLite restart test",
                    IsActive = true,
                    CreatedAtUtc = DateTimeOffset.UnixEpoch,
                    Cards =
                    [
                        new DriverCardEntity
                        {
                            Id = DriverCardId,
                            CountryCode = "PL",
                            ValidFrom = new DateOnly(2026, 1, 1),
                            ValidUntil = new DateOnly(2031, 1, 1)
                        }
                    ]
                });
                await context.SaveChangesAsync();
                var repository = new ActivityRepository(context);
                await repository.ApplySessionWritesAsync(
                [
                    new ActivitySessionWrite(
                        DriverCardId,
                        0,
                        new GameTime(0),
                        history)
                ]);
                before = await LoadAndEvaluateSingleAsync(repository, now);
            }

            WeeklyRestCompensationDto after;
            await using (var context = CreateContext(connectionString))
            {
                var repository = new ActivityRepository(context);
                after = await LoadAndEvaluateSingleAsync(repository, now);
            }

            return (before, after);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            DeleteIfExists(databasePath);
            DeleteIfExists(databasePath + "-shm");
            DeleteIfExists(databasePath + "-wal");
        }
    }

    private static TachographDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<TachographDbContext>()
            .UseSqlite(connectionString)
            .Options;
        return new TachographDbContext(options);
    }

    private static async Task<WeeklyRestCompensationDto> LoadAndEvaluateSingleAsync(
        IActivityRepository repository,
        GameTime now)
    {
        var restoredHistory = await repository.LoadDriverHistoryAsync(DriverCardId);
        var analysis = new RegulationReportAnalyzer().Analyze(now, restoredHistory);
        return Assert.Single(analysis.CompensationObligations);
    }

    private static void AssertCompleteContractEqual(
        WeeklyRestCompensationDto expected,
        WeeklyRestCompensationDto actual)
    {
        Assert.Equal(expected.IdentitySchemeVersion, actual.IdentitySchemeVersion);
        Assert.Equal(expected.DriverCardId, actual.DriverCardId);
        Assert.Equal(expected.ObligationId, actual.ObligationId);
        Assert.Equal(expected.SourceRestBlockId, actual.SourceRestBlockId);
        Assert.Equal(expected.PaymentRestBlockId, actual.PaymentRestBlockId);
        Assert.Equal(expected.OriginalOwedMinutes, actual.OriginalOwedMinutes);
        Assert.Equal(expected.RemainingMinutes, actual.RemainingMinutes);
        Assert.Equal(expected.DueAtGameMinuteExclusive, actual.DueAtGameMinuteExclusive);
        Assert.Equal(expected.Status, actual.Status);
        Assert.Equal(expected.SettledAtGameMinute, actual.SettledAtGameMinute);
        Assert.Equal(
            expected.PaymentRange?.StartGameMinute,
            actual.PaymentRange?.StartGameMinute);
        Assert.Equal(
            expected.PaymentRange?.EndGameMinuteExclusive,
            actual.PaymentRange?.EndGameMinuteExclusive);
    }

    private static ActivityRecord Record(
        long start,
        long end,
        DriverActivity activity) => new()
    {
        Id = Guid.NewGuid(),
        DriverCardId = DriverCardId,
        Activity = activity,
        Start = new GameTime(start),
        EndExclusive = new GameTime(end),
        RecordedAtUtc = DateTimeOffset.UnixEpoch.AddMinutes(start),
        Source = ActivitySource.Telemetry
    };

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}

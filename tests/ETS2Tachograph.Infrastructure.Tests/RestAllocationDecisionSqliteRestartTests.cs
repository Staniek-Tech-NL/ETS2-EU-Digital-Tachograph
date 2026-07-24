using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Application.Services;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.Infrastructure.Persistence;
using ETS2Tachograph.RuleEngine;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ETS2Tachograph.Infrastructure.Tests;

public sealed class RestAllocationDecisionSqliteRestartTests
{
    private const string CardId = "PL-ALLOCATION-RESTART";

    [Fact]
    public async Task Sqlite_restart_preserves_decision_candidate_and_payment_trace()
    {
        var databasePath = TempDatabasePath();
        var connectionString = ConnectionString(databasePath);
        try
        {
            RestAllocationDecision beforeDecision;
            RegulationEvaluation beforeEvaluation;
            await using (var context = CreateContext(connectionString))
            {
                await SeedAsync(context);
                var activityRepository = new ActivityRepository(context);
                await SaveHistoryAsync(activityRepository);
                var decisionRepository = new RestAllocationRepository(context);
                var service = new RestAllocationService(
                    activityRepository,
                    decisionRepository);
                var pending = await service.EvaluateAsync(CardId, new GameTime(3_301));
                var allocation = Assert.Single(pending.PendingRestAllocations);
                var candidate = Assert.Single(allocation.Candidates, item =>
                    item.Purpose == RestAllocationPurpose.DailyRestWithCompensation);
                beforeEvaluation = await service.DecideAsync(
                    CardId,
                    allocation.RestBlockId,
                    candidate.CandidateId,
                    new GameTime(3_301),
                    DateTimeOffset.UnixEpoch.AddMinutes(3_301));
                beforeDecision = Assert.Single(
                    await decisionRepository.LoadDriverDecisionsAsync(CardId));
            }

            await using (var context = CreateContext(connectionString))
            {
                var activityRepository = new ActivityRepository(context);
                var decisionRepository = new RestAllocationRepository(context);
                var afterDecision = Assert.Single(
                    await decisionRepository.LoadDriverDecisionsAsync(CardId));
                var afterEvaluation = await new RestAllocationService(
                    activityRepository,
                    decisionRepository).EvaluateAsync(
                    CardId,
                    new GameTime(3_301));

                Assert.Equal(beforeDecision, afterDecision);
                AssertCandidateEqual(
                    beforeEvaluation.RestAllocations.Single(item =>
                        item.RestBlockId == beforeDecision.RestBlockId).SelectedCandidate!,
                    afterEvaluation.RestAllocations.Single(item =>
                        item.RestBlockId == afterDecision.RestBlockId).SelectedCandidate!);
                Assert.Equal(
                    beforeEvaluation.CompensationObligations.Single(),
                    afterEvaluation.CompensationObligations.Single());
                Assert.Equal(
                    beforeEvaluation.CompensationObligations.Single().PaymentRange,
                    afterEvaluation.CompensationObligations.Single().PaymentRange);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            DeleteDatabase(databasePath);
        }
    }

    [Fact]
    public async Task Crew_registration_reapplies_persisted_payment_decision_before_showing_compensation()
    {
        var databasePath = TempDatabasePath();
        var connectionString = ConnectionString(databasePath);
        try
        {
            await using (var context = CreateContext(connectionString))
            {
                await SeedAsync(context);
                var activityRepository = new ActivityRepository(context);
                await SaveHistoryAsync(activityRepository);
                var decisionRepository = new RestAllocationRepository(context);
                var allocationService = new RestAllocationService(
                    activityRepository,
                    decisionRepository);
                var pending = await allocationService.EvaluateAsync(
                    CardId,
                    new GameTime(3_301));
                var allocation = Assert.Single(pending.PendingRestAllocations);
                var repayment = Assert.Single(allocation.Candidates, item =>
                    item.Purpose == RestAllocationPurpose.DailyRestWithCompensation);
                await allocationService.DecideAsync(
                    CardId,
                    allocation.RestBlockId,
                    repayment.CandidateId,
                    new GameTime(3_301),
                    DateTimeOffset.UnixEpoch.AddMinutes(3_301));
                await activityRepository.ObserveGameTimeAsync(
                    CardId,
                    new GameTime(
                        1_507 + ActivityRetentionPolicy.HotWindowMinutes));
                await activityRepository.ArchiveWarmAsync(CardId);
            }

            await using (var context = CreateContext(connectionString))
            {
                var activityRepository = new ActivityRepository(context);
                var decisionRepository = new RestAllocationRepository(context);
                var crew = new CrewTachographService(
                    new ETS2Tachograph.Engine.CrewTachographEngine(),
                    activityRepository,
                    restAllocations: decisionRepository);

                await crew.RegisterCardAsync(CardId);

                var regulation = crew.Engine.GetEngine(CardId)!.Current.Regulation!;
                var obligation = Assert.Single(regulation.CompensationObligations);
                Assert.Equal(0, obligation.RemainingMinutes);
                Assert.Equal(
                    WeeklyRestCompensationStatus.PaidOnTime,
                    obligation.Status);
                Assert.Empty(regulation.Compensations);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            DeleteDatabase(databasePath);
        }
    }

    [Fact]
    public async Task Saving_new_choice_supersedes_previous_active_decision()
    {
        var databasePath = TempDatabasePath();
        var connectionString = ConnectionString(databasePath);
        try
        {
            await using var context = CreateContext(connectionString);
            await SeedAsync(context);
            var repository = new RestAllocationRepository(context);
            var first = Decision(Guid.NewGuid(), "rest-1", "candidate-daily");
            var second = Decision(Guid.NewGuid(), "rest-1", "candidate-weekly");

            await repository.SaveDecisionAsync(first);
            var storedSecond = await repository.SaveDecisionAsync(second);
            var all = await repository.LoadDriverDecisionsAsync(CardId);

            Assert.Equal(2, all.Count);
            Assert.Equal(
                RestAllocationDecisionStatus.Superseded,
                all.Single(item => item.DecisionId == first.DecisionId).Status);
            var active = all.Single(item =>
                item.Status == RestAllocationDecisionStatus.Active);
            Assert.Equal(second.DecisionId, active.DecisionId);
            Assert.Equal(first.DecisionId, storedSecond.SupersedesDecisionId);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            DeleteDatabase(databasePath);
        }
    }

    [Fact]
    public async Task Missing_canonical_rest_block_invalidates_active_decision()
    {
        var databasePath = TempDatabasePath();
        var connectionString = ConnectionString(databasePath);
        try
        {
            await using var context = CreateContext(connectionString);
            await SeedAsync(context);
            var repository = new RestAllocationRepository(context);
            var decision = Decision(Guid.NewGuid(), "old-rest", "candidate");
            await repository.SaveDecisionAsync(decision);

            await repository.InvalidateMissingRestBlocksAsync(
                CardId,
                new HashSet<string>(StringComparer.Ordinal) { "new-rest" });

            var stored = Assert.Single(
                await repository.LoadDriverDecisionsAsync(CardId));
            Assert.Equal(RestAllocationDecisionStatus.Invalidated, stored.Status);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            DeleteDatabase(databasePath);
        }
    }

    private static async Task SeedAsync(TachographDbContext context)
    {
        await context.Database.EnsureCreatedAsync();
        context.DriverProfiles.Add(new DriverProfileEntity
        {
            Id = Guid.NewGuid(),
            DisplayName = "Allocation restart",
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UnixEpoch,
            Cards =
            [
                new DriverCardEntity
                {
                    Id = CardId,
                    CountryCode = "PL",
                    ValidFrom = new DateOnly(2026, 1, 1),
                    ValidUntil = new DateOnly(2031, 1, 1)
                }
            ]
        });
        await context.SaveChangesAsync();
    }

    private static Task SaveHistoryAsync(IActivityRepository repository) =>
        repository.ApplySessionWritesAsync(
        [
            new ActivitySessionWrite(
                CardId,
                0,
                new GameTime(0),
                [
                    Record(0, 1_447, DriverActivity.BreakOrRest),
                    Record(1_447, 1_507, DriverActivity.OtherWork),
                    Record(1_507, 3_300, DriverActivity.BreakOrRest),
                    Record(3_300, 3_301, DriverActivity.OtherWork)
                ])
        ]);

    private static RestAllocationDecision Decision(
        Guid id,
        string restBlockId,
        string candidateId) => new(
        id,
        CardId,
        restBlockId,
        candidateId,
        EffectiveAtGameMinute: 3_300,
        DateTimeOffset.UnixEpoch.AddMinutes(3_300),
        DecisionSchemeVersion: 1);

    private static void AssertCandidateEqual(
        RestAllocationCandidate expected,
        RestAllocationCandidate actual)
    {
        Assert.Equal(expected.CandidateId, actual.CandidateId);
        Assert.Equal(expected.RestBlockId, actual.RestBlockId);
        Assert.Equal(expected.Purpose, actual.Purpose);
        Assert.Equal(expected.HostMinimumMinutes, actual.HostMinimumMinutes);
        Assert.Equal(expected.ObligationIds, actual.ObligationIds);
        Assert.Equal(expected.NewDebtMinutes, actual.NewDebtMinutes);
        Assert.Equal(
            expected.SatisfiesWeeklyRestRequirement,
            actual.SatisfiesWeeklyRestRequirement);
    }

    private static ActivityRecord Record(
        long start,
        long end,
        DriverActivity activity) => new()
    {
        Id = Guid.NewGuid(),
        DriverCardId = CardId,
        Activity = activity,
        Start = new GameTime(start),
        EndExclusive = new GameTime(end),
        RecordedAtUtc = DateTimeOffset.UnixEpoch.AddMinutes(start)
    };

    private static TachographDbContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder<TachographDbContext>()
            .UseSqlite(connectionString)
            .Options);

    private static string TempDatabasePath() => Path.Combine(
        Path.GetTempPath(),
        $"ets2-rest-allocation-{Guid.NewGuid():N}.db");

    private static string ConnectionString(string databasePath) =>
        new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Pooling = false
        }.ToString();

    private static void DeleteDatabase(string databasePath)
    {
        foreach (var path in new[]
                 {
                     databasePath,
                     databasePath + "-shm",
                     databasePath + "-wal"
                 })
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}

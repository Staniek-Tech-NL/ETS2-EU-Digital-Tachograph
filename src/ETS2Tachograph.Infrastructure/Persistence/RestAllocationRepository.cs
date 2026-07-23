using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.RuleEngine;
using Microsoft.EntityFrameworkCore;

namespace ETS2Tachograph.Infrastructure.Persistence;

public sealed class RestAllocationRepository(TachographDbContext context)
    : IRestAllocationRepository
{
    public async Task<IReadOnlyList<RestAllocationDecision>> LoadDriverDecisionsAsync(
        string driverCardId,
        CancellationToken cancellationToken = default) =>
        (await context.RestAllocationDecisions
            .AsNoTracking()
            .Where(item => item.DriverCardId == driverCardId)
            .ToListAsync(cancellationToken))
        .OrderBy(item => item.DecidedAtUtc)
        .ThenBy(item => item.DecisionId)
        .Select(Map)
        .ToList();

    public async Task<RestAllocationDecision> SaveDecisionAsync(
        RestAllocationDecision decision,
        CancellationToken cancellationToken = default)
    {
        if (decision.Status != RestAllocationDecisionStatus.Active)
            throw new InvalidOperationException("Nowa decyzja musi mieć status Active.");
        if (await context.RestAllocationDecisions.AnyAsync(
                item => item.DecisionId == decision.DecisionId,
                cancellationToken))
        {
            return decision;
        }

        await using var transaction = await context.Database.BeginTransactionAsync(
            cancellationToken);
        var active = await context.RestAllocationDecisions
            .Where(item =>
                item.DriverCardId == decision.DriverCardId &&
                item.RestBlockId == decision.RestBlockId &&
                item.Status == (int)RestAllocationDecisionStatus.Active)
            .ToListAsync(cancellationToken);
        active = active
            .OrderByDescending(item => item.DecidedAtUtc)
            .ThenByDescending(item => item.DecisionId)
            .ToList();
        foreach (var previous in active)
            previous.Status = (int)RestAllocationDecisionStatus.Superseded;

        var supersedes = decision.SupersedesDecisionId ??
            active.FirstOrDefault()?.DecisionId;
        var stored = decision with { SupersedesDecisionId = supersedes };
        context.RestAllocationDecisions.Add(Map(stored));
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return stored;
    }

    public async Task InvalidateMissingRestBlocksAsync(
        string driverCardId,
        IReadOnlySet<string> validRestBlockIds,
        CancellationToken cancellationToken = default)
    {
        var active = await context.RestAllocationDecisions
            .Where(item =>
                item.DriverCardId == driverCardId &&
                item.Status == (int)RestAllocationDecisionStatus.Active)
            .ToListAsync(cancellationToken);
        var changed = false;
        foreach (var decision in active.Where(item =>
                     !validRestBlockIds.Contains(item.RestBlockId)))
        {
            decision.Status = (int)RestAllocationDecisionStatus.Invalidated;
            changed = true;
        }

        if (changed)
            await context.SaveChangesAsync(cancellationToken);
    }

    private static RestAllocationDecision Map(RestAllocationDecisionEntity item) => new(
        item.DecisionId,
        item.DriverCardId,
        item.RestBlockId,
        item.CandidateId,
        item.EffectiveAtGameMinute,
        item.DecidedAtUtc,
        item.DecisionSchemeVersion,
        (RestAllocationDecisionStatus)item.Status,
        item.SupersedesDecisionId);

    private static RestAllocationDecisionEntity Map(RestAllocationDecision item) => new()
    {
        DecisionId = item.DecisionId,
        DriverCardId = item.DriverCardId,
        RestBlockId = item.RestBlockId,
        CandidateId = item.CandidateId,
        EffectiveAtGameMinute = item.EffectiveAtGameMinute,
        DecidedAtUtc = item.DecidedAtUtc,
        DecisionSchemeVersion = item.DecisionSchemeVersion,
        Status = (int)item.Status,
        SupersedesDecisionId = item.SupersedesDecisionId
    };
}

using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Core.Rules;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.RuleEngine;

namespace ETS2Tachograph.Application.Services;

public sealed class RestAllocationService(
    IActivityRepository activities,
    IRestAllocationRepository decisions,
    RegulationEngine? engine = null,
    RegulationOptions? options = null)
{
    private readonly RegulationEngine _engine = engine ?? new RegulationEngine();
    private readonly RegulationOptions _options = options ?? new RegulationOptions();

    public async Task<RegulationEvaluation> EvaluateAsync(
        string driverCardId,
        GameTime now,
        CancellationToken cancellationToken = default)
    {
        var history = await activities.LoadDriverHistoryAsync(
            driverCardId,
            cancellationToken: cancellationToken);
        var storedDecisions = await decisions.LoadDriverDecisionsAsync(
            driverCardId,
            cancellationToken);
        var evaluation = _engine.Evaluate(
            new RuleContext(now, history),
            _options,
            storedDecisions);
        await decisions.InvalidateMissingRestBlocksAsync(
            driverCardId,
            evaluation.RestAllocations
                .Select(item => item.RestBlockId)
                .ToHashSet(StringComparer.Ordinal),
            cancellationToken);
        return evaluation;
    }

    public async Task<RegulationEvaluation> DecideAsync(
        string driverCardId,
        string restBlockId,
        string candidateId,
        GameTime now,
        DateTimeOffset decidedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var current = await EvaluateAsync(driverCardId, now, cancellationToken);
        var allocation = current.RestAllocations.SingleOrDefault(item =>
            string.Equals(item.RestBlockId, restBlockId, StringComparison.Ordinal)) ??
            throw new InvalidOperationException("Blok odpoczynku nie jest już kanoniczny.");
        var candidate = allocation.Candidates.SingleOrDefault(item =>
            string.Equals(item.CandidateId, candidateId, StringComparison.Ordinal)) ??
            throw new InvalidOperationException("Wybrana kandydatura nie jest już dopuszczalna.");

        await decisions.SaveDecisionAsync(
            new RestAllocationDecision(
                Guid.NewGuid(),
                driverCardId,
                allocation.RestBlockId,
                candidate.CandidateId,
                allocation.EndExclusive.TotalMinutes,
                decidedAtUtc,
                DecisionSchemeVersion: 1),
            cancellationToken);
        return await EvaluateAsync(driverCardId, now, cancellationToken);
    }
}

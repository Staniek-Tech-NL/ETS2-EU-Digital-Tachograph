using ETS2Tachograph.RuleEngine;

namespace ETS2Tachograph.Application.Persistence;

public interface IRestAllocationRepository
{
    Task<IReadOnlyList<RestAllocationDecision>> LoadDriverDecisionsAsync(
        string driverCardId,
        CancellationToken cancellationToken = default);

    Task<RestAllocationDecision> SaveDecisionAsync(
        RestAllocationDecision decision,
        CancellationToken cancellationToken = default);

    Task InvalidateMissingRestBlocksAsync(
        string driverCardId,
        IReadOnlySet<string> validRestBlockIds,
        CancellationToken cancellationToken = default);
}

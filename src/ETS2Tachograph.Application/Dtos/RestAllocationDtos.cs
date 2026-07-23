using ETS2Tachograph.RuleEngine;

namespace ETS2Tachograph.Application.Dtos;

public sealed record RestAllocationCandidateDto(
    string CandidateId,
    string RestBlockId,
    RestAllocationPurpose Purpose,
    int HostMinimumMinutes,
    IReadOnlyList<string> ObligationIds,
    int NewDebtMinutes,
    bool SatisfiesWeeklyRestRequirement);

public sealed record RestAllocationDecisionDto(
    Guid DecisionId,
    string DriverCardId,
    string RestBlockId,
    string CandidateId,
    long EffectiveAtGameMinute,
    DateTimeOffset DecidedAtUtc,
    int DecisionSchemeVersion,
    RestAllocationDecisionStatus Status,
    Guid? SupersedesDecisionId);

public sealed record RestAllocationProjectionDto(
    string RestBlockId,
    string DriverCardId,
    long StartGameMinute,
    long EndGameMinuteExclusive,
    IReadOnlyList<RestAllocationCandidateDto> Candidates,
    RestAllocationDecisionDto? Decision,
    RestAllocationCandidateDto? SelectedCandidate,
    bool IsPending,
    bool HasInvalidDecision);

public static class RestAllocationDtoMapper
{
    public static IReadOnlyList<RestAllocationProjectionDto> MapAll(
        IEnumerable<RestAllocationProjection> source) =>
        source.Select(Map).ToList();

    public static RestAllocationProjectionDto Map(RestAllocationProjection source) => new(
        source.RestBlockId,
        source.DriverCardId,
        source.Start.TotalMinutes,
        source.EndExclusive.TotalMinutes,
        source.Candidates.Select(Map).ToList(),
        source.Decision is null ? null : Map(source.Decision),
        source.SelectedCandidate is null ? null : Map(source.SelectedCandidate),
        source.IsPending,
        source.HasInvalidDecision);

    private static RestAllocationCandidateDto Map(RestAllocationCandidate source) => new(
        source.CandidateId,
        source.RestBlockId,
        source.Purpose,
        source.HostMinimumMinutes,
        source.ObligationIds.ToList(),
        source.NewDebtMinutes,
        source.SatisfiesWeeklyRestRequirement);

    private static RestAllocationDecisionDto Map(RestAllocationDecision source) => new(
        source.DecisionId,
        source.DriverCardId,
        source.RestBlockId,
        source.CandidateId,
        source.EffectiveAtGameMinute,
        source.DecidedAtUtc,
        source.DecisionSchemeVersion,
        source.Status,
        source.SupersedesDecisionId);
}

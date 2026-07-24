using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.RuleEngine;

public enum RestAllocationPurpose
{
    DailyRestWithCompensation = 0,
    ReducedWeeklyRestOnly = 1,
    ReducedWeeklyRestWithCompensation = 2,
    RegularWeeklyRestOnly = 3,
    RegularWeeklyRestWithCompensation = 4
}

public enum RestAllocationDecisionStatus
{
    Active = 0,
    Superseded = 1,
    Invalidated = 2
}

public sealed record RestAllocationCandidate(
    string CandidateId,
    string RestBlockId,
    RestAllocationPurpose Purpose,
    int HostMinimumMinutes,
    IReadOnlyList<string> ObligationIds,
    int NewDebtMinutes,
    bool SatisfiesWeeklyRestRequirement);

public sealed record RestAllocationDecision(
    Guid DecisionId,
    string DriverCardId,
    string RestBlockId,
    string CandidateId,
    long EffectiveAtGameMinute,
    DateTimeOffset DecidedAtUtc,
    int DecisionSchemeVersion,
    RestAllocationDecisionStatus Status = RestAllocationDecisionStatus.Active,
    Guid? SupersedesDecisionId = null);

public sealed record RestAllocationProjection(
    string RestBlockId,
    string DriverCardId,
    GameTime Start,
    GameTime EndExclusive,
    IReadOnlyList<RestAllocationCandidate> Candidates,
    RestAllocationDecision? Decision,
    RestAllocationCandidate? SelectedCandidate)
{
    public bool IsPending =>
        SelectedCandidate is null &&
        (Candidates.Count > 1 || Decision is not null);
    public bool HasInvalidDecision => Decision is not null && SelectedCandidate is null;
}

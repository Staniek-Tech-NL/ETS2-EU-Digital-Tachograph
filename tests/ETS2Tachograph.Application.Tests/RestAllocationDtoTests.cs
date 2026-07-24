using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.RuleEngine;

namespace ETS2Tachograph.Application.Tests;

public sealed class RestAllocationDtoTests
{
    [Fact]
    public void Mapper_preserves_candidate_decision_and_audit_fields()
    {
        var obligationIds = new[] { "obligation-1", "obligation-2" };
        var candidate = new RestAllocationCandidate(
            "candidate-1",
            "rest-1",
            RestAllocationPurpose.RegularWeeklyRestWithCompensation,
            2_700,
            obligationIds,
            NewDebtMinutes: 0,
            SatisfiesWeeklyRestRequirement: true);
        var supersededId = Guid.NewGuid();
        var decision = new RestAllocationDecision(
            Guid.NewGuid(),
            "CARD-1",
            "rest-1",
            "candidate-1",
            EffectiveAtGameMinute: 5_000,
            DateTimeOffset.UnixEpoch.AddMinutes(5_000),
            DecisionSchemeVersion: 1,
            RestAllocationDecisionStatus.Active,
            supersededId);
        var projection = new RestAllocationProjection(
            "rest-1",
            "CARD-1",
            new GameTime(1_000),
            new GameTime(5_000),
            [candidate],
            decision,
            candidate);

        var result = RestAllocationDtoMapper.Map(projection);

        Assert.Equal(projection.RestBlockId, result.RestBlockId);
        Assert.Equal(projection.DriverCardId, result.DriverCardId);
        Assert.Equal(1_000, result.StartGameMinute);
        Assert.Equal(5_000, result.EndGameMinuteExclusive);
        Assert.False(result.IsPending);
        Assert.False(result.HasInvalidDecision);
        Assert.Equal(candidate.CandidateId, result.SelectedCandidate?.CandidateId);
        Assert.Equal(candidate.Purpose, result.SelectedCandidate?.Purpose);
        Assert.Equal(candidate.HostMinimumMinutes, result.SelectedCandidate?.HostMinimumMinutes);
        Assert.Equal(obligationIds, result.SelectedCandidate?.ObligationIds);
        Assert.Equal(decision.DecisionId, result.Decision?.DecisionId);
        Assert.Equal(decision.DecidedAtUtc, result.Decision?.DecidedAtUtc);
        Assert.Equal(supersededId, result.Decision?.SupersedesDecisionId);
    }

    [Fact]
    public void Pending_projection_is_exposed_without_selected_candidate()
    {
        var candidateOne = Candidate(
            "daily",
            RestAllocationPurpose.DailyRestWithCompensation);
        var candidateTwo = Candidate(
            "weekly",
            RestAllocationPurpose.ReducedWeeklyRestOnly);
        var projection = new RestAllocationProjection(
            "rest",
            "CARD",
            new GameTime(100),
            new GameTime(2_000),
            [candidateOne, candidateTwo],
            Decision: null,
            SelectedCandidate: null);

        var result = RestAllocationDtoMapper.Map(projection);

        Assert.True(result.IsPending);
        Assert.Null(result.Decision);
        Assert.Null(result.SelectedCandidate);
        Assert.Equal(2, result.Candidates.Count);
    }

    [Fact]
    public void Invalid_decision_with_one_current_candidate_requires_new_selection()
    {
        var candidate = Candidate(
            "current-candidate",
            RestAllocationPurpose.ReducedWeeklyRestOnly);
        var invalidDecision = new RestAllocationDecision(
            Guid.NewGuid(),
            "CARD",
            "rest",
            "obsolete-candidate",
            EffectiveAtGameMinute: 2_000,
            DateTimeOffset.UnixEpoch,
            DecisionSchemeVersion: 1);
        var projection = new RestAllocationProjection(
            "rest",
            "CARD",
            new GameTime(100),
            new GameTime(2_000),
            [candidate],
            invalidDecision,
            SelectedCandidate: null);

        var result = RestAllocationDtoMapper.Map(projection);

        Assert.True(result.IsPending);
        Assert.True(result.HasInvalidDecision);
        Assert.Single(result.Candidates);
        Assert.Null(result.SelectedCandidate);
    }

    private static RestAllocationCandidate Candidate(
        string id,
        RestAllocationPurpose purpose) => new(
        id,
        "rest",
        purpose,
        purpose == RestAllocationPurpose.DailyRestWithCompensation ? 540 : 1_440,
        purpose == RestAllocationPurpose.DailyRestWithCompensation
            ? ["obligation"]
            : [],
        purpose == RestAllocationPurpose.ReducedWeeklyRestOnly ? 900 : 0,
        purpose != RestAllocationPurpose.DailyRestWithCompensation);
}

using ETS2Tachograph.RuleEngine.JourneyPlanning;

namespace ETS2Tachograph.RuleEngine.Tests;

public sealed class JourneyPlanningContractTests
{
    public static TheoryData<string, JourneyPlanStatus> RequiredStatuses => new()
    {
        { "JP-ST-01", JourneyPlanStatus.MeetsDeadline },
        { "JP-ST-02", JourneyPlanStatus.MissesDeadline },
        { "JP-ST-03", JourneyPlanStatus.BlockedByGap },
        { "JP-ST-04", JourneyPlanStatus.InsufficientData },
        { "JP-ST-05", JourneyPlanStatus.StaleSnapshot },
        { "JP-ST-06", JourneyPlanStatus.UnsupportedScenario },
        { "JP-ST-07", JourneyPlanStatus.NoLegalContinuation },
        { "JP-ST-08", JourneyPlanStatus.CalculationLimitReached }
    };

    [Theory]
    [MemberData(nameof(RequiredStatuses))]
    [Trait("Stage", "M1Contract")]
    public void Status_contract_contains_every_required_terminal_state(
        string testId,
        JourneyPlanStatus expected)
    {
        Assert.Equal(expected, Enum.Parse<JourneyPlanStatus>(expected.ToString()));
        Assert.StartsWith("JP-ST-", testId, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Stage", "M1Contract")]
    public void Daily_rest_window_models_completion_deadline_for_regular_and_reduced_rest()
    {
        var window = new DailyRestPlanningWindow(
            CompletionDeadlineGameMinute: 10_000,
            LatestRegularRestStartGameMinute: 10_000 - (11 * 60),
            LatestReducedRestStartGameMinute: 10_000 - (9 * 60));

        Assert.Equal(9_340, window.LatestRegularRestStartGameMinute);
        Assert.Equal(9_460, window.LatestReducedRestStartGameMinute);
    }

    [Fact]
    [Trait("Stage", "M1Contract")]
    public void Result_keeps_arrival_and_delivery_completion_as_distinct_values()
    {
        var snapshot = JourneyPlanningTestData.Snapshot(startGameMinute: 1_000);
        var result = new JourneyPlanResult(
            JourneyPlanStatus.MeetsDeadline,
            JourneyPlanConfidence.VerifiedByCurrentRuleModel,
            snapshot.StartGameMinute,
            EarliestArrivalGameMinute: 1_120,
            EarliestCompletionGameMinute: 1_150,
            RequiredElapsedMinutes: 150,
            MarginMinutes: 30,
            Segments: [],
            Warnings: [],
            JourneyPlanUsageSummary.Empty,
            snapshot.Identity);

        Assert.Equal(1_120, result.EarliestArrivalGameMinute);
        Assert.Equal(1_150, result.EarliestCompletionGameMinute);
    }

    [Theory]
    [InlineData(nameof(JourneyPlanningSnapshot.DriverSlot), JourneyPlanSnapshotMismatch.DriverSlotChanged)]
    [InlineData(nameof(JourneyPlanningSnapshot.ActivitySessionId), JourneyPlanSnapshotMismatch.ActivitySessionChanged)]
    [InlineData(nameof(JourneyPlanningSnapshot.WorldGeneration), JourneyPlanSnapshotMismatch.WorldGenerationChanged)]
    [InlineData(nameof(JourneyPlanningSnapshot.HistoryHighWaterMark), JourneyPlanSnapshotMismatch.HistoryChanged)]
    [InlineData(nameof(JourneyPlanningSnapshot.WeekEpochOffsetDays), JourneyPlanSnapshotMismatch.WeekDefinitionChanged)]
    [InlineData(nameof(JourneyPlanningSnapshot.StartGameMinute), JourneyPlanSnapshotMismatch.StartGameMinuteChanged)]
    [Trait("Stage", "M1Contract")]
    public void Snapshot_identity_detects_every_change_that_invalidates_a_result(
        string changedMember,
        JourneyPlanSnapshotMismatch expected)
    {
        var original = JourneyPlanningTestData.Snapshot(startGameMinute: 1_000);
        var current = changedMember switch
        {
            nameof(JourneyPlanningSnapshot.DriverSlot) => original with { DriverSlot = 2 },
            nameof(JourneyPlanningSnapshot.ActivitySessionId) => original with { ActivitySessionId = Guid.NewGuid() },
            nameof(JourneyPlanningSnapshot.WorldGeneration) => original with { WorldGeneration = 8 },
            nameof(JourneyPlanningSnapshot.HistoryHighWaterMark) => original with { HistoryHighWaterMark = 43 },
            nameof(JourneyPlanningSnapshot.WeekEpochOffsetDays) => original with { WeekEpochOffsetDays = 1 },
            nameof(JourneyPlanningSnapshot.StartGameMinute) => original with { StartGameMinute = 1_001 },
            _ => throw new ArgumentOutOfRangeException(nameof(changedMember))
        };

        Assert.Equal(expected, original.Identity.CompareTo(current));
        Assert.False(original.Identity.IsCurrentFor(current));
    }

    [Fact]
    [Trait("Stage", "M1Contract")]
    public void Snapshot_identity_distinguishes_game_time_rollback()
    {
        var original = JourneyPlanningTestData.Snapshot(startGameMinute: 1_000);
        var current = original with { StartGameMinute = 999 };

        Assert.Equal(
            JourneyPlanSnapshotMismatch.GameTimeMovedBackward,
            original.Identity.CompareTo(current));
    }

    [Fact]
    [Trait("Stage", "M1Contract")]
    public void RuleEngine_planner_contract_has_no_WPF_or_SQLite_dependency()
    {
        var references = typeof(JourneyPlanRequest).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.DoesNotContain(references, name =>
            name is not null &&
            (name.Contains("PresentationFramework", StringComparison.OrdinalIgnoreCase) ||
             name.Contains("EntityFrameworkCore", StringComparison.OrdinalIgnoreCase) ||
             name.Contains("SQLite", StringComparison.OrdinalIgnoreCase) ||
             name.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase)));
    }
}

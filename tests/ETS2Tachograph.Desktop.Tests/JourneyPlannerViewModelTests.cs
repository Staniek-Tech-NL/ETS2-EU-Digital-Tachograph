using ETS2Tachograph.Application.Services;
using ETS2Tachograph.Desktop;
using ETS2Tachograph.RuleEngine.JourneyPlanning;

namespace ETS2Tachograph.Desktop.Tests;

public sealed class JourneyPlannerViewModelTests
{
    [Theory]
    [InlineData("00:00", 0)]
    [InlineData("12:35", 755)]
    [InlineData("28:00", 1_680)]
    public void Duration_parser_accepts_full_minutes_and_hours_above_23(
        string text,
        int expected)
    {
        Assert.True(JourneyPlannerViewModel.TryParseDuration(text, out var minutes));
        Assert.Equal(expected, minutes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("01:60")]
    [InlineData("-1:00")]
    [InlineData("aa:10")]
    public void Duration_parser_rejects_invalid_machine_input(string text)
    {
        Assert.False(JourneyPlannerViewModel.TryParseDuration(text, out _));
    }

    [Fact]
    public async Task Calculation_presents_arrival_completion_margin_and_segments()
    {
        var service = new FakePlannerService(Result());
        var viewModel = new JourneyPlannerViewModel(service)
        {
            RemainingDrive = "01:00",
            DeliveryWindow = "02:00",
            OperationalBuffer = "00:30"
        };

        await viewModel.CalculateAsync();

        Assert.True(viewModel.HasResult);
        Assert.Equal("Plan mieści się w terminie.", viewModel.StatusText);
        Assert.NotEqual("—", viewModel.ArrivalText);
        Assert.NotEqual("—", viewModel.CompletionText);
        Assert.Equal("+00:30", viewModel.MarginText);
        Assert.Single(viewModel.Segments);
    }

    [Fact]
    public async Task Changed_snapshot_removes_old_result_and_requests_recalculation()
    {
        var service = new FakePlannerService(Result());
        var viewModel = new JourneyPlannerViewModel(service);
        await viewModel.CalculateAsync();
        service.Current = false;

        viewModel.ObserveStateChange();

        Assert.False(viewModel.HasResult);
        Assert.Empty(viewModel.Segments);
        Assert.Contains("Oblicz plan ponownie", viewModel.StatusText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invalid_form_does_not_call_service()
    {
        var service = new FakePlannerService(Result());
        var viewModel = new JourneyPlannerViewModel(service)
        {
            RemainingDrive = "01:99"
        };

        await viewModel.CalculateAsync();

        Assert.Equal(0, service.CallCount);
        Assert.NotEmpty(viewModel.ValidationMessage);
    }

    private static JourneyPlanResult Result()
    {
        var identity = new JourneyPlanSnapshotIdentity(1, 100, Guid.NewGuid(), 7, 42, 0);
        return new(
            JourneyPlanStatus.MeetsDeadline,
            JourneyPlanConfidence.VerifiedByCurrentRuleModel,
            100,
            160,
            190,
            90,
            30,
            [
                new(
                    JourneyPlanSegmentType.Drive,
                    1,
                    100,
                    160,
                    60,
                    JourneyPlanSegmentReason.RemainingRouteDrive,
                    ETS2Tachograph.Core.Enums.DriverActivity.Driving,
                    false,
                    null)
            ],
            [],
            JourneyPlanUsageSummary.Empty,
            identity);
    }

    private sealed class FakePlannerService(JourneyPlanResult result) : IJourneyPlannerService
    {
        internal bool Current { get; set; } = true;
        internal int CallCount { get; private set; }

        public Task<JourneyPlanResult> PlanAsync(
            JourneyPlannerInput input,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(result);
        }

        public bool IsCurrent(JourneyPlanSnapshotIdentity identity) => Current;
    }
}

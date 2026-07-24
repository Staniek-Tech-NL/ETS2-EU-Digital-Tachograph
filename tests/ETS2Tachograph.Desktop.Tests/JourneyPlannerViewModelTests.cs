using ETS2Tachograph.Application.Services;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.Desktop;
using ETS2Tachograph.RuleEngine.JourneyPlanning;

namespace ETS2Tachograph.Desktop.Tests;

public sealed class JourneyPlannerViewModelTests
{
    [Theory]
    [InlineData("00:00", 0)]
    [InlineData("12:35", 755)]
    [InlineData("28:00", 1_680)]
    public void Duration_parser_accepts_hours_above_23(string text, int expected)
    {
        Assert.True(JourneyPlannerViewModel.TryParseDuration(text, out var minutes));
        Assert.Equal(expected, minutes);
    }

    [Fact]
    public async Task Market_offer_presents_verdict_calendar_and_all_phases()
    {
        var service = new FakeService(Result(DeliveryPlanningUseCase.MarketOffer));
        var viewModel = new JourneyPlannerViewModel(service);

        await viewModel.CalculateAsync();

        Assert.True(viewModel.HasResult);
        Assert.Equal("MOŻNA PRZYJĄĆ", viewModel.StatusText);
        Assert.Contains("D1", viewModel.CurrentTimeText, StringComparison.Ordinal);
        Assert.NotEqual("—", viewModel.OfferExpiryText);
        Assert.Equal("+00:30", viewModel.MarginText);
        Assert.Equal(3, viewModel.Segments.Count);
        Assert.Equal(1, service.MarketCalls);
        Assert.Equal(GameWeekday.Thursday, service.LastMarketInput!.DeliveryWindowStart.Weekday);
        Assert.Equal(21, service.LastMarketInput.DeliveryWindowStart.Hour);
        Assert.Equal(GameWeekday.Saturday, service.LastMarketInput.DeliveryWindowEnd.Weekday);
        Assert.Equal(1, service.LastMarketInput.DeliveryWindowEnd.Hour);
    }

    [Fact]
    public async Task Active_delivery_uses_separate_service_path()
    {
        var service = new FakeService(Result(DeliveryPlanningUseCase.ActiveDelivery));
        var viewModel = new JourneyPlannerViewModel(service)
        {
            SelectedMode = new JourneyPlannerModeOption(false, "Aktywna dostawa")
        };

        await viewModel.CalculateAsync();

        Assert.Equal(1, service.ActiveCalls);
        Assert.Equal("Nie dotyczy", viewModel.OfferExpiryText);
    }

    [Fact]
    public async Task Changed_crew_snapshot_removes_old_result()
    {
        var service = new FakeService(Result(DeliveryPlanningUseCase.MarketOffer));
        var viewModel = new JourneyPlannerViewModel(service);
        await viewModel.CalculateAsync();
        service.Current = false;

        viewModel.ObserveStateChange();

        Assert.False(viewModel.HasResult);
        Assert.Empty(viewModel.Segments);
        Assert.Contains("Oblicz plan ponownie", viewModel.StatusText);
    }

    [Fact]
    public void Telemetry_state_observation_does_not_reload_planner_readiness()
    {
        var service = new FakeService(Result(DeliveryPlanningUseCase.MarketOffer));
        var viewModel = new JourneyPlannerViewModel(service);
        var initialReadinessCalls = service.ReadinessCalls;

        for (var frame = 0; frame < 100; frame++)
            viewModel.ObserveStateChange();

        Assert.Equal(initialReadinessCalls, service.ReadinessCalls);
    }

    [Fact]
    public async Task Invalid_market_input_does_not_call_service()
    {
        var service = new FakeService(Result(DeliveryPlanningUseCase.MarketOffer));
        var viewModel = new JourneyPlannerViewModel(service)
        {
            DriveToPickup = "01:99"
        };

        await viewModel.CalculateAsync();

        Assert.Equal(0, service.MarketCalls);
        Assert.NotEmpty(viewModel.ValidationMessage);
    }

    [Theory]
    [InlineData(DeliveryPlanFailureReason.OfferExpired, "NIE ZDĄŻYSZ ODEBRAĆ")]
    [InlineData(DeliveryPlanFailureReason.DeliveryWindowMissed, "NIE ZDĄŻYSZ DOSTARCZYĆ")]
    [InlineData(DeliveryPlanFailureReason.NoLegalContinuation, "BRAK LEGALNEJ KONTYNUACJI")]
    [InlineData(DeliveryPlanFailureReason.InsufficientData, "BRAK WIARYGODNYCH DANYCH")]
    [InlineData(DeliveryPlanFailureReason.StaleSnapshot, "BRAK WIARYGODNYCH DANYCH")]
    public async Task Failure_reason_maps_to_product_verdict(
        DeliveryPlanFailureReason failure,
        string expected)
    {
        var rejected = Result(DeliveryPlanningUseCase.MarketOffer) with
        {
            Verdict = DeliveryPlanVerdict.Reject,
            FailureReason = failure
        };
        var viewModel = new JourneyPlannerViewModel(new FakeService(rejected));

        await viewModel.CalculateAsync();

        Assert.Equal(expected, viewModel.StatusText);
    }

    private static DeliveryPlanResult Result(DeliveryPlanningUseCase useCase)
    {
        var slot1 = new JourneyPlanSnapshotIdentity(1, 100, Guid.NewGuid(), 7, 42, 0);
        var slot2 = new JourneyPlanSnapshotIdentity(2, 100, Guid.NewGuid(), 7, 43, 0);
        return new DeliveryPlanResult(
            useCase,
            DeliveryPlanVerdict.Take,
            DeliveryPlanFailureReason.None,
            100,
            useCase == DeliveryPlanningUseCase.MarketOffer ? 600 : null,
            100,
            400,
            110,
            125,
            200,
            370,
            30,
            0,
            [
                Segment(DeliveryPlanPhase.DriveToPickup, 100, 110, 1),
                Segment(DeliveryPlanPhase.Pickup, 110, 125, null),
                Segment(DeliveryPlanPhase.DriveWithCargo, 125, 200, 2)
            ],
            [],
            new CrewDeliveryPlanSnapshotIdentity(100, 7, 0, true, slot1, slot2));
    }

    private static DeliveryPlanSegment Segment(
        DeliveryPlanPhase phase, long start, long end, int? slot) => new(
        phase, start, end, slot,
        slot == 1 ? DriverActivity.Driving : DriverActivity.Availability,
        slot == 2 ? DriverActivity.Driving : DriverActivity.Availability);

    private sealed class FakeService(DeliveryPlanResult result) : IDeliveryPlannerService
    {
        internal bool Current { get; set; } = true;
        internal int MarketCalls { get; private set; }
        internal int ActiveCalls { get; private set; }
        internal int ReadinessCalls { get; private set; }
        internal MarketOfferPlannerInput? LastMarketInput { get; private set; }

        public Task<DeliveryPlannerReadiness> GetReadinessAsync(
            CancellationToken cancellationToken = default)
        {
            ReadinessCalls++;
            return Task.FromResult(new DeliveryPlannerReadiness(
                true,
                100,
                0,
                "S1",
                "S2",
                true,
                true,
                false,
                []));
        }

        public Task<DeliveryPlanResult> PlanMarketOfferAsync(
            MarketOfferPlannerInput input,
            CancellationToken cancellationToken = default)
        {
            MarketCalls++;
            LastMarketInput = input;
            return Task.FromResult(result with
            {
                UseCase = DeliveryPlanningUseCase.MarketOffer
            });
        }

        public Task<DeliveryPlanResult> PlanActiveDeliveryAsync(
            ActiveDeliveryPlannerInput input,
            CancellationToken cancellationToken = default)
        {
            ActiveCalls++;
            return Task.FromResult(result with
            {
                UseCase = DeliveryPlanningUseCase.ActiveDelivery,
                OfferExpiresAtGameMinuteExclusive = null
            });
        }

        public bool IsCurrent(CrewDeliveryPlanSnapshotIdentity identity) => Current;
    }
}

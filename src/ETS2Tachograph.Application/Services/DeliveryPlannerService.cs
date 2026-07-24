using System.Security.Cryptography;
using System.Text;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.Engine;
using ETS2Tachograph.RuleEngine.JourneyPlanning;

namespace ETS2Tachograph.Application.Services;

public sealed record MarketOfferPlannerInput(
    int InitialDrivingSlot,
    int DriveToPickupMinutes,
    int OfferExpiresInMinutes,
    int LoadedRouteDriveMinutes,
    GameWeekdayTime DeliveryWindowStart,
    GameWeekdayTime DeliveryWindowEnd,
    int PickupWorkMinutes,
    int UnloadingWorkMinutes,
    int PostDeliveryWorkMinutes,
    int TightMarginThresholdMinutes);

public sealed record ActiveDeliveryPlannerInput(
    int InitialDrivingSlot,
    int RemainingLoadedRouteDriveMinutes,
    GameWeekdayTime DeliveryWindowStart,
    GameWeekdayTime DeliveryWindowEnd,
    int UnloadingWorkMinutes,
    int PostDeliveryWorkMinutes,
    int TightMarginThresholdMinutes);

public sealed record DeliveryPlannerReadiness(
    bool IsReady,
    long? CurrentGameMinute,
    int WeekEpochOffsetDays,
    string? Slot1CardId,
    string? Slot2CardId,
    bool MultiManningActive,
    bool TelemetryAvailable,
    bool HasBlockingCardRemovedGap,
    IReadOnlyList<string> Issues);

public interface IDeliveryPlannerService
{
    Task<DeliveryPlannerReadiness> GetReadinessAsync(
        CancellationToken cancellationToken = default);

    Task<DeliveryPlanResult> PlanMarketOfferAsync(
        MarketOfferPlannerInput input,
        CancellationToken cancellationToken = default);

    Task<DeliveryPlanResult> PlanActiveDeliveryAsync(
        ActiveDeliveryPlannerInput input,
        CancellationToken cancellationToken = default);

    bool IsCurrent(CrewDeliveryPlanSnapshotIdentity identity);
}

public sealed class DeliveryPlannerService(
    CrewTachographService crew,
    DeliveryPlanningEngine? planningEngine = null) : IDeliveryPlannerService
{
    private readonly DeliveryPlanningEngine _planningEngine =
        planningEngine ?? new DeliveryPlanningEngine();
    private readonly object _identityGate = new();
    private (
        CrewDeliveryPlanSnapshotIdentity Identity,
        CrewTachographSnapshot Owner)? _snapshotOwner;

    public async Task<DeliveryPlannerReadiness> GetReadinessAsync(
        CancellationToken cancellationToken = default)
    {
        var snapshot = await GetSnapshotAsync(cancellationToken);
        if (snapshot is null)
        {
            return new DeliveryPlannerReadiness(
                false,
                crew.Current.Frame?.GameTime.TotalMinutes,
                crew.Engine.WeekEpochOffsetDays,
                crew.Current.DriverCardId,
                crew.Current.CoDriverCardId,
                crew.Current.MultiManning,
                crew.Current.Frame is not null,
                crew.Current.ManualEntryRequired,
                ["Wymagany jest aktualny snapshot telemetryczny obu kart w podwójnej obsadzie."]);
        }

        var blockingGap = new[] { snapshot.Slot1, snapshot.Slot2 }
            .SelectMany(driver => driver.Gaps)
            .Any(gap =>
                gap.Reason == ActivityGapReason.CardRemoved &&
                gap.State == ActivityGapState.Unresolved);
        var issues = blockingGap
            ? new[] { "Rozlicz lukę po wyjęciu karty przed obliczeniem planu." }
            : [];
        return new DeliveryPlannerReadiness(
            !blockingGap,
            snapshot.StartGameMinute,
            snapshot.WeekEpochOffsetDays,
            snapshot.Slot1.DriverCardId,
            snapshot.Slot2.DriverCardId,
            snapshot.MultiManningActive,
            snapshot.TelemetryAvailable,
            blockingGap,
            issues);
    }

    public async Task<DeliveryPlanResult> PlanMarketOfferAsync(
        MarketOfferPlannerInput input,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await GetSnapshotAsync(cancellationToken);
        if (snapshot is null)
            return Unavailable(DeliveryPlanningUseCase.MarketOffer);
        var calendar = new GameCalendarResolver(
            new GameCalendarContext(snapshot.WeekEpochOffsetDays));
        var windowStart = calendar.ResolveNext(
            input.DeliveryWindowStart,
            new GameTime(snapshot.StartGameMinute));
        var windowEnd = calendar.ResolveNext(
            input.DeliveryWindowEnd,
            windowStart.GameTime.AddMinutes(1));

        var result = _planningEngine.Plan(new MarketOffer(
            snapshot,
            input.InitialDrivingSlot,
            input.DriveToPickupMinutes,
            checked(snapshot.StartGameMinute + input.OfferExpiresInMinutes),
            input.LoadedRouteDriveMinutes,
            windowStart.GameTime.TotalMinutes,
            windowEnd.GameTime.TotalMinutes,
            input.PickupWorkMinutes,
            input.UnloadingWorkMinutes,
            input.PostDeliveryWorkMinutes,
            input.TightMarginThresholdMinutes,
            JourneyPlanningLimits.Default));
        return CurrentOrStale(result);
    }

    public async Task<DeliveryPlanResult> PlanActiveDeliveryAsync(
        ActiveDeliveryPlannerInput input,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await GetSnapshotAsync(cancellationToken);
        if (snapshot is null)
            return Unavailable(DeliveryPlanningUseCase.ActiveDelivery);
        var calendar = new GameCalendarResolver(
            new GameCalendarContext(snapshot.WeekEpochOffsetDays));
        var windowStart = calendar.ResolveNext(
            input.DeliveryWindowStart,
            new GameTime(snapshot.StartGameMinute));
        var windowEnd = calendar.ResolveNext(
            input.DeliveryWindowEnd,
            windowStart.GameTime.AddMinutes(1));

        var result = _planningEngine.Plan(new ActiveDelivery(
            snapshot,
            input.InitialDrivingSlot,
            input.RemainingLoadedRouteDriveMinutes,
            windowStart.GameTime.TotalMinutes,
            windowEnd.GameTime.TotalMinutes,
            input.UnloadingWorkMinutes,
            input.PostDeliveryWorkMinutes,
            input.TightMarginThresholdMinutes,
            JourneyPlanningLimits.Default));
        return CurrentOrStale(result);
    }

    public async Task<CrewJourneyPlanningSnapshot?> GetSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var captured = crew.Current;
            if (captured.DriverCardId is null ||
                captured.CoDriverCardId is null ||
                captured.Driver?.Regulation is null ||
                captured.CoDriver?.Regulation is null ||
                !captured.MultiManning)
            {
                return null;
            }

            var slot1History = await crew.LoadDriverHistoryAsync(
                captured.DriverCardId,
                cancellationToken: cancellationToken);
            var slot1Gaps = await crew.LoadDriverGapsAsync(
                captured.DriverCardId,
                cancellationToken);
            var slot2History = await crew.LoadDriverHistoryAsync(
                captured.CoDriverCardId,
                cancellationToken: cancellationToken);
            var slot2Gaps = await crew.LoadDriverGapsAsync(
                captured.CoDriverCardId,
                cancellationToken);

            var afterLoad = crew.Current;
            if (!SamePlanningState(captured, afterLoad))
                continue;

            var slot1 = DriverSnapshot(
                1,
                captured.DriverCardId,
                captured.Driver,
                slot1History,
                slot1Gaps);
            var slot2 = DriverSnapshot(
                2,
                captured.CoDriverCardId,
                captured.CoDriver,
                slot2History,
                slot2Gaps);
            var start = captured.Frame?.GameTime.TotalMinutes ??
                        Math.Max(slot1.HistoryHighWaterMark, slot2.HistoryHighWaterMark);
            var snapshot = new CrewJourneyPlanningSnapshot(
                start,
                captured.Frame?.WorldGeneration ?? 0,
                crew.Engine.WeekEpochOffsetDays,
                MultiManningActive: true,
                TelemetryAvailable: captured.Frame is not null,
                slot1,
                slot2);
            var identity = CrewDeliveryPlanSnapshotIdentity.From(snapshot);
            lock (_identityGate)
                _snapshotOwner = (identity, captured);
            return snapshot;
        }

        return null;
    }

    public bool IsCurrent(CrewDeliveryPlanSnapshotIdentity identity)
    {
        var current = crew.Current;
        lock (_identityGate)
        {
            if (_snapshotOwner is not { } owner ||
                owner.Identity != identity ||
                !SamePlanningState(owner.Owner, current))
            {
                return false;
            }
        }

        return current.DriverCardId is not null &&
               current.CoDriverCardId is not null &&
               current.Driver is not null &&
               current.CoDriver is not null &&
               identity.StartGameMinute ==
               (current.Frame?.GameTime.TotalMinutes ?? identity.StartGameMinute) &&
               identity.WorldGeneration == (current.Frame?.WorldGeneration ?? 0) &&
               identity.WeekEpochOffsetDays == crew.Engine.WeekEpochOffsetDays &&
               identity.MultiManningActive == current.MultiManning &&
               identity.Slot1.ActivitySessionId ==
               SessionIdentity(current.DriverCardId, current.Driver.SessionIndex) &&
               identity.Slot2.ActivitySessionId ==
               SessionIdentity(current.CoDriverCardId, current.CoDriver.SessionIndex);
    }

    private DeliveryPlanResult CurrentOrStale(DeliveryPlanResult result) =>
        IsCurrent(result.SnapshotIdentity)
            ? result
            : result with
            {
                Verdict = DeliveryPlanVerdict.Reject,
                FailureReason = DeliveryPlanFailureReason.StaleSnapshot,
                PickupStartedAtGameMinute = null,
                PickupCompletedAtGameMinute = null,
                ArrivedAtDeliveryGameMinute = null,
                DeliveryCompletedAtGameMinute = null,
                Segments = []
            };

    private static CrewDriverPlanningSnapshot DriverSnapshot(
        int slot,
        string cardId,
        TachographSnapshot tachograph,
        IReadOnlyList<ActivityRecord> loadedHistory,
        IReadOnlyList<ActivityGap> loadedGaps)
    {
        var history = loadedHistory.OrderBy(record => record.Start).ToArray();
        var gaps = loadedGaps.OrderBy(gap => gap.Start).ToArray();
        var highWaterMark = Math.Max(
            history.Select(record => record.EndExclusive.TotalMinutes)
                .DefaultIfEmpty(0)
                .Max(),
            gaps.Select(gap =>
                    gap.EndExclusive?.TotalMinutes ?? gap.Start.TotalMinutes)
                .DefaultIfEmpty(0)
                .Max());
        return new CrewDriverPlanningSnapshot(
            slot,
            cardId,
            SessionIdentity(cardId, tachograph.SessionIndex),
            highWaterMark,
            tachograph.Regulation!,
            history,
            gaps);
    }

    private static bool SamePlanningState(
        CrewTachographSnapshot left,
        CrewTachographSnapshot right) =>
        string.Equals(left.DriverCardId, right.DriverCardId, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(left.CoDriverCardId, right.CoDriverCardId, StringComparison.OrdinalIgnoreCase) &&
        SameTachograph(left.Driver, right.Driver) &&
        SameTachograph(left.CoDriver, right.CoDriver) &&
        left.MultiManning == right.MultiManning &&
        left.Frame?.GameTime.TotalMinutes == right.Frame?.GameTime.TotalMinutes &&
        left.Frame?.WorldGeneration == right.Frame?.WorldGeneration;

    private static bool SameTachograph(
        TachographSnapshot? left,
        TachographSnapshot? right) =>
        left is not null &&
        right is not null &&
        left.SessionIndex == right.SessionIndex &&
        left.ManualActivity == right.ManualActivity &&
        left.OutModeEnabled == right.OutModeEnabled &&
        left.FerryModeEnabled == right.FerryModeEnabled &&
        left.Regulation?.State == right.Regulation?.State &&
        SameGaps(left.CurrentSessionGaps, right.CurrentSessionGaps);

    private static bool SameGaps(
        IReadOnlyList<ActivityGap> left,
        IReadOnlyList<ActivityGap> right) =>
        left.Count == right.Count &&
        left.Zip(right).All(pair =>
            pair.First.Id == pair.Second.Id &&
            pair.First.State == pair.Second.State &&
            pair.First.EndExclusive == pair.Second.EndExclusive);

    private static Guid SessionIdentity(string cardId, int sessionIndex)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(
            string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"{cardId.ToUpperInvariant()}|{sessionIndex}")));
        return new Guid(bytes.AsSpan(0, 16));
    }

    private static DeliveryPlanResult Unavailable(
        DeliveryPlanningUseCase useCase)
    {
        var emptyDriver = new JourneyPlanSnapshotIdentity(
            0,
            0,
            Guid.Empty,
            0,
            0,
            0);
        return new DeliveryPlanResult(
            useCase,
            DeliveryPlanVerdict.Reject,
            DeliveryPlanFailureReason.InsufficientData,
            StartGameMinute: 0,
            OfferExpiresAtGameMinuteExclusive: null,
            DeliveryWindowStartGameMinute: 0,
            DeliveryWindowEndGameMinuteExclusive: 0,
            PickupStartedAtGameMinute: null,
            PickupCompletedAtGameMinute: null,
            ArrivedAtDeliveryGameMinute: null,
            DeliveryCompletedAtGameMinute: null,
            MarginMinutes: 0,
            WeekEpochOffsetDays: 0,
            Segments: [],
            Warnings: [],
            new CrewDeliveryPlanSnapshotIdentity(
                0,
                0,
                0,
                false,
                emptyDriver,
                emptyDriver));
    }
}

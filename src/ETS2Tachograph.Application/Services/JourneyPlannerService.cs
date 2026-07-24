using System.Security.Cryptography;
using System.Text;
using ETS2Tachograph.Engine;
using ETS2Tachograph.RuleEngine.JourneyPlanning;

namespace ETS2Tachograph.Application.Services;

public sealed record JourneyPlannerInput(
    int DriverSlot,
    int RemainingDriveMinutes,
    int DeliveryWindowMinutes,
    int OperationalBufferMinutes);

public interface IJourneyPlannerService
{
    Task<JourneyPlanResult> PlanAsync(
        JourneyPlannerInput input,
        CancellationToken cancellationToken = default);
    bool IsCurrent(JourneyPlanSnapshotIdentity identity);
}

public sealed class JourneyPlannerService(
    CrewTachographService crew,
    JourneyPlanningEngine? planningEngine = null) : IJourneyPlannerService
{
    private readonly JourneyPlanningEngine _planningEngine =
        planningEngine ?? new JourneyPlanningEngine();
    private readonly object _identityGate = new();
    private readonly Dictionary<int, (JourneyPlanSnapshotIdentity Identity, CrewTachographSnapshot Owner)>
        _snapshotOwners = [];

    public async Task<JourneyPlanningSnapshot?> GetSnapshotAsync(
        int driverSlot,
        CancellationToken cancellationToken = default)
    {
        if (driverSlot is not (1 or 2))
        {
            return null;
        }

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var captured = crew.Current;
            var cardId = CardId(captured, driverSlot);
            var tachograph = Tachograph(captured, driverSlot);
            if (cardId is null ||
                tachograph?.Regulation is null)
            {
                return null;
            }

            var historyTask = crew.LoadDriverHistoryAsync(
                cardId,
                cancellationToken: cancellationToken);
            var gapsTask = crew.LoadDriverGapsAsync(cardId, cancellationToken);
            await Task.WhenAll(historyTask, gapsTask);
            var afterLoad = crew.Current;
            if (!ReferenceEquals(captured, afterLoad))
            {
                continue;
            }

            var history = (await historyTask)
                .OrderBy(record => record.Start)
                .ToArray();
            var gaps = (await gapsTask)
                .OrderBy(gap => gap.Start)
                .ToArray();
            var highWaterMark = Math.Max(
                history.Select(record => record.EndExclusive.TotalMinutes).DefaultIfEmpty(0).Max(),
                gaps.Select(gap => gap.EndExclusive?.TotalMinutes ?? gap.Start.TotalMinutes)
                    .DefaultIfEmpty(0)
                    .Max());
            var startGameMinute = captured.Frame?.GameTime.TotalMinutes ?? highWaterMark;

            var result = new JourneyPlanningSnapshot(
                driverSlot,
                startGameMinute,
                SessionIdentity(cardId, tachograph.SessionIndex),
                captured.Frame?.WorldGeneration ?? 0,
                highWaterMark,
                tachograph.Regulation,
                history,
                gaps,
                crew.Engine.WeekEpochOffsetDays,
                captured.MultiManning,
                TelemetryAvailable: captured.Frame is not null);
            lock (_identityGate)
            {
                _snapshotOwners[driverSlot] = (result.Identity, captured);
            }
            return result;
        }

        return null;
    }

    public async Task<JourneyPlanResult> PlanAsync(
        JourneyPlannerInput input,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await GetSnapshotAsync(input.DriverSlot, cancellationToken);
        if (snapshot is null)
        {
            return Unavailable(input.DriverSlot);
        }

        var result = _planningEngine.Plan(new JourneyPlanRequest(
            snapshot,
            input.RemainingDriveMinutes,
            input.DeliveryWindowMinutes,
            input.OperationalBufferMinutes,
            JourneyOperationalBufferPolicy.OtherWorkAfterArrival,
            JourneyPlanningLimits.Default));

        return IsCurrent(result.SnapshotIdentity)
            ? result
            : result with
            {
                Status = JourneyPlanStatus.StaleSnapshot,
                EarliestArrivalGameMinute = null,
                EarliestCompletionGameMinute = null,
                Segments = []
            };
    }

    public bool IsCurrent(JourneyPlanSnapshotIdentity identity)
    {
        var current = crew.Current;
        var cardId = CardId(current, identity.DriverSlot);
        var tachograph = Tachograph(current, identity.DriverSlot);
        if (cardId is null || tachograph is null)
        {
            return false;
        }

        lock (_identityGate)
        {
            if (!_snapshotOwners.TryGetValue(identity.DriverSlot, out var owner) ||
                owner.Identity != identity ||
                !ReferenceEquals(owner.Owner, current))
            {
                return false;
            }
        }

        var timeMatches = current.Frame is null ||
            identity.StartGameMinute == current.Frame.GameTime.TotalMinutes;
        return identity.DriverSlot is 1 or 2 &&
            timeMatches &&
            identity.ActivitySessionId == SessionIdentity(cardId, tachograph.SessionIndex) &&
            identity.WorldGeneration == (current.Frame?.WorldGeneration ?? 0) &&
            identity.WeekEpochOffsetDays == crew.Engine.WeekEpochOffsetDays;
    }

    private static string? CardId(CrewTachographSnapshot snapshot, int slot) =>
        slot == 1 ? snapshot.DriverCardId : snapshot.CoDriverCardId;

    private static TachographSnapshot? Tachograph(CrewTachographSnapshot snapshot, int slot) =>
        slot == 1 ? snapshot.Driver : snapshot.CoDriver;

    private static Guid SessionIdentity(string cardId, int sessionIndex)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(
            string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"{cardId.ToUpperInvariant()}|{sessionIndex}")));
        return new Guid(bytes.AsSpan(0, 16));
    }

    private static JourneyPlanResult Unavailable(int driverSlot) => new(
        JourneyPlanStatus.InsufficientData,
        JourneyPlanConfidence.BasedOnLastSavedState,
        0,
        null,
        null,
        0,
        0,
        [],
        [],
        JourneyPlanUsageSummary.Empty,
        new JourneyPlanSnapshotIdentity(driverSlot, 0, Guid.Empty, 0, 0, 0));
}

using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Telemetry;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.Engine;

namespace ETS2Tachograph.Application.Services;

public sealed class CrewTachographService(
    CrewTachographEngine engine,
    IActivityRepository activities,
    ActivityRetentionService? retention = null,
    IRestAllocationRepository? restAllocations = null)
{
    private readonly Dictionary<string, long> _highWaterMarks =
        new(StringComparer.OrdinalIgnoreCase);

    public CrewTachographEngine Engine => engine;
    public CrewTachographSnapshot Current => engine.Current;

    public async Task RegisterCardAsync(
        string cardId,
        CancellationToken cancellationToken = default)
    {
        if (engine.RegisteredCardIds.Contains(cardId, StringComparer.OrdinalIgnoreCase))
            return;
        if (retention is not null)
        {
            var result = await retention.ArchiveCardAsync(cardId, cancellationToken);
            _highWaterMarks[cardId] = result.HighWaterMarkGameMinute;
        }
        var sessions = await activities.LoadSessionsAsync(cardId, cancellationToken);
        engine.RegisterCard(cardId, sessions.Select(x => new RestoredActivitySession(
            x.SessionIndex,
            x.StartedAt,
            x.Records,
            x.Gaps ?? [])).ToList());
        if (restAllocations is not null)
        {
            engine.SetRestAllocationDecisions(
                cardId,
                await restAllocations.LoadDriverDecisionsAsync(cardId, cancellationToken));
        }
    }

    public void InsertCard(TachographSlot slot, string cardId) => engine.InsertCard(slot, cardId);

    public async Task<InsertedCardResult> InsertCardAsync(
        TachographSlot slot,
        string cardId,
        CancellationToken cancellationToken = default)
    {
        var result = engine.InsertCard(slot, cardId);
        await SaveSnapshotsAsync([(result.CardId, result.Snapshot)], cancellationToken);
        return result;
    }

    public async Task<EjectedCardResult> EjectCardAsync(
        TachographSlot slot,
        DateTimeOffset recordedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var result = engine.EjectCard(slot, recordedAtUtc);
        await SaveSnapshotsAsync([(result.CardId, result.Snapshot)], cancellationToken);
        return result;
    }

    public async Task<CrewTachographSnapshot> ProcessFrameAsync(
        TelemetryFrame frame,
        CancellationToken cancellationToken = default)
    {
        if (frame.GamePaused)
            return engine.ProcessFrame(frame);

        await ObserveGameTimeAsync(frame, cancellationToken);
        var snapshot = engine.ProcessFrame(frame);
        var cardSnapshots = new List<(string CardId, TachographSnapshot Snapshot)>();
        if (snapshot.DriverCardId is not null && snapshot.Driver is not null)
            cardSnapshots.Add((snapshot.DriverCardId, snapshot.Driver));
        if (snapshot.CoDriverCardId is not null && snapshot.CoDriver is not null)
            cardSnapshots.Add((snapshot.CoDriverCardId, snapshot.CoDriver));
        cardSnapshots.AddRange(snapshot.DetachedCardUpdates.Select(update =>
            (update.CardId, update.Snapshot)));
        await SaveSnapshotsAsync(cardSnapshots, cancellationToken);
        return snapshot;
    }

    private async Task ObserveGameTimeAsync(
        TelemetryFrame frame,
        CancellationToken cancellationToken)
    {
        if (retention is null)
            return;

        foreach (var cardId in new[] { engine.DriverCardId, engine.CoDriverCardId }
                     .Concat(engine.RemovedCardIds)
                     .Where(x => x is not null)
                     .Cast<string>()
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (_highWaterMarks.TryGetValue(cardId, out var current) &&
                frame.GameTime.TotalMinutes <= current)
                continue;
            _highWaterMarks[cardId] = await retention.ObserveGameTimeAsync(
                cardId,
                frame.GameTime,
                cancellationToken);
        }
    }

    public Task<IReadOnlyList<ActivityRecord>> LoadDriverHistoryAsync(
        string cardId,
        GameTime? from = null,
        GameTime? toExclusive = null,
        CancellationToken cancellationToken = default) =>
        activities.LoadDriverHistoryAsync(cardId, from, toExclusive, cancellationToken);

    public async Task RefreshRestAllocationDecisionsAsync(
        string cardId,
        CancellationToken cancellationToken = default)
    {
        if (restAllocations is null)
            return;
        engine.SetRestAllocationDecisions(
            cardId,
            await restAllocations.LoadDriverDecisionsAsync(cardId, cancellationToken));
    }

    private Task SaveSnapshotsAsync(
        IReadOnlyList<(string CardId, TachographSnapshot Snapshot)> cardSnapshots,
        CancellationToken cancellationToken)
    {
        var writes = new List<ActivitySessionWrite>();
        foreach (var (cardId, snapshot) in cardSnapshots)
        {
            writes.AddRange(snapshot.CompletedBatches.Select(batch => new ActivitySessionWrite(
                cardId,
                batch.SessionIndex,
                batch.SessionStartedAt,
                batch.Records)));
            writes.AddRange(snapshot.CreatedGapBatches.Select(batch => new ActivitySessionWrite(
                cardId,
                batch.SessionIndex,
                batch.SessionStartedAt,
                [],
                batch.Gaps,
                batch.RemovedGapIds)));
            writes.AddRange(snapshot.OpenedSessions.Select(session => new ActivitySessionWrite(
                cardId,
                session.SessionIndex,
                session.StartedAt,
                [])));
        }

        return activities.ApplySessionWritesAsync(writes, cancellationToken);
    }
}

using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Telemetry;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.Engine;

namespace ETS2Tachograph.Application.Services;

public sealed class TachographService(
    string driverCardId,
    ITachographEngine engine,
    IActivityRepository activities)
{
    public TachographSnapshot Current => engine.Current;

    public async Task<TachographSnapshot> ProcessFrameAsync(
        TelemetryFrame frame,
        CancellationToken cancellationToken = default)
    {
        var snapshot = engine.ProcessFrame(frame);
        await SaveSnapshotAsync(snapshot, cancellationToken);
        return snapshot;
    }

    public async Task SaveSnapshotAsync(
        TachographSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        var writes = snapshot.CompletedBatches.Select(batch => new ActivitySessionWrite(
                driverCardId,
                batch.SessionIndex,
                batch.SessionStartedAt,
                batch.Records))
            .Concat(snapshot.CreatedGapBatches.Select(batch => new ActivitySessionWrite(
                driverCardId,
                batch.SessionIndex,
                batch.SessionStartedAt,
                [],
                batch.Gaps)))
            .Concat(snapshot.OpenedSessions.Select(session => new ActivitySessionWrite(
                driverCardId,
                session.SessionIndex,
                session.StartedAt,
                [])))
            .ToList();
        await activities.ApplySessionWritesAsync(writes, cancellationToken);
    }

    public Task<IReadOnlyList<ActivityRecord>> LoadDriverHistoryAsync(
        GameTime? from = null,
        GameTime? toExclusive = null,
        CancellationToken cancellationToken = default) =>
        activities.LoadDriverHistoryAsync(driverCardId, from, toExclusive, cancellationToken);
}

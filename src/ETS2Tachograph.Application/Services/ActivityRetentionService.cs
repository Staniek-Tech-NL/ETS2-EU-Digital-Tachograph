using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Application.Services;

public sealed class ActivityRetentionService(IActivityRetentionRepository repository)
{
    public Task<ActivityRetentionResult> ArchiveCardAsync(
        string driverCardId,
        CancellationToken cancellationToken = default) =>
        repository.ArchiveWarmAsync(driverCardId, cancellationToken);

    public Task<long> ObserveGameTimeAsync(
        string driverCardId,
        GameTime gameTime,
        CancellationToken cancellationToken = default) =>
        repository.ObserveGameTimeAsync(driverCardId, gameTime, cancellationToken);

    public Task<long?> GetHighWaterMarkAsync(
        string driverCardId,
        CancellationToken cancellationToken = default) =>
        repository.GetHighWaterMarkAsync(driverCardId, cancellationToken);
}

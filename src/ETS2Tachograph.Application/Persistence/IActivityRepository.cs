using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Application.Persistence;

public sealed record StoredActivitySession(
    int SessionIndex,
    GameTime StartedAt,
    IReadOnlyList<ActivityRecord> Records,
    IReadOnlyList<ActivityGap>? Gaps = null);

/// <summary>
/// A session boundary and the records to append to that session. An empty record
/// list is meaningful: it persists a newly opened clock branch.
/// </summary>
public sealed record ActivitySessionWrite(
    string DriverCardId,
    int SessionIndex,
    GameTime StartedAt,
    IReadOnlyList<ActivityRecord> Records,
    IReadOnlyList<ActivityGap>? Gaps = null,
    IReadOnlyList<Guid>? RemovedGapIds = null);

public interface IActivityPersistenceDiagnostics
{
    void RecordConflict(
        string driverCardId,
        int sessionIndex,
        ActivityRecord existing,
        ActivityRecord incoming);

    void RecordWarmProjectionInvalidated(
        string driverCardId,
        long branchAnchorGameMinute,
        long warmThresholdGameMinute,
        int removedWarmBlocks,
        int restoredRawRecords)
    {
    }

    void RecordCanonicalProjectionFallback(
        string driverCardId,
        ActivityRecord previous,
        ActivityRecord current)
    {
    }
}

public interface IActivityRepository
{
    Task EnsureSessionAsync(
        string driverCardId,
        int sessionIndex,
        GameTime startedAt,
        CancellationToken cancellationToken = default);

    Task AppendAsync(
        string driverCardId,
        int sessionIndex,
        IReadOnlyList<ActivityRecord> records,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies all session boundaries and record batches in one transaction.
    /// This is used for a shared game-time boundary affecting both card slots.
    /// </summary>
    Task ApplySessionWritesAsync(
        IReadOnlyList<ActivitySessionWrite> writes,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ActivityRecord>> LoadDriverHistoryAsync(
        string driverCardId,
        GameTime? from = null,
        GameTime? toExclusive = null,
        CancellationToken cancellationToken = default);

    /// <summary>Loads canonical minute-level source data, including archived minutes.</summary>
    Task<IReadOnlyList<ActivityRecord>> LoadRawDriverHistoryAsync(
        string driverCardId,
        GameTime? from = null,
        GameTime? toExclusive = null,
        CancellationToken cancellationToken = default) =>
        LoadDriverHistoryAsync(driverCardId, from, toExclusive, cancellationToken);

    Task<IReadOnlyList<ActivityGap>> LoadDriverGapsAsync(
        string driverCardId,
        GameTime? from = null,
        GameTime? toExclusive = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ActivityGap>>([]);

    /// <summary>
    /// Loads unresolved gaps from the canonical truncate-and-append projection.
    /// A null card id searches all cards; abandoned source branches are excluded.
    /// </summary>
    async Task<IReadOnlyList<ActivityGap>> GetUnresolvedGapsAsync(
        string? driverCardId = null,
        GameTime? fromGameMinute = null,
        GameTime? toGameMinute = null,
        CancellationToken cancellationToken = default)
    {
        if (driverCardId is null)
            return [];

        var gaps = await LoadDriverGapsAsync(
            driverCardId,
            fromGameMinute,
            toGameMinute,
            cancellationToken);
        return gaps.Where(gap => gap.State == Core.Enums.ActivityGapState.Unresolved).ToList();
    }

    /// <summary>
    /// Loads gaps from the canonical truncate-and-append projection. When resolved
    /// gaps are not requested, the result is equivalent to GetUnresolvedGapsAsync.
    /// </summary>
    async Task<IReadOnlyList<ActivityGap>> GetCanonicalGapsAsync(
        string? driverCardId,
        GameTime? fromGameMinute,
        GameTime? toGameMinute,
        bool includeResolved,
        CancellationToken cancellationToken = default)
    {
        if (!includeResolved)
            return await GetUnresolvedGapsAsync(
                driverCardId,
                fromGameMinute,
                toGameMinute,
                cancellationToken);

        if (driverCardId is null)
            return [];

        return await LoadDriverGapsAsync(
            driverCardId,
            fromGameMinute,
            toGameMinute,
            cancellationToken);
    }

    /// <summary>Loads only the hot sessions used to restore the rule engine.</summary>
    Task<IReadOnlyList<StoredActivitySession>> LoadSessionsAsync(
        string driverCardId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads sessions for rule-engine restoration, including the canonical warm
    /// projection required to rebuild historical regulatory obligations.
    /// </summary>
    Task<IReadOnlyList<StoredActivitySession>> LoadRestorationSessionsAsync(
        string driverCardId,
        CancellationToken cancellationToken = default) =>
        LoadSessionsAsync(driverCardId, cancellationToken);

    /// <summary>Loads all source sessions for lossless export and diagnostics.</summary>
    Task<IReadOnlyList<StoredActivitySession>> LoadRawSessionsAsync(
        string driverCardId,
        CancellationToken cancellationToken = default) =>
        LoadSessionsAsync(driverCardId, cancellationToken);
}

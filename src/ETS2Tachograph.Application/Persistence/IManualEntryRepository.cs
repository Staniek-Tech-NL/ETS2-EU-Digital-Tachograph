using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Application.Persistence;

public sealed record ManualEntryGapContext(
    ActivityGap Gap,
    bool IsCanonical,
    bool ProjectionMatchesSource,
    IReadOnlyList<ActivityRecord> CanonicalRecords,
    IReadOnlyList<ActivityRecord> ExistingResolutionRecords);

public sealed record ManualEntryResolutionWrite(
    Guid GapId,
    GameTime ResolvedAt,
    IReadOnlyList<ActivityRecord> Segments);

public enum ManualEntryPersistenceStatus
{
    Applied = 0,
    AlreadyApplied = 1,
    Conflict = 2
}

public sealed record ManualEntryPersistenceResult(
    ManualEntryPersistenceStatus Status,
    ActivityGap Gap,
    IReadOnlyList<ActivityRecord> Segments);

public interface IManualEntryRepository
{
    Task<ManualEntryGapContext?> LoadGapContextAsync(
        Guid gapId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically inserts every manual segment and marks its source gap resolved.
    /// Implementations must recheck state and idempotency inside the transaction.
    /// </summary>
    Task<ManualEntryPersistenceResult> ApplyGapResolutionAsync(
        ManualEntryResolutionWrite write,
        CancellationToken cancellationToken = default);
}

public interface IManualEntryDiagnostics
{
    void RecordResolutionConflict(
        Guid gapId,
        IReadOnlyList<ActivityRecord> existing,
        IReadOnlyList<ActivityRecord> incoming);
}

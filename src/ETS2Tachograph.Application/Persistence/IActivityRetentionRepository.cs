using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Application.Persistence;

public static class ActivityRetentionPolicy
{
    public const int GameMinutesPerDay = 1_440;
    public const int HotGameDays = 14;
    public const int ColdGameDays = 365;
    public const long HotWindowMinutes = HotGameDays * GameMinutesPerDay;
    public const long ColdWindowMinutes = ColdGameDays * GameMinutesPerDay;
}

public sealed record ActivityRetentionResult(
    string DriverCardId,
    long HighWaterMarkGameMinute,
    long WarmThresholdGameMinute,
    long ColdThresholdGameMinute,
    long WarmMinutes,
    int WarmBlockCount);

/// <summary>
/// Maintains game-time retention projections. The cold threshold is exposed as an
/// architectural hook, but no cold-tier archiver is implemented yet.
/// </summary>
public interface IActivityRetentionRepository
{
    Task<ActivityRetentionResult> ArchiveWarmAsync(
        string driverCardId,
        CancellationToken cancellationToken = default);

    Task<long> ObserveGameTimeAsync(
        string driverCardId,
        GameTime gameTime,
        CancellationToken cancellationToken = default);

    Task<long?> GetHighWaterMarkAsync(
        string driverCardId,
        CancellationToken cancellationToken = default);
}

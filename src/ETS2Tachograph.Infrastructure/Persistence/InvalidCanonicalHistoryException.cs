using ETS2Tachograph.Core.Entities;

namespace ETS2Tachograph.Infrastructure.Persistence;

/// <summary>
/// Raised when the canonical projection hands overlapping records to a consumer that
/// requires a single unambiguous timeline. The unique index on the warm blocks is the
/// last guard; this exception fails earlier, where the driver history is still readable.
/// </summary>
public sealed class InvalidCanonicalHistoryException(
    string driverCardId,
    ActivityRecord previous,
    ActivityRecord current) : Exception(
        $"Canonical records overlap for card {driverCardId}: " +
        $"{previous.Start.TotalMinutes}-{previous.EndExclusive.TotalMinutes} " +
        $"({previous.Activity}, {previous.Source}) and " +
        $"{current.Start.TotalMinutes}-{current.EndExclusive.TotalMinutes} " +
        $"({current.Activity}, {current.Source}).")
{
    public string DriverCardId { get; } = driverCardId;
    public ActivityRecord Previous { get; } = previous;
    public ActivityRecord Current { get; } = current;
}

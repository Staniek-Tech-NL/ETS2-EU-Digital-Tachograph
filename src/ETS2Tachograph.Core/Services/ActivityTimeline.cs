using ETS2Tachograph.Core.Entities;

namespace ETS2Tachograph.Core.Services;

/// <summary>In-memory append-only timeline with ordering and overlap protection.</summary>
public sealed class ActivityTimeline
{
    private readonly List<ActivityRecord> _records = [];

    public IReadOnlyList<ActivityRecord> Records => _records.AsReadOnly();

    public void Append(ActivityRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (string.IsNullOrWhiteSpace(record.DriverCardId))
        {
            throw new ArgumentException("Driver card id is required.", nameof(record));
        }

        if (record.EndExclusive <= record.Start)
        {
            throw new ArgumentException("An activity must have a positive duration.", nameof(record));
        }

        if (_records.Count > 0 && record.Start < _records[^1].EndExclusive)
        {
            throw new InvalidOperationException("Activity history cannot overlap or move backwards.");
        }

        _records.Add(record);
    }
}

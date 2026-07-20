using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Telemetry;

namespace ETS2Tachograph.Engine;

/// <summary>Single public contract for processing telemetry and controlling tachograph modes.</summary>
public interface ITachographEngine
{
    TachographSnapshot Current { get; }
    event EventHandler<TachographSnapshot>? SnapshotChanged;

    TachographSnapshot ProcessFrame(TelemetryFrame frame);
    TachographSnapshot Flush(DateTimeOffset recordedAtUtc);
    void RestoreSessions(IEnumerable<IReadOnlyList<ActivityRecord>> sessions);
    void SetManualActivity(DriverActivity activity);
    void SetOutMode(bool enabled);
    void SetFerryMode(bool enabled);
    void SetMultiManning(bool enabled);
}

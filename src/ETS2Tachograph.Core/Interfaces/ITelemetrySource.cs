using ETS2Tachograph.Core.Telemetry;

namespace ETS2Tachograph.Core.Interfaces;

public interface ITelemetrySource
{
    IAsyncEnumerable<TelemetryFrame> ReadFramesAsync(CancellationToken cancellationToken = default);
}

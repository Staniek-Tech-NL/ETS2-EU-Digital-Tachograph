using ETS2Tachograph.Core.Interfaces;

namespace ETS2Tachograph.Engine;

/// <summary>Central asynchronous pump from any telemetry source into the tachograph engine.</summary>
public sealed class TelemetryProcessor(ITelemetrySource source, ITachographEngine engine)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await foreach (var frame in source.ReadFramesAsync(cancellationToken)
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            engine.ProcessFrame(frame);
        }
    }
}

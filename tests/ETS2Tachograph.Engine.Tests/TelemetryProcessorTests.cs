using System.Runtime.CompilerServices;
using ETS2Tachograph.Core.Interfaces;
using ETS2Tachograph.Core.Telemetry;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Engine.Tests;

public sealed class TelemetryProcessorTests
{
    [Fact]
    public async Task Central_processor_consumes_generic_telemetry_source()
    {
        var engine = new TachographEngine("PL-TEST");
        var source = new FakeSource(
        [
            Frame(0, 20),
            Frame(1, 20),
            Frame(2, 20)
        ]);
        var processor = new TelemetryProcessor(source, engine);

        await processor.RunAsync();

        Assert.NotNull(engine.Current.LastClosedRecord);
        Assert.NotNull(engine.Current.Regulation);
    }

    private static TelemetryFrame Frame(long minute, double speed) =>
        new(new GameTime(minute), DateTimeOffset.UtcNow.AddSeconds(minute), speed, false);

    private sealed class FakeSource(IReadOnlyList<TelemetryFrame> frames) : ITelemetrySource
    {
        public async IAsyncEnumerable<TelemetryFrame> ReadFramesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var frame in frames)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return frame;
                await Task.Yield();
            }
        }
    }
}

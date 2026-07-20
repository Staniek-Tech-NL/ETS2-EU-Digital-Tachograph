namespace ETS2Tachograph.Telemetry.Scs.Tests;

public sealed class ScsTelemetrySourceTests
{
    [Fact]
    public async Task Source_converts_units_and_pause_state()
    {
        using var source = new ScsTelemetrySource(
            new QueueReader(
            [
                new ScsTelemetrySnapshot(2, true, 500, 10, 4, 8),
                new ScsTelemetrySnapshot(4, false, 501, -2, 5, 9)
            ]),
            TimeSpan.FromMilliseconds(1));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await using var frames = source.ReadFramesAsync(timeout.Token).GetAsyncEnumerator();

        Assert.True(await frames.MoveNextAsync());
        Assert.Equal(500, frames.Current.GameTime.TotalMinutes);
        Assert.Equal(36, frames.Current.SpeedKph, precision: 5);
        Assert.False(frames.Current.GamePaused);
        Assert.Equal(4U, frames.Current.WorldGeneration);
        Assert.Equal(8U, frames.Current.CargoOperationGeneration);

        Assert.True(await frames.MoveNextAsync());
        Assert.Equal(7.2, frames.Current.SpeedKph, precision: 5);
        Assert.True(frames.Current.GamePaused);
        Assert.Equal(5U, frames.Current.WorldGeneration);
    }

    [Fact]
    public async Task Duplicate_sequence_is_not_emitted_twice()
    {
        using var source = new ScsTelemetrySource(
            new QueueReader(
            [
                new ScsTelemetrySnapshot(2, true, 1, 0, 1),
                new ScsTelemetrySnapshot(2, true, 1, 0, 1),
                new ScsTelemetrySnapshot(4, true, 2, 0, 1)
            ]),
            TimeSpan.FromMilliseconds(1));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await using var frames = source.ReadFramesAsync(timeout.Token).GetAsyncEnumerator();

        Assert.True(await frames.MoveNextAsync());
        Assert.Equal(1, frames.Current.GameTime.TotalMinutes);
        Assert.True(await frames.MoveNextAsync());
        Assert.Equal(2, frames.Current.GameTime.TotalMinutes);
    }

    private sealed class QueueReader(IEnumerable<ScsTelemetrySnapshot> snapshots) : IScsTelemetryReader
    {
        private readonly Queue<ScsTelemetrySnapshot> _snapshots = new(snapshots);

        public bool TryRead(out ScsTelemetrySnapshot snapshot)
        {
            if (_snapshots.TryDequeue(out snapshot))
            {
                return true;
            }

            snapshot = default;
            return false;
        }

        public void Dispose()
        {
        }
    }
}

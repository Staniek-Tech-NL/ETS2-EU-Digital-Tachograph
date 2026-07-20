using System.Runtime.CompilerServices;
using ETS2Tachograph.Core.Interfaces;
using ETS2Tachograph.Core.Telemetry;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Telemetry.Scs;

public sealed class ScsTelemetrySource : ITelemetrySource, IDisposable
{
    private readonly IScsTelemetryReader _reader;
    private readonly TimeSpan _pollInterval;
    private bool _disposed;

    public ScsTelemetrySource(
        IScsTelemetryReader? reader = null,
        TimeSpan? pollInterval = null)
    {
        _reader = reader ?? new ScsMemoryMappedTelemetryReader();
        _pollInterval = pollInterval ?? TimeSpan.FromMilliseconds(100);
        if (_pollInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(pollInterval));
        }
    }

    public async IAsyncEnumerable<TelemetryFrame> ReadFramesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        using var timer = new PeriodicTimer(_pollInterval);
        uint? lastSequence = null;

        do
        {
            if (_reader.TryRead(out var snapshot) && snapshot.Sequence != lastSequence)
            {
                lastSequence = snapshot.Sequence;
                yield return new TelemetryFrame(
                    new GameTime(snapshot.GameTimeMinutes),
                    DateTimeOffset.UtcNow,
                    Math.Abs(snapshot.SpeedMetersPerSecond) * 3.6,
                    GamePaused: !snapshot.Running,
                    WorldGeneration: snapshot.WorldGeneration,
                    CargoOperationGeneration: snapshot.CargoOperationGeneration);
            }
        }
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _reader.Dispose();
        _disposed = true;
    }
}

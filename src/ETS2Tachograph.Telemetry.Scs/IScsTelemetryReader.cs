namespace ETS2Tachograph.Telemetry.Scs;

public interface IScsTelemetryReader : IDisposable
{
    bool TryRead(out ScsTelemetrySnapshot snapshot);
}

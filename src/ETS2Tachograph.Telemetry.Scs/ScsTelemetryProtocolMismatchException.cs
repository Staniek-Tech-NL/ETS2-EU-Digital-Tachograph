namespace ETS2Tachograph.Telemetry.Scs;

public sealed class ScsTelemetryProtocolMismatchException(
    ushort actualVersion,
    ushort actualSize)
    : InvalidOperationException(
        $"Niezgodna wersja pluginu SCS: wykryto protokół v{actualVersion} " +
        $"(rozmiar {actualSize}), a aplikacja oczekuje v{ScsTelemetryProtocol.Version} " +
        $"(rozmiar {ScsTelemetryProtocol.Size}). Zainstaluj właściwy plik " +
        "ETS2Tachograph.ScsPlugin.dll.")
{
    public ushort ActualVersion { get; } = actualVersion;
    public ushort ActualSize { get; } = actualSize;
}

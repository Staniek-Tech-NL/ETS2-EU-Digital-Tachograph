using System.IO.MemoryMappedFiles;
using System.Runtime.Versioning;

namespace ETS2Tachograph.Telemetry.Scs;

public sealed class ScsMemoryMappedTelemetryReader : IScsTelemetryReader
{
    private readonly string _mappingName;
    private readonly string? _legacyMappingName;
    private MemoryMappedFile? _mapping;
    private MemoryMappedViewAccessor? _view;
    private bool _disposed;

    public ScsMemoryMappedTelemetryReader(string? mappingName = null)
    {
        _mappingName = mappingName ?? ScsTelemetryProtocol.MappingName;
        _legacyMappingName = mappingName is null ? ScsTelemetryProtocol.LegacyMappingName : null;
        if (string.IsNullOrWhiteSpace(_mappingName))
        {
            throw new ArgumentException("Mapping name is required.", nameof(mappingName));
        }
    }

    public bool TryRead(out ScsTelemetrySnapshot snapshot)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        snapshot = default;

        if (!OperatingSystem.IsWindows() || !EnsureOpen())
        {
            return false;
        }

        if (_view!.Capacity < 8)
        {
            return false;
        }

        var header = new byte[8];
        _view.ReadArray(0, header, 0, header.Length);
        ScsTelemetryProtocol.ThrowIfIncompatible(header);
        if (_view.Capacity < ScsTelemetryProtocol.Size)
        {
            return false;
        }

        var sequenceBefore = _view.ReadUInt32(8);
        if ((sequenceBefore & 1) != 0)
        {
            return false;
        }

        var buffer = new byte[ScsTelemetryProtocol.Size];
        _view.ReadArray(0, buffer, 0, buffer.Length);
        Thread.MemoryBarrier();
        var sequenceAfter = _view.ReadUInt32(8);

        if (sequenceBefore == sequenceAfter)
        {
            ScsTelemetryProtocol.ThrowIfIncompatible(buffer);
        }

        return sequenceBefore == sequenceAfter &&
            ScsTelemetryProtocol.TryDecode(buffer, out snapshot) &&
            snapshot.Sequence == sequenceAfter;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _view?.Dispose();
        _mapping?.Dispose();
        _disposed = true;
    }

    [SupportedOSPlatform("windows")]
    private bool EnsureOpen()
    {
        if (_view is not null)
        {
            return true;
        }

        try
        {
            _mapping = OpenMapping(_mappingName) ??
                (_legacyMappingName is null ? null : OpenMapping(_legacyMappingName));
            if (_mapping is null)
                return false;
            _view = _mapping.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            return true;
        }
        catch (FileNotFoundException)
        {
            _mapping?.Dispose();
            _mapping = null;
            return false;
        }
    }

    [SupportedOSPlatform("windows")]
    private static MemoryMappedFile? OpenMapping(string name)
    {
        try
        {
            return MemoryMappedFile.OpenExisting(name, MemoryMappedFileRights.Read);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }
}

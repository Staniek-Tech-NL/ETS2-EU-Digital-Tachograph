using System.Buffers.Binary;
using System.IO.MemoryMappedFiles;

namespace ETS2Tachograph.Telemetry.Scs.Tests;

public sealed class ScsTelemetryProtocolTests
{
    [Fact]
    public void Stable_version_three_frame_is_decoded()
    {
        var data = Frame(
            sequence: 10,
            running: true,
            gameTime: 12_345,
            speed: 20.5F,
            worldGeneration: 7,
            cargoOperationGeneration: 3);

        Assert.True(ScsTelemetryProtocol.TryDecode(data, out var snapshot));
        Assert.Equal(10U, snapshot.Sequence);
        Assert.True(snapshot.Running);
        Assert.Equal(12_345U, snapshot.GameTimeMinutes);
        Assert.Equal(20.5F, snapshot.SpeedMetersPerSecond);
        Assert.Equal(7U, snapshot.WorldGeneration);
        Assert.Equal(3U, snapshot.CargoOperationGeneration);
    }

    [Fact]
    public void Odd_sequence_is_rejected_as_write_in_progress()
    {
        Assert.False(ScsTelemetryProtocol.TryDecode(
            Frame(sequence: 11, running: true, gameTime: 1, speed: 1),
            out _));
    }

    [Theory]
    [InlineData(0x00000000U, (ushort)1)]
    [InlineData(ScsTelemetryProtocol.Magic, (ushort)1)]
    public void Unknown_magic_or_version_is_rejected(uint magic, ushort version)
    {
        var data = Frame(sequence: 2, running: true, gameTime: 1, speed: 1);
        BinaryPrimitives.WriteUInt32LittleEndian(data, magic);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(4), version);

        Assert.False(ScsTelemetryProtocol.TryDecode(data, out _));
    }

    [Fact]
    public void Known_protocol_with_wrong_version_produces_clear_diagnostic()
    {
        var data = Frame(sequence: 2, running: true, gameTime: 1, speed: 1);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(4), 1);

        var exception = Assert.Throws<ScsTelemetryProtocolMismatchException>(
            () => ScsTelemetryProtocol.ThrowIfIncompatible(data));

        Assert.Equal((ushort)1, exception.ActualVersion);
        Assert.Contains("oczekuje v3", exception.Message);
        Assert.Contains("ETS2Tachograph.ScsPlugin.dll", exception.Message);
    }

    [Fact]
    public void Legacy_28_byte_mapping_produces_protocol_mismatch_instead_of_size_error()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var mappingName = $"Local\\ETS2Tachograph.LegacyTest.{Guid.NewGuid():N}";
        using var mapping = MemoryMappedFile.CreateNew(mappingName, 28);
        using var view = mapping.CreateViewAccessor(0, 28);
        var legacy = new byte[28];
        BinaryPrimitives.WriteUInt32LittleEndian(legacy, ScsTelemetryProtocol.Magic);
        BinaryPrimitives.WriteUInt16LittleEndian(legacy.AsSpan(4), 2);
        BinaryPrimitives.WriteUInt16LittleEndian(legacy.AsSpan(6), 28);
        BinaryPrimitives.WriteUInt32LittleEndian(legacy.AsSpan(8), 2);
        view.WriteArray(0, legacy, 0, legacy.Length);
        view.Flush();
        using var reader = new ScsMemoryMappedTelemetryReader(mappingName);

        var exception = Assert.Throws<ScsTelemetryProtocolMismatchException>(() => reader.TryRead(out _));

        Assert.Equal((ushort)2, exception.ActualVersion);
        Assert.Equal((ushort)28, exception.ActualSize);
    }

    private static byte[] Frame(
        uint sequence,
        bool running,
        uint gameTime,
        float speed,
        uint worldGeneration = 0,
        uint cargoOperationGeneration = 0)
    {
        var data = new byte[ScsTelemetryProtocol.Size];
        BinaryPrimitives.WriteUInt32LittleEndian(data, ScsTelemetryProtocol.Magic);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(4), ScsTelemetryProtocol.Version);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(6), ScsTelemetryProtocol.Size);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(8), sequence);
        data[12] = running ? (byte)1 : (byte)0;
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(16), gameTime);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(20), BitConverter.SingleToInt32Bits(speed));
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(24), worldGeneration);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(28), cargoOperationGeneration);
        return data;
    }
}

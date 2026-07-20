using System.Buffers.Binary;

namespace ETS2Tachograph.Telemetry.Scs;

public static class ScsTelemetryProtocol
{
    public const string MappingName = "Local\\ETS2Tachograph.Telemetry.v3";
    public const string LegacyMappingName = "Local\\ETS2Tachograph.Telemetry.v2";
    public const uint Magic = 0x32535445;
    public const ushort Version = 3;
    public const ushort Size = 32;

    public static void ThrowIfIncompatible(ReadOnlySpan<byte> data)
    {
        if (data.Length < 8 ||
            BinaryPrimitives.ReadUInt32LittleEndian(data) != Magic)
        {
            return;
        }

        var actualVersion = BinaryPrimitives.ReadUInt16LittleEndian(data[4..]);
        var actualSize = BinaryPrimitives.ReadUInt16LittleEndian(data[6..]);
        if (actualVersion != Version || actualSize != Size)
        {
            throw new ScsTelemetryProtocolMismatchException(actualVersion, actualSize);
        }
    }

    public static bool TryDecode(ReadOnlySpan<byte> data, out ScsTelemetrySnapshot snapshot)
    {
        snapshot = default;
        if (data.Length < Size ||
            BinaryPrimitives.ReadUInt32LittleEndian(data) != Magic ||
            BinaryPrimitives.ReadUInt16LittleEndian(data[4..]) != Version ||
            BinaryPrimitives.ReadUInt16LittleEndian(data[6..]) != Size)
        {
            return false;
        }

        var sequence = BinaryPrimitives.ReadUInt32LittleEndian(data[8..]);
        if ((sequence & 1) != 0)
        {
            return false;
        }

        snapshot = new ScsTelemetrySnapshot(
            Sequence: sequence,
            Running: data[12] != 0,
            GameTimeMinutes: BinaryPrimitives.ReadUInt32LittleEndian(data[16..]),
            SpeedMetersPerSecond: BitConverter.Int32BitsToSingle(
                BinaryPrimitives.ReadInt32LittleEndian(data[20..])),
            WorldGeneration: BinaryPrimitives.ReadUInt32LittleEndian(data[24..]),
            CargoOperationGeneration: BinaryPrimitives.ReadUInt32LittleEndian(data[28..]));
        return float.IsFinite(snapshot.SpeedMetersPerSecond);
    }
}

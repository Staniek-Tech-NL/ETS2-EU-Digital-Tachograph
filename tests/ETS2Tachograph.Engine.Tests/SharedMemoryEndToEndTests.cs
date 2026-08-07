using System.Buffers.Binary;
using System.IO.MemoryMappedFiles;
using ETS2Tachograph.Telemetry.Scs;

namespace ETS2Tachograph.Engine.Tests;

public sealed class SharedMemoryEndToEndTests
{
    [Fact]
    public async Task Real_shared_memory_flows_to_history_and_rules()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var mappingName = $"Local\\ETS2Tachograph.Test.{Guid.NewGuid():N}";
        using var mapping = MemoryMappedFile.CreateNew(mappingName, ScsTelemetryProtocol.Size);
        using var view = mapping.CreateViewAccessor(0, ScsTelemetryProtocol.Size);
        using var reader = new ScsMemoryMappedTelemetryReader(mappingName);
        using var source = new ScsTelemetrySource(reader, TimeSpan.FromMilliseconds(2));
        var engine = new TachographEngine("PL-E2E");
        var processor = new TelemetryProcessor(source, engine);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var processing = processor.RunAsync(cancellation.Token);
        Write(view, sequence: 2, gameMinute: 100, speedMps: 20, worldGeneration: 3);
        await WaitUntilAsync(
            () => engine.Current.GameTime?.TotalMinutes == 100,
            cancellation.Token);
        Write(view, sequence: 4, gameMinute: 101, speedMps: 20, worldGeneration: 3);
        await WaitUntilAsync(
            () => engine.Current.GameTime?.TotalMinutes == 101,
            cancellation.Token);
        Write(view, sequence: 6, gameMinute: 102, speedMps: 20, worldGeneration: 3);

        await WaitUntilAsync(
            () => engine.Current.GameTime?.TotalMinutes == 102 &&
                engine.Current.LastClosedRecord is not null,
            cancellation.Token);
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await processing);

        Assert.Equal(100, engine.Current.LastClosedRecord!.Start.TotalMinutes);
        Assert.NotNull(engine.Current.Regulation);
    }

    private static void Write(
        MemoryMappedViewAccessor view,
        uint sequence,
        uint gameMinute,
        float speedMps,
        uint worldGeneration)
    {
        var data = new byte[ScsTelemetryProtocol.Size];
        BinaryPrimitives.WriteUInt32LittleEndian(data, ScsTelemetryProtocol.Magic);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(4), ScsTelemetryProtocol.Version);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(6), ScsTelemetryProtocol.Size);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(8), sequence);
        data[12] = 1;
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(16), gameMinute);
        BinaryPrimitives.WriteInt32LittleEndian(
            data.AsSpan(20),
            BitConverter.SingleToInt32Bits(speedMps));
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(24), worldGeneration);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(28), 0);
        view.WriteArray(0, data, 0, data.Length);
        view.Flush();
    }

    private static async Task WaitUntilAsync(Func<bool> predicate, CancellationToken cancellationToken)
    {
        while (!predicate())
        {
            await Task.Delay(10, cancellationToken);
        }
    }
}

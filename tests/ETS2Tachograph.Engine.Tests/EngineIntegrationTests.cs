using System.Runtime.CompilerServices;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Interfaces;
using ETS2Tachograph.Core.Telemetry;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.RuleEngine;

namespace ETS2Tachograph.Engine.Tests;

public sealed class EngineIntegrationTests
{
    [Fact]
    public async Task Multi_manning_flows_through_processor_and_uses_30_hour_window()
    {
        var engine = new TachographEngine("PL-CREW");
        engine.SetMultiManning(true);
        var processor = new TelemetryProcessor(
            new FakeSource([Frame(0), Frame(1_441)]),
            engine);

        await processor.RunAsync();

        Assert.True(engine.Current.MultiManningEnabled);
        Assert.Equal(1_800, engine.Current.DailyRestWindowMinutes);
        Assert.Equal(359, engine.Current.Regulation!.State.MinutesUntilDailyRestDeadline);
        Assert.DoesNotContain(
            engine.Current.Regulation.Violations,
            violation => violation.Type == ViolationType.DailyRestMissing);
    }

    [Fact]
    public async Task Ferry_mode_flows_through_processor_into_history_and_snapshot()
    {
        var engine = new TachographEngine("PL-FERRY");
        engine.SetManualActivity(DriverActivity.BreakOrRest);
        engine.SetFerryMode(true);
        var processor = new TelemetryProcessor(
            new FakeSource([Frame(0), Frame(1), Frame(2)]),
            engine);

        await processor.RunAsync();

        Assert.True(engine.Current.FerryModeEnabled);
        Assert.NotEmpty(engine.Current.CompletedRecords);
        Assert.All(
            engine.Current.CompletedRecords,
            record => Assert.Equal(SpecialCondition.FerryCrossing, record.Condition));
        Assert.Equal(
            SpecialCondition.FerryCrossing,
            engine.Current.LastClosedRecord!.Condition);
    }

    private static TelemetryFrame Frame(long minute) =>
        new(
            new GameTime(minute),
            new DateTimeOffset(2026, 7, 14, 12, 0, 0, TimeSpan.Zero).AddSeconds(minute),
            SpeedKph: 0,
            GamePaused: false);

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

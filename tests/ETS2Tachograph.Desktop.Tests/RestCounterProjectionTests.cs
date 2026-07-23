using ETS2Tachograph.Engine;
using ETS2Tachograph.RuleEngine;

namespace ETS2Tachograph.Desktop.Tests;

public sealed class RestCounterProjectionTests
{
    [Theory]
    [InlineData(44, "00:44", "00:01", 44d / 45d * 100d, "W TRAKCIE")]
    [InlineData(45, "00:45", "00:00", 100d, "ZALICZONA")]
    public void Rest_counter_uses_current_continuous_break_from_regulation_state(
        long elapsedMinutes,
        string expectedElapsed,
        string expectedRemaining,
        double expectedProgressPercent,
        string expectedStatus)
    {
        var snapshot = new TachographSnapshot
        {
            Regulation = new RegulationEvaluation(
                new RegulationState
                {
                    CurrentContinuousBreakMinutes = elapsedMinutes
                },
                [],
                [])
        };

        var projection = MainViewModel.ProjectRestCounter(snapshot, 45);

        Assert.Equal(expectedElapsed, projection.Elapsed);
        Assert.Equal(expectedRemaining, projection.Remaining);
        Assert.Equal(expectedProgressPercent, projection.ProgressPercent, precision: 10);
        Assert.Equal(expectedStatus, projection.Status);
    }
}

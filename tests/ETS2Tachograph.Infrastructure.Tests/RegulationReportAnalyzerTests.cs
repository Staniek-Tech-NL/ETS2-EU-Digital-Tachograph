using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.Infrastructure.Persistence;

namespace ETS2Tachograph.Infrastructure.Tests;

public sealed class RegulationReportAnalyzerTests
{
    [Fact]
    public void Analyze_maps_complete_paid_obligation_trace_to_application_dto()
    {
        IReadOnlyList<ActivityRecord> history =
        [
            Record(0, 2_400, DriverActivity.BreakOrRest),
            Record(2_400, 2_401, DriverActivity.OtherWork),
            Record(2_401, 3_241, DriverActivity.BreakOrRest),
            Record(3_241, 3_242, DriverActivity.OtherWork)
        ];

        var result = new RegulationReportAnalyzer().Analyze(new GameTime(3_242), history);

        var obligation = Assert.Single(result.CompensationObligations);
        Assert.Equal(1, obligation.IdentitySchemeVersion);
        Assert.StartsWith("obligation-v1-", obligation.ObligationId);
        Assert.Equal("PL-ANALYZER", obligation.DriverCardId);
        Assert.StartsWith("rest-v1-", obligation.SourceRestBlockId);
        Assert.Equal(2_400, obligation.SourceRestEndGameMinuteExclusive);
        Assert.Equal(300, obligation.OriginalOwedMinutes);
        Assert.Equal(0, obligation.RemainingMinutes);
        Assert.Equal(0, obligation.ReductionWeek);
        Assert.Equal(4 * GameWeek.MinutesPerWeek, obligation.DueAtGameMinuteExclusive);
        Assert.StartsWith("rest-v1-", obligation.PaymentRestBlockId);
        Assert.NotNull(obligation.PaymentRange);
        Assert.Equal(2_941, obligation.PaymentRange.StartGameMinute);
        Assert.Equal(3_241, obligation.PaymentRange.EndGameMinuteExclusive);
        Assert.Equal(300, obligation.PaymentRange.DurationMinutes);
        Assert.Equal(3_241, obligation.SettledAtGameMinute);
        Assert.Equal(WeeklyRestCompensationStatusDto.PaidOnTime, obligation.Status);
        Assert.Equal(0, result.CompensationSummary.TotalOwedMinutes);
        Assert.Equal(0, result.CompensationSummary.Count);
    }

    private static ActivityRecord Record(long start, long end, DriverActivity activity) => new()
    {
        Id = Guid.NewGuid(),
        DriverCardId = "PL-ANALYZER",
        Activity = activity,
        Start = new GameTime(start),
        EndExclusive = new GameTime(end),
        RecordedAtUtc = DateTimeOffset.UtcNow,
        Source = ActivitySource.Telemetry
    };
}

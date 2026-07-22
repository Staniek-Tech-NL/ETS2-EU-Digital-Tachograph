using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.RuleEngine;

namespace ETS2Tachograph.Application.Tests;

public sealed class WeeklyRestCompensationDtoTests
{
    [Fact]
    public void Map_copies_every_obligation_and_payment_trace_field()
    {
        var source = new WeeklyRestCompensation
        {
            IdentitySchemeVersion = 7,
            ObligationId = "obligation-v7-source",
            DriverCardId = "PL-DTO",
            SourceRestBlockId = "rest-v7-source",
            SourceRestEndExclusive = new GameTime(12_345),
            OriginalOwedMinutes = 1_253,
            RemainingMinutes = 0,
            ReductionWeek = new GameWeek(8),
            DueAtExclusive = new GameTime(120_960),
            PaymentRestBlockId = "rest-v7-payment",
            PaymentRange = new CompensationMinuteRange(
                new GameTime(100_000),
                new GameTime(101_253)),
            SettledAt = new GameTime(101_253),
            Status = WeeklyRestCompensationStatus.PaidOnTime
        };

        var result = WeeklyRestCompensationDtoMapper.Map(source);

        Assert.Equal(7, result.IdentitySchemeVersion);
        Assert.Equal("obligation-v7-source", result.ObligationId);
        Assert.Equal("PL-DTO", result.DriverCardId);
        Assert.Equal("rest-v7-source", result.SourceRestBlockId);
        Assert.Equal(12_345, result.SourceRestEndGameMinuteExclusive);
        Assert.Equal(1_253, result.OriginalOwedMinutes);
        Assert.Equal(0, result.RemainingMinutes);
        Assert.Equal(8, result.ReductionWeek);
        Assert.Equal(120_960, result.DueAtGameMinuteExclusive);
        Assert.Equal("rest-v7-payment", result.PaymentRestBlockId);
        Assert.NotNull(result.PaymentRange);
        Assert.Equal(100_000, result.PaymentRange.StartGameMinute);
        Assert.Equal(101_253, result.PaymentRange.EndGameMinuteExclusive);
        Assert.Equal(1_253, result.PaymentRange.DurationMinutes);
        Assert.Equal(101_253, result.SettledAtGameMinute);
        Assert.Equal(WeeklyRestCompensationStatusDto.PaidOnTime, result.Status);
    }

    [Fact]
    public void Map_preserves_null_payment_trace_for_open_obligation()
    {
        var result = WeeklyRestCompensationDtoMapper.Map(Obligation(
            status: WeeklyRestCompensationStatus.OpenOnTime,
            remainingMinutes: 300));

        Assert.Null(result.PaymentRestBlockId);
        Assert.Null(result.PaymentRange);
        Assert.Null(result.SettledAtGameMinute);
        Assert.True(result.IsOpen);
        Assert.False(result.IsOverdue);
    }

    [Theory]
    [InlineData(WeeklyRestCompensationStatus.OpenOnTime, WeeklyRestCompensationStatusDto.OpenOnTime)]
    [InlineData(WeeklyRestCompensationStatus.Overdue, WeeklyRestCompensationStatusDto.Overdue)]
    [InlineData(WeeklyRestCompensationStatus.PaidOnTime, WeeklyRestCompensationStatusDto.PaidOnTime)]
    [InlineData(WeeklyRestCompensationStatus.PaidLate, WeeklyRestCompensationStatusDto.PaidLate)]
    public void Map_maps_every_status(
        WeeklyRestCompensationStatus sourceStatus,
        WeeklyRestCompensationStatusDto expectedStatus)
    {
        var result = WeeklyRestCompensationDtoMapper.Map(Obligation(sourceStatus));

        Assert.Equal(expectedStatus, result.Status);
    }

    [Fact]
    public void Aggregate_compatibility_fields_are_derived_from_full_obligations()
    {
        var obligations = WeeklyRestCompensationDtoMapper.MapAll(
        [
            Obligation(WeeklyRestCompensationStatus.Overdue, 600, reductionWeek: 30),
            Obligation(WeeklyRestCompensationStatus.OpenOnTime, 660, reductionWeek: 31),
            Obligation(WeeklyRestCompensationStatus.PaidLate, 0, reductionWeek: 29)
        ]);
        var analysis = new RegulationReportAnalysisDto([], obligations);
        var report = new ReportDto("PL-DTO", 0, 1, 0, 1, 0, 0, 0, [], [], [])
        {
            CompensationObligations = obligations
        };

        Assert.Equal(1_260, analysis.CompensationSummary.TotalOwedMinutes);
        Assert.Equal(2, analysis.CompensationSummary.Count);
        Assert.Equal(new GameWeek(33), analysis.CompensationSummary.NearestDueByEndOfWeek);
        Assert.True(analysis.CompensationSummary.HasOverdue);
        Assert.Equal(analysis.CompensationSummary, report.CompensationSummary);
    }

    private static WeeklyRestCompensation Obligation(
        WeeklyRestCompensationStatus status,
        long remainingMinutes = 0,
        long reductionWeek = 8) => new()
    {
        IdentitySchemeVersion = 1,
        ObligationId = $"obligation-{status}-{reductionWeek}",
        DriverCardId = "PL-DTO",
        SourceRestBlockId = $"rest-{reductionWeek}",
        SourceRestEndExclusive = new GameTime(10_000),
        OriginalOwedMinutes = Math.Max(300, remainingMinutes),
        RemainingMinutes = remainingMinutes,
        ReductionWeek = new GameWeek(reductionWeek),
        DueAtExclusive = new GameTime((reductionWeek + 4) * GameWeek.MinutesPerWeek),
        Status = status
    };
}

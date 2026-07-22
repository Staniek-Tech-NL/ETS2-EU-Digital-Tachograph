using ETS2Tachograph.Core.Time;
using ETS2Tachograph.RuleEngine;

namespace ETS2Tachograph.Application.Dtos;

public enum WeeklyRestCompensationStatusDto
{
    OpenOnTime = 0,
    Overdue = 1,
    PaidOnTime = 2,
    PaidLate = 3
}

public sealed record CompensationMinuteRangeDto(
    long StartGameMinute,
    long EndGameMinuteExclusive)
{
    public long DurationMinutes => EndGameMinuteExclusive - StartGameMinute;
}

public sealed record WeeklyRestCompensationDto(
    int IdentitySchemeVersion,
    string ObligationId,
    string DriverCardId,
    string SourceRestBlockId,
    long SourceRestEndGameMinuteExclusive,
    long OriginalOwedMinutes,
    long RemainingMinutes,
    long ReductionWeek,
    long DueAtGameMinuteExclusive,
    string? PaymentRestBlockId,
    CompensationMinuteRangeDto? PaymentRange,
    long? SettledAtGameMinute,
    WeeklyRestCompensationStatusDto Status)
{
    public bool IsOpen => Status is
        WeeklyRestCompensationStatusDto.OpenOnTime or
        WeeklyRestCompensationStatusDto.Overdue;

    public bool IsOverdue => Status is
        WeeklyRestCompensationStatusDto.Overdue or
        WeeklyRestCompensationStatusDto.PaidLate;

    public GameWeek DueByEndOfWeek => new(ReductionWeek + 3);
}

public static class WeeklyRestCompensationDtoMapper
{
    public static WeeklyRestCompensationDto Map(WeeklyRestCompensation source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new WeeklyRestCompensationDto(
            source.IdentitySchemeVersion,
            source.ObligationId,
            source.DriverCardId,
            source.SourceRestBlockId,
            source.SourceRestEndExclusive.TotalMinutes,
            source.OriginalOwedMinutes,
            source.RemainingMinutes,
            source.ReductionWeek.Index,
            source.DueAtExclusive.TotalMinutes,
            source.PaymentRestBlockId,
            source.PaymentRange is null
                ? null
                : new CompensationMinuteRangeDto(
                    source.PaymentRange.Start.TotalMinutes,
                    source.PaymentRange.EndExclusive.TotalMinutes),
            source.SettledAt?.TotalMinutes,
            MapStatus(source.Status));
    }

    public static IReadOnlyList<WeeklyRestCompensationDto> MapAll(
        IEnumerable<WeeklyRestCompensation> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Select(Map).ToList();
    }

    private static WeeklyRestCompensationStatusDto MapStatus(
        WeeklyRestCompensationStatus status) => status switch
    {
        WeeklyRestCompensationStatus.OpenOnTime => WeeklyRestCompensationStatusDto.OpenOnTime,
        WeeklyRestCompensationStatus.Overdue => WeeklyRestCompensationStatusDto.Overdue,
        WeeklyRestCompensationStatus.PaidOnTime => WeeklyRestCompensationStatusDto.PaidOnTime,
        WeeklyRestCompensationStatus.PaidLate => WeeklyRestCompensationStatusDto.PaidLate,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown compensation status.")
    };
}

internal static class CompensationSummaryProjection
{
    public static CompensationSummary From(
        IReadOnlyList<WeeklyRestCompensationDto> obligations)
    {
        ArgumentNullException.ThrowIfNull(obligations);
        var open = obligations.Where(item => item.IsOpen).ToList();
        var hasOverdue = obligations.Any(item => item.IsOverdue);

        if (open.Count == 0)
            return hasOverdue
                ? CompensationSummary.Empty with { HasOverdue = true }
                : CompensationSummary.Empty;

        return new CompensationSummary(
            open.Sum(item => item.RemainingMinutes),
            open.MinBy(item => item.DueAtGameMinuteExclusive)!.DueByEndOfWeek,
            open.Count,
            hasOverdue);
    }
}

using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.RuleEngine;

namespace ETS2Tachograph.Application.Dtos;

public sealed record ReportDto(
    string DriverCardId,
    long FromGameMinute,
    long ToGameMinuteExclusive,
    long DrivingMinutes,
    long OtherWorkMinutes,
    long AvailabilityMinutes,
    long RestMinutes,
    long OutMinutes,
    IReadOnlyList<ActivityRecord> Records,
    IReadOnlyList<ActivityGap> Gaps,
    IReadOnlyList<ReportViolationDto> Violations)
{
    public IReadOnlyList<WeeklyRestCompensationDto> CompensationObligations { get; init; } = [];
    public IReadOnlyList<RestAllocationProjectionDto> RestAllocations { get; init; } = [];
    public bool PendingRestAllocation => RestAllocations.Any(item => item.IsPending);
    public CompensationSummary CompensationSummary =>
        CompensationSummaryProjection.From(CompensationObligations);
    public long TotalMinutes => DrivingMinutes + OtherWorkMinutes + AvailabilityMinutes + RestMinutes + OutMinutes;
    public int UnresolvedGapCount => Gaps.Count;
    public long GapMinutes => Gaps.Sum(gap => gap.DurationMinutes ?? 0);
    public long CoveredMinutes => TotalMinutes + GapMinutes;
    public long RangeMinutes => Math.Max(0, ToGameMinuteExclusive - FromGameMinute);
    public bool CoverageMatchesRange => CoveredMinutes == RangeMinutes;
    public bool EvidenceComplete =>
        UnresolvedGapCount == 0 &&
        CoverageMatchesRange &&
        !PendingRestAllocation;
    public string GapSummaryText => UnresolvedGapCount == 0
        ? "LUKI: brak"
        : $"LUKI NIEROZLICZONE: {UnresolvedGapCount} · {FormatMinutes(GapMinutes)}";
    public string CoverageBalanceText =>
        $"BILANS: {FormatMinutes(TotalMinutes)} + {FormatMinutes(GapMinutes)} = {FormatMinutes(CoveredMinutes)} / zakres {FormatMinutes(RangeMinutes)}";

    private static string FormatMinutes(long minutes) => $"{minutes / 60:00}:{minutes % 60:00}";
}

public sealed record ReportViolationDto(
    string Type,
    string Article,
    long DetectedAtGameMinute,
    long ExcessMinutes);

public sealed record RegulationReportAnalysisDto(
    IReadOnlyList<ReportViolationDto> Violations,
    IReadOnlyList<WeeklyRestCompensationDto> CompensationObligations)
{
    public IReadOnlyList<RestAllocationProjectionDto> RestAllocations { get; init; } = [];
    public bool PendingRestAllocation => RestAllocations.Any(item => item.IsPending);
    public CompensationSummary CompensationSummary =>
        CompensationSummaryProjection.From(CompensationObligations);

    public static RegulationReportAnalysisDto Empty { get; } = new(
        [],
        []);
}

using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Rules;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.RuleEngine;

namespace ETS2Tachograph.Infrastructure.Persistence;

public sealed class RegulationReportAnalyzer(
    RegulationEngine? engine = null,
    RegulationOptions? options = null) : IRegulationReportAnalyzer
{
    private readonly RegulationEngine _engine = engine ?? new RegulationEngine();
    private readonly RegulationOptions _options = options ?? new RegulationOptions();

    public RegulationReportAnalysisDto Analyze(
        GameTime now,
        IReadOnlyList<ActivityRecord> history) =>
        Analyze(now, history, []);

    public RegulationReportAnalysisDto Analyze(
        GameTime now,
        IReadOnlyList<ActivityRecord> history,
        IReadOnlyList<RestAllocationDecision> decisions)
    {
        var evaluation = _engine.Evaluate(
            new RuleContext(now, history),
            _options,
            decisions);
        return new RegulationReportAnalysisDto(
            evaluation.Violations.Select(x => new ReportViolationDto(
                x.Type.ToString(), x.Article, x.DetectedAt.TotalMinutes, x.ExcessMinutes)).ToList(),
            WeeklyRestCompensationDtoMapper.MapAll(evaluation.CompensationObligations))
        {
            RestAllocations = RestAllocationDtoMapper.MapAll(evaluation.RestAllocations)
        };
    }
}

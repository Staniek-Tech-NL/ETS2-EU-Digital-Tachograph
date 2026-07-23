using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.RuleEngine;

namespace ETS2Tachograph.Application.Persistence;

public interface IRegulationReportAnalyzer
{
    RegulationReportAnalysisDto Analyze(
        GameTime now,
        IReadOnlyList<ActivityRecord> history);

    RegulationReportAnalysisDto Analyze(
        GameTime now,
        IReadOnlyList<ActivityRecord> history,
        IReadOnlyList<RestAllocationDecision> decisions) =>
        Analyze(now, history);
}

public interface IPdfReportExporter
{
    Task ExportAsync(
        ReportDto report,
        Stream destination,
        CancellationToken cancellationToken = default);
}

using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Application.Persistence;

public interface IRegulationReportAnalyzer
{
    RegulationReportAnalysisDto Analyze(
        GameTime now,
        IReadOnlyList<ActivityRecord> history);
}

public interface IPdfReportExporter
{
    Task ExportAsync(
        ReportDto report,
        Stream destination,
        CancellationToken cancellationToken = default);
}

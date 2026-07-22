using System.Text;
using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Application.Services;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.RuleEngine;

namespace ETS2Tachograph.Application.Tests;

public sealed class ReportServiceTests
{
    [Fact]
    public async Task Report_calculates_filtered_totals_and_exports_all_formats()
    {
        var repository = new ReportRepository(
        [
            Record(0, 60, DriverActivity.Driving),
            Record(60, 90, DriverActivity.OtherWork),
            Record(90, 120, DriverActivity.BreakOrRest)
        ]);
        var service = new ReportService(repository, new EmptyRegulationAnalyzer());
        var report = await service.CreateAsync("PL-REPORT", new GameTime(0), new GameTime(120));

        Assert.Equal(60, report.DrivingMinutes);
        Assert.Equal(30, report.OtherWorkMinutes);
        Assert.Equal(30, report.RestMinutes);
        Assert.Equal(0, report.UnresolvedGapCount);
        Assert.Equal("LUKI: brak", report.GapSummaryText);
        Assert.True(report.CoverageMatchesRange);
        Assert.True(report.EvidenceComplete);

        await using var csv = new MemoryStream();
        await service.ExportCsvAsync(report, csv);
        var csvText = Encoding.UTF8.GetString(csv.ToArray());
        Assert.Contains("Driving", csvText);
        Assert.Contains("start_game_time;end_game_time", csvText);
        Assert.Contains("Dzień 1, 00:00;Dzień 1, 01:00", csvText);

        await using var json = new MemoryStream();
        await service.ExportVtcJsonAsync(report, json);
        var jsonText = Encoding.UTF8.GetString(json.ToArray());
        Assert.Contains("ets2-tachograph-vtc/1", jsonText);
        Assert.Contains("\"unresolvedGapCount\": 0", jsonText);
        Assert.Contains("\"evidenceComplete\": true", jsonText);

    }

    [Fact]
    public async Task Csv_export_writes_exactly_one_record_per_compensation_obligation()
    {
        var repository = new ReportRepository(
        [
            Record(0, 1, DriverActivity.Driving),
            Record(1, 2, DriverActivity.Driving),
            Record(2, 3, DriverActivity.Driving)
        ]);
        IReadOnlyList<WeeklyRestCompensationDto> obligations =
        [
            Compensation(300, 30, WeeklyRestCompensationStatusDto.OpenOnTime),
            PaidCompensation(420, 31)
        ];
        var service = new ReportService(repository, new FixedRegulationAnalyzer(obligations));
        var report = await service.CreateAsync("PL-REPORT", new GameTime(0), new GameTime(3));
        await using var csv = new MemoryStream();

        await service.ExportCompensationCsvAsync(report, csv);

        var lines = Encoding.UTF8.GetString(csv.ToArray())
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Contains("obligation-30;PL-REPORT;rest-30", lines[1]);
        Assert.Contains("payment-31;500;920;920;PaidOnTime", lines[2]);
    }

    [Fact]
    public async Task Report_keeps_gaps_out_of_activity_totals_and_counts_them_as_coverage()
    {
        var gap = new ActivityGap
        {
            Id = Guid.NewGuid(),
            DriverCardId = "PL-REPORT",
            Slot = 1,
            SessionIndex = 0,
            Start = new GameTime(1),
            EndExclusive = new GameTime(5),
            Reason = ActivityGapReason.ForwardTimeJump,
            State = ActivityGapState.Unresolved
        };
        var repository = new ReportRepository(
        [
            Record(0, 1, DriverActivity.Driving),
            Record(5, 6, DriverActivity.Driving)
        ],
        [gap]);
        var service = new ReportService(repository, new EmptyRegulationAnalyzer());
        var report = await service
            .CreateAsync("PL-REPORT", new GameTime(0), new GameTime(6));

        Assert.Equal(2, report.DrivingMinutes);
        Assert.Equal(4, report.GapMinutes);
        Assert.Equal(6, report.CoveredMinutes);
        Assert.Equal(6, report.RangeMinutes);
        Assert.True(report.CoverageMatchesRange);
        Assert.False(report.EvidenceComplete);
        Assert.Equal(1, report.UnresolvedGapCount);
        Assert.Equal("LUKI NIEROZLICZONE: 1 · 00:04", report.GapSummaryText);
        Assert.Single(report.Gaps);
        Assert.Equal(1, repository.UnresolvedQueryCount);

        await using var json = new MemoryStream();
        await service.ExportVtcJsonAsync(report, json);
        var jsonText = Encoding.UTF8.GetString(json.ToArray());
        Assert.Contains("\"unresolvedGapCount\": 1", jsonText);
        Assert.Contains("\"unresolvedGapMinutes\": 4", jsonText);
        Assert.Contains("\"balanceMatchesRange\": true", jsonText);
        Assert.Contains("\"evidenceComplete\": false", jsonText);
    }

    [Fact]
    public async Task Resolved_gap_does_not_reduce_report_completeness()
    {
        var resolvedGap = new ActivityGap
        {
            Id = Guid.NewGuid(),
            DriverCardId = "PL-REPORT",
            Slot = 1,
            SessionIndex = 0,
            Start = new GameTime(1),
            EndExclusive = new GameTime(5),
            Reason = ActivityGapReason.CardRemoved,
            State = ActivityGapState.Resolved,
            ResolvedAt = new GameTime(6)
        };
        var repository = new ReportRepository(
            [Record(0, 6, DriverActivity.BreakOrRest)],
            [resolvedGap]);

        var report = await new ReportService(repository, new EmptyRegulationAnalyzer())
            .CreateAsync("PL-REPORT", new GameTime(0), new GameTime(6));

        Assert.Empty(report.Gaps);
        Assert.Equal(0, report.UnresolvedGapCount);
        Assert.Equal("LUKI: brak", report.GapSummaryText);
        Assert.True(report.EvidenceComplete);
        Assert.Equal(1, repository.UnresolvedQueryCount);
    }

    [Fact]
    public async Task Report_derives_compensation_summary_from_obligations_and_exports_it_to_json()
    {
        IReadOnlyList<WeeklyRestCompensationDto> obligations =
        [
            Compensation(600, 30, WeeklyRestCompensationStatusDto.Overdue),
            Compensation(660, 31, WeeklyRestCompensationStatusDto.OpenOnTime)
        ];
        var repository = new ReportRepository([Record(0, 1, DriverActivity.OtherWork)]);
        var report = await new ReportService(repository, new FixedRegulationAnalyzer(obligations))
            .CreateAsync("PL-REPORT", new GameTime(0), new GameTime(1));

        Assert.Equal(obligations, report.CompensationObligations);
        Assert.Equal(1_260, report.CompensationSummary.TotalOwedMinutes);
        Assert.Equal(new GameWeek(33), report.CompensationSummary.NearestDueByEndOfWeek);

        await using var json = new MemoryStream();
        await new ReportService(repository, new FixedRegulationAnalyzer(obligations))
            .ExportVtcJsonAsync(report, json);
        var text = Encoding.UTF8.GetString(json.ToArray());
        Assert.Contains("\"totalOwedMinutes\": 1260", text);
        Assert.Contains("\"count\": 2", text);
        Assert.Contains("\"hasOverdue\": true", text);
        Assert.Contains("\"compensationObligations\"", text);
        Assert.Contains("\"obligationId\": \"obligation-30\"", text);
        Assert.Contains("\"sourceRestBlockId\": \"rest-31\"", text);
        Assert.Contains("\"dueAtGameMinuteExclusive\"", text);
        Assert.Contains("\"status\": \"Overdue\"", text);
    }

    private static WeeklyRestCompensationDto Compensation(
        long remainingMinutes,
        long reductionWeek,
        WeeklyRestCompensationStatusDto status) => new(
            1,
            $"obligation-{reductionWeek}",
            "PL-REPORT",
            $"rest-{reductionWeek}",
            reductionWeek * GameWeek.MinutesPerWeek,
            remainingMinutes,
            remainingMinutes,
            reductionWeek,
            (reductionWeek + 4) * GameWeek.MinutesPerWeek,
            null,
            null,
            null,
            status);

    private static WeeklyRestCompensationDto PaidCompensation(
        long originalOwedMinutes,
        long reductionWeek) => new(
            1,
            $"obligation-{reductionWeek}",
            "PL-REPORT",
            $"rest-{reductionWeek}",
            reductionWeek * GameWeek.MinutesPerWeek,
            originalOwedMinutes,
            0,
            reductionWeek,
            (reductionWeek + 4) * GameWeek.MinutesPerWeek,
            $"payment-{reductionWeek}",
            new CompensationMinuteRangeDto(500, 920),
            920,
            WeeklyRestCompensationStatusDto.PaidOnTime);

    private static ActivityRecord Record(long start, long end, DriverActivity activity) => new()
    {
        Id = Guid.NewGuid(),
        DriverCardId = "PL-REPORT",
        Activity = activity,
        Start = new GameTime(start),
        EndExclusive = new GameTime(end),
        RecordedAtUtc = DateTimeOffset.UtcNow,
        Source = ActivitySource.Telemetry
    };

    private sealed class ReportRepository(
        IReadOnlyList<ActivityRecord> records,
        IReadOnlyList<ActivityGap>? gaps = null) : IActivityRepository
    {
        public int UnresolvedQueryCount { get; private set; }
        public Task EnsureSessionAsync(string driverCardId, int sessionIndex, GameTime startedAt, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AppendAsync(string driverCardId, int sessionIndex, IReadOnlyList<ActivityRecord> recordsToAdd, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ApplySessionWritesAsync(IReadOnlyList<ActivitySessionWrite> writes, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<ActivityRecord>> LoadDriverHistoryAsync(string driverCardId, GameTime? from = null, GameTime? toExclusive = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ActivityRecord>>(records.Where(x =>
                (from is null || x.EndExclusive > from.Value) &&
                (toExclusive is null || x.Start < toExclusive.Value)).ToList());
        public Task<IReadOnlyList<StoredActivitySession>> LoadSessionsAsync(string driverCardId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StoredActivitySession>>([]);
        public Task<IReadOnlyList<ActivityGap>> LoadDriverGapsAsync(string driverCardId, GameTime? from = null, GameTime? toExclusive = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ActivityGap>>((gaps ?? []).Where(gap =>
                (from is null || gap.EndExclusive is null || gap.EndExclusive.Value > from.Value) &&
                (toExclusive is null || gap.Start < toExclusive.Value)).ToList());
        public Task<IReadOnlyList<ActivityGap>> GetUnresolvedGapsAsync(
            string? driverCardId = null,
            GameTime? fromGameMinute = null,
            GameTime? toGameMinute = null,
            CancellationToken cancellationToken = default)
        {
            UnresolvedQueryCount++;
            return Task.FromResult<IReadOnlyList<ActivityGap>>((gaps ?? []).Where(gap =>
                gap.State == ActivityGapState.Unresolved &&
                (driverCardId is null || string.Equals(gap.DriverCardId, driverCardId, StringComparison.OrdinalIgnoreCase)) &&
                (fromGameMinute is null || gap.EndExclusive is null || gap.EndExclusive.Value > fromGameMinute.Value) &&
                (toGameMinute is null || gap.Start < toGameMinute.Value)).ToList());
        }
    }

    private sealed class EmptyRegulationAnalyzer : IRegulationReportAnalyzer
    {
        public Application.Dtos.RegulationReportAnalysisDto Analyze(
            GameTime now,
            IReadOnlyList<ActivityRecord> history) => Application.Dtos.RegulationReportAnalysisDto.Empty;
    }

    private sealed class FixedRegulationAnalyzer(
        IReadOnlyList<WeeklyRestCompensationDto> obligations) : IRegulationReportAnalyzer
    {
        public Application.Dtos.RegulationReportAnalysisDto Analyze(
            GameTime now,
            IReadOnlyList<ActivityRecord> history) => new([], obligations);
    }
}

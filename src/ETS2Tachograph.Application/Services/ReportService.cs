using System.Globalization;
using System.Text;
using System.Text.Json;
using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Application.Services;

public sealed class ReportService(
    IActivityRepository activities,
    IRegulationReportAnalyzer regulationAnalyzer)
{

    public async Task<ReportDto> CreateAsync(
        string driverCardId,
        GameTime? from = null,
        GameTime? toExclusive = null,
        CancellationToken cancellationToken = default)
    {
        var loaded = await activities.LoadDriverHistoryAsync(driverCardId, from, toExclusive, cancellationToken);
        // Report completeness is affected only by unresolved canonical gaps.
        // Resolved gaps remain available through the audit history/ManualEntry link.
        var loadedGaps = await activities.GetUnresolvedGapsAsync(
            driverCardId, from, toExclusive, cancellationToken);
        var firstKnownMinute = loaded.Select(record => record.Start.TotalMinutes)
            .Concat(loadedGaps.Select(gap => gap.Start.TotalMinutes))
            .DefaultIfEmpty(0)
            .Min();
        var lastKnownMinute = loaded.Select(record => record.EndExclusive.TotalMinutes)
            .Concat(loadedGaps.Where(gap => gap.EndExclusive is not null)
                .Select(gap => gap.EndExclusive!.Value.TotalMinutes))
            .DefaultIfEmpty(firstKnownMinute)
            .Max();
        var start = from?.TotalMinutes ?? firstKnownMinute;
        var end = toExclusive?.TotalMinutes ?? lastKnownMinute;
        var records = loaded.Select(record => record with
        {
            Start = new GameTime(Math.Max(start, record.Start.TotalMinutes)),
            EndExclusive = new GameTime(Math.Min(end, record.EndExclusive.TotalMinutes))
        }).Where(record => record.EndExclusive > record.Start).ToList();
        var gaps = loadedGaps.Select(gap => gap with
        {
            Start = new GameTime(Math.Max(start, gap.Start.TotalMinutes)),
            EndExclusive = new GameTime(Math.Min(
                end,
                gap.EndExclusive?.TotalMinutes ?? end))
        }).Where(gap => gap.EndExclusive > gap.Start).ToList();
        long Sum(DriverActivity activity) => records.Where(x => x.Activity == activity)
            .Sum(x => x.EndExclusive - x.Start);
        var regulation = regulationAnalyzer.Analyze(new GameTime(end), records);
        return new ReportDto(
            driverCardId, start, end,
            Sum(DriverActivity.Driving), Sum(DriverActivity.OtherWork),
            Sum(DriverActivity.Availability), Sum(DriverActivity.BreakOrRest),
            Sum(DriverActivity.OutOfScope), records, gaps, regulation.Violations)
        {
            CompensationObligations = regulation.CompensationObligations
        };
    }

    public async Task ExportCsvAsync(ReportDto report, Stream destination, CancellationToken cancellationToken = default)
    {
        var rawRecords = await activities.LoadRawDriverHistoryAsync(
            report.DriverCardId,
            new GameTime(report.FromGameMinute),
            new GameTime(report.ToGameMinuteExclusive),
            cancellationToken);
        await using var writer = new StreamWriter(destination, new UTF8Encoding(true), leaveOpen: true);
        await writer.WriteLineAsync("start_game_time;end_game_time;activity;source;condition;source_gap_id;recorded_at_utc");
        foreach (var record in rawRecords)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(string.Join(';',
                GameClockFormatter.Format(record.Start), GameClockFormatter.Format(record.EndExclusive), record.Activity,
                record.Source, record.Condition, record.SourceGapId,
                record.RecordedAtUtc.ToString("O", CultureInfo.InvariantCulture)));
        }
        await writer.FlushAsync(cancellationToken);
    }

    public async Task ExportCompensationCsvAsync(
        ReportDto report,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        await using var writer = new StreamWriter(destination, new UTF8Encoding(true), leaveOpen: true);
        await writer.WriteLineAsync(
            "identity_scheme_version;obligation_id;driver_card_id;source_rest_block_id;" +
            "source_rest_end_game_minute_exclusive;original_owed_minutes;remaining_minutes;" +
            "reduction_week;due_at_game_minute_exclusive;payment_rest_block_id;" +
            "payment_range_start_game_minute;payment_range_end_game_minute_exclusive;" +
            "settled_at_game_minute;status");
        foreach (var obligation in report.CompensationObligations
                     .OrderBy(item => item.DueAtGameMinuteExclusive)
                     .ThenBy(item => item.ObligationId, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(string.Join(';',
                obligation.IdentitySchemeVersion.ToString(CultureInfo.InvariantCulture),
                CsvCell(obligation.ObligationId),
                CsvCell(obligation.DriverCardId),
                CsvCell(obligation.SourceRestBlockId),
                obligation.SourceRestEndGameMinuteExclusive.ToString(CultureInfo.InvariantCulture),
                obligation.OriginalOwedMinutes.ToString(CultureInfo.InvariantCulture),
                obligation.RemainingMinutes.ToString(CultureInfo.InvariantCulture),
                obligation.ReductionWeek.ToString(CultureInfo.InvariantCulture),
                obligation.DueAtGameMinuteExclusive.ToString(CultureInfo.InvariantCulture),
                CsvCell(obligation.PaymentRestBlockId),
                obligation.PaymentRange?.StartGameMinute.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                obligation.PaymentRange?.EndGameMinuteExclusive.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                obligation.SettledAtGameMinute?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                obligation.Status.ToString()));
        }
        await writer.FlushAsync(cancellationToken);
    }

    public Task ExportVtcJsonAsync(ReportDto report, Stream destination, CancellationToken cancellationToken = default) =>
        JsonSerializer.SerializeAsync(destination, new
        {
            schema = "ets2-tachograph-vtc/1",
            driverCard = report.DriverCardId,
            range = new
            {
                fromGameMinute = report.FromGameMinute,
                toGameMinute = report.ToGameMinuteExclusive,
                fromGameTime = GameClockFormatter.Format(new GameTime(report.FromGameMinute)),
                toGameTime = GameClockFormatter.Format(new GameTime(report.ToGameMinuteExclusive))
            },
            totals = new
            {
                driving = report.DrivingMinutes,
                work = report.OtherWorkMinutes,
                availability = report.AvailabilityMinutes,
                rest = report.RestMinutes,
                outOfScope = report.OutMinutes
            },
            completeness = new
            {
                unresolvedGapCount = report.UnresolvedGapCount,
                unresolvedGapMinutes = report.GapMinutes,
                activityMinutes = report.TotalMinutes,
                coveredMinutes = report.CoveredMinutes,
                rangeMinutes = report.RangeMinutes,
                balanceMatchesRange = report.CoverageMatchesRange,
                evidenceComplete = report.EvidenceComplete
            },
            violations = report.Violations.Select(x => new { x.Type, x.Article, x.ExcessMinutes }),
            compensation = new
            {
                totalOwedMinutes = report.CompensationSummary.TotalOwedMinutes,
                count = report.CompensationSummary.Count,
                nearestDueByEndOfWeek = report.CompensationSummary.NearestDueByEndOfWeek?.Index,
                hasOverdue = report.CompensationSummary.HasOverdue
            },
            compensationObligations = report.CompensationObligations,
            gaps = report.Gaps.Select(gap => new
            {
                start = gap.Start.TotalMinutes,
                end = gap.EndExclusive?.TotalMinutes,
                reason = gap.Reason.ToString(),
                state = gap.State.ToString(),
                resolvedAt = gap.ResolvedAt?.TotalMinutes,
                gap.Slot,
                gap.SessionIndex
            }),
            activities = report.Records.Select(x => new
            {
                start = x.Start.TotalMinutes,
                end = x.EndExclusive.TotalMinutes,
                startGameTime = GameClockFormatter.Format(x.Start),
                endGameTime = GameClockFormatter.Format(x.EndExclusive),
                activity = x.Activity.ToString(),
                source = x.Source.ToString(),
                condition = x.Condition.ToString(),
                sourceGapId = x.SourceGapId
            })
        }, ExportService.JsonOptions, cancellationToken);

    private static string CsvCell(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.IndexOfAny([';', '"', '\r', '\n']) < 0
            ? value
            : $"\"{value.Replace("\"", "\"\"")}\"";
    }

}

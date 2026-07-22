using System.Text;
using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.RuleEngine;
using PdfSharp.Pdf.IO;
using Xunit;

namespace ETS2Tachograph.Reports.Tests;

public sealed class PdfReportExporterTests
{
    [Fact]
    public async Task Export_creates_pdf_document()
    {
        var record = new ActivityRecord
        {
            Id = Guid.NewGuid(),
            DriverCardId = "PL-REPORT",
            Activity = DriverActivity.Driving,
            Start = new GameTime(0),
            EndExclusive = new GameTime(60),
            RecordedAtUtc = DateTimeOffset.UtcNow,
            Source = ActivitySource.Telemetry
        };
        var report = new ReportDto("PL-REPORT", 0, 60, 60, 0, 0, 0, 0, [record], [], [])
        {
            CompensationObligations =
            [
                Compensation(600, 30, WeeklyRestCompensationStatusDto.Overdue),
                PaidCompensation(660, 31)
            ]
        };
        await using var destination = new MemoryStream();

        await new PdfReportExporter().ExportAsync(report, destination);

        Assert.Equal("%PDF", Encoding.ASCII.GetString(destination.ToArray(), 0, 4));
        Assert.True(destination.Length > 3_000);
        destination.Position = 0;
        using var document = PdfReader.Open(destination, PdfDocumentOpenMode.Import);
        Assert.NotEmpty(document.Pages);
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
            $"obligation-v1-{new string('a', 64)}",
            "PL-REPORT",
            $"rest-v1-{new string('b', 64)}",
            reductionWeek * GameWeek.MinutesPerWeek,
            originalOwedMinutes,
            0,
            reductionWeek,
            (reductionWeek + 4) * GameWeek.MinutesPerWeek,
            $"rest-v1-{new string('c', 64)}",
            new CompensationMinuteRangeDto(10_000, 10_000 + originalOwedMinutes),
            10_000 + originalOwedMinutes,
            WeeklyRestCompensationStatusDto.PaidOnTime);

    [Fact]
    public async Task Export_accepts_explicit_unresolved_gap_completeness_summary()
    {
        var gap = new ActivityGap
        {
            Id = Guid.NewGuid(),
            DriverCardId = "PL-REPORT",
            Slot = 1,
            SessionIndex = 0,
            Start = new GameTime(60),
            EndExclusive = new GameTime(120),
            Reason = ActivityGapReason.CardRemoved,
            State = ActivityGapState.Unresolved
        };
        var report = new ReportDto(
            "PL-REPORT", 0, 120, 60, 0, 0, 0, 0,
            [Record(0, 60, DriverActivity.Driving)], [gap], []);
        await using var destination = new MemoryStream();

        await new PdfReportExporter().ExportAsync(report, destination);

        Assert.Equal("LUKI NIEROZLICZONE: 1 · 01:00", report.GapSummaryText);
        Assert.Equal("BILANS: 01:00 + 01:00 = 02:00 / zakres 02:00", report.CoverageBalanceText);
        Assert.True(destination.Length > 2_000);
    }

    [Fact]
    public void Presentation_merges_equal_activity_even_when_source_changes()
    {
        var blocks = new ReportPresentationBuilder().BuildBlocks(
        [
            Record(0, 1, DriverActivity.Driving, ActivitySource.Telemetry),
            Record(1, 2, DriverActivity.Driving, ActivitySource.Reconstructed),
            Record(2, 3, DriverActivity.OtherWork, ActivitySource.Manual)
        ]);

        Assert.Equal(2, blocks.Count);
        Assert.Equal(2, blocks[0].DurationMinutes);
        Assert.Equal("Telemetria / częściowo rekonstruowana", blocks[0].SourceLabel);
    }

    [Fact]
    public void Presentation_reduces_254_minute_records_to_one_block()
    {
        var records = Enumerable.Range(0, 254)
            .Select(minute => Record(minute, minute + 1, DriverActivity.Driving))
            .ToList();

        var block = Assert.Single(new ReportPresentationBuilder().BuildBlocks(records));

        Assert.Equal(254, block.DurationMinutes);
    }

    [Fact]
    public void Presentation_keeps_rest_blocks_separate_and_labels_forward_jump_gap()
    {
        var gap = new ActivityGap
        {
            Id = Guid.NewGuid(),
            DriverCardId = "PL-REPORT",
            Slot = 1,
            SessionIndex = 0,
            Start = new GameTime(360),
            EndExclusive = new GameTime(361),
            Reason = ActivityGapReason.ForwardTimeJump,
            State = ActivityGapState.Unresolved
        };

        var timeline = new ReportPresentationBuilder().BuildTimelineBlocks(
        [
            Record(0, 360, DriverActivity.BreakOrRest),
            Record(361, 601, DriverActivity.BreakOrRest)
        ],
        [gap]);

        Assert.Equal(3, timeline.Count);
        Assert.Equal(2, timeline.Count(block => !block.IsGap));
        Assert.Equal(
            "Brak danych — skok czasu",
            Assert.Single(timeline, block => block.IsGap).ActivityLabel);
    }

    [Fact]
    public void Presentation_labels_card_removed_gap()
    {
        var gap = new ActivityGap
        {
            Id = Guid.NewGuid(),
            DriverCardId = "PL-REPORT",
            Slot = 2,
            SessionIndex = 0,
            Start = new GameTime(100),
            EndExclusive = new GameTime(500),
            Reason = ActivityGapReason.CardRemoved,
            State = ActivityGapState.Unresolved
        };

        var block = Assert.Single(new ReportPresentationBuilder().BuildTimelineBlocks([], [gap]));

        Assert.True(block.IsGap);
        Assert.Equal("Brak danych — karta wyjęta", block.ActivityLabel);
    }

    [Fact]
    public void Resolved_gap_is_preserved_for_audit_but_not_rendered_over_manual_activity()
    {
        var gapId = Guid.NewGuid();
        var gap = new ActivityGap
        {
            Id = gapId,
            DriverCardId = "PL-REPORT",
            Slot = 1,
            SessionIndex = 0,
            Start = new GameTime(100),
            EndExclusive = new GameTime(160),
            Reason = ActivityGapReason.CardRemoved,
            State = ActivityGapState.Resolved,
            ResolvedAt = new GameTime(500)
        };
        var manual = Record(
            100, 160, DriverActivity.BreakOrRest, ActivitySource.ManualEntry) with
        {
            SourceGapId = gapId
        };

        var block = Assert.Single(new ReportPresentationBuilder().BuildTimelineBlocks(
            [manual],
            [gap]));

        Assert.False(block.IsGap);
        Assert.Equal("Wpis manualny", block.Activity!.SourceLabel);
    }

    [Fact]
    public void Checkpoint_shows_continuous_counter_reset_after_45_minute_break()
    {
        var checkpoint = Assert.Single(new ReportPresentationBuilder().BuildCheckpoints(
        [
            Record(0, 270, DriverActivity.Driving),
            Record(270, 315, DriverActivity.BreakOrRest)
        ]));

        Assert.Equal(270, checkpoint.ContinuousDrivingBefore);
        Assert.Equal(0, checkpoint.ContinuousDrivingAfter);
        Assert.Equal(270, checkpoint.DailyDrivingBefore);
        Assert.Equal(270, checkpoint.DailyDrivingAfter);
        Assert.False(checkpoint.DailyDrivingReset);
    }

    [Fact]
    public void Checkpoint_marks_daily_counter_reset_after_nine_hour_rest()
    {
        var checkpoint = Assert.Single(new ReportPresentationBuilder().BuildCheckpoints(
        [
            Record(0, 300, DriverActivity.Driving),
            Record(300, 840, DriverActivity.BreakOrRest)
        ]));

        Assert.Equal(300, checkpoint.DailyDrivingBefore);
        Assert.Equal(0, checkpoint.DailyDrivingAfter);
        Assert.True(checkpoint.DailyDrivingReset);
    }

    private static ActivityRecord Record(
        long start,
        long end,
        DriverActivity activity,
        ActivitySource source = ActivitySource.Telemetry) => new()
    {
        Id = Guid.NewGuid(),
        DriverCardId = "PL-REPORT",
        Activity = activity,
        Start = new GameTime(start),
        EndExclusive = new GameTime(end),
        RecordedAtUtc = DateTimeOffset.UtcNow,
        Source = source
    };
}

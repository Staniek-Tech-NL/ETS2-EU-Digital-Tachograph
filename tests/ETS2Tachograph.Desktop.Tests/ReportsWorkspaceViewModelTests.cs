using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Application.Services;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.Desktop;

namespace ETS2Tachograph.Desktop.Tests;

public sealed class ReportsWorkspaceViewModelTests
{
    [Fact]
    public async Task Rpt_rng_01_current_week_uses_raw_week_offset()
    {
        const long now = 20_000;
        const int offset = -1;
        var fixture = Fixture(0, now);
        var viewModel = fixture.ViewModel(() => now, offset);

        await viewModel.InitializeAsync([Profile()]);

        var expected = GameWeek.From(new GameTime(now), offset)
            .GetBounds(offset)
            .StartGameMinute;
        Assert.Equal(expected, viewModel.CurrentReport!.FromGameMinute);
        Assert.Equal(now, viewModel.CurrentReport.ToGameMinuteExclusive);
    }

    [Fact]
    public async Task Rpt_rng_03_last_24_hours_is_exactly_1440_minutes()
    {
        const long now = 20_000;
        var fixture = Fixture(0, now);
        var viewModel = fixture.ViewModel(() => now);
        await viewModel.InitializeAsync([Profile()]);

        await viewModel.SelectPresetAsync(ReportRangePreset.Last24GameHours);

        Assert.Equal(1_440, viewModel.CurrentReport!.RangeMinutes);
    }

    [Fact]
    public async Task Rpt_rng_04_last_24_hours_clips_to_short_history()
    {
        const long now = 2_000;
        var fixture = Fixture(1_500, now);
        var viewModel = fixture.ViewModel(() => now);
        await viewModel.InitializeAsync([Profile()]);

        await viewModel.SelectPresetAsync(ReportRangePreset.Last24GameHours);

        Assert.Equal(1_500, viewModel.CurrentReport!.FromGameMinute);
        Assert.Equal(500, viewModel.CurrentReport.RangeMinutes);
    }

    [Fact]
    public async Task Rpt_rng_05_all_history_uses_available_bounds()
    {
        var fixture = Fixture(120, 420);
        var viewModel = fixture.ViewModel(() => 500);
        await viewModel.InitializeAsync([Profile()]);

        await viewModel.SelectPresetAsync(ReportRangePreset.AllHistory);

        Assert.Equal(120, viewModel.CurrentReport!.FromGameMinute);
        Assert.Equal(420, viewModel.CurrentReport.ToGameMinuteExclusive);
    }

    [Fact]
    public async Task Rpt_rng_06_no_history_is_explicit_and_blocks_export()
    {
        var fixture = new FixtureState([]);
        var viewModel = fixture.ViewModel(() => 500);

        await viewModel.InitializeAsync([Profile()]);

        Assert.False(viewModel.HasData);
        Assert.False(viewModel.CanExport);
        Assert.Null(viewModel.CurrentReport);
        Assert.Contains("nie ma historii", viewModel.StatusMessage);
    }

    [Fact]
    public async Task Rpt_rng_08_equal_custom_bounds_are_invalid()
    {
        var fixture = Fixture(0, 3_000);
        var viewModel = fixture.ViewModel(() => 3_000);
        await viewModel.InitializeAsync([Profile()]);
        await viewModel.SelectPresetAsync(ReportRangePreset.Custom);
        viewModel.ToDay = viewModel.FromDay;
        viewModel.ToTime = viewModel.FromTime;
        var calls = fixture.Repository.CreateQueryCount;

        await viewModel.RefreshAsync();

        Assert.Equal(ReportPreviewStatus.InvalidParameters, viewModel.PreviewStatus);
        Assert.Equal(calls, fixture.Repository.CreateQueryCount);
    }

    [Fact]
    public async Task Rpt_rng_07_custom_range_crosses_midnight_without_day_error()
    {
        var fixture = Fixture(0, 3_000);
        var viewModel = fixture.ViewModel(() => 3_000);
        await viewModel.InitializeAsync([Profile()]);
        await viewModel.SelectPresetAsync(ReportRangePreset.Custom);
        viewModel.FromDay = viewModel.DayOptions.Single(day => day.Day == 1);
        viewModel.FromTime = "23:59";
        viewModel.ToDay = viewModel.DayOptions.Single(day => day.Day == 2);
        viewModel.ToTime = "00:01";

        await viewModel.RefreshAsync();

        Assert.Equal(2, viewModel.CurrentReport!.RangeMinutes);
        Assert.Equal(1_439, viewModel.CurrentReport.FromGameMinute);
        Assert.Equal(1_441, viewModel.CurrentReport.ToGameMinuteExclusive);
    }

    [Fact]
    public async Task Rpt_state_02_gap_marks_current_preview_incomplete()
    {
        var gap = new ActivityGap
        {
            Id = Guid.NewGuid(),
            DriverCardId = "CARD-1",
            Slot = 1,
            SessionIndex = 0,
            Start = new GameTime(60),
            EndExclusive = new GameTime(120),
            Reason = ActivityGapReason.ForwardTimeJump,
            State = ActivityGapState.Unresolved
        };
        var fixture = new FixtureState(
            [Record(0, 60), Record(120, 180)],
            [gap]);
        var viewModel = fixture.ViewModel(() => 180);

        await viewModel.InitializeAsync([Profile()]);

        Assert.Equal(ReportPreviewStatus.CurrentIncomplete, viewModel.PreviewStatus);
        Assert.True(viewModel.CanExport);
        Assert.Contains("NIEKOMPLETNY", viewModel.StatusMessage);
    }

    [Fact]
    public async Task Rpt_state_02_missing_coverage_is_explained_without_a_registered_gap()
    {
        var fixture = Fixture(0, 177);
        var viewModel = fixture.ViewModel(() => 180);

        await viewModel.InitializeAsync([Profile()]);

        Assert.Equal(ReportPreviewStatus.CurrentIncomplete, viewModel.PreviewStatus);
        Assert.Equal(0, viewModel.CurrentReport!.UnresolvedGapCount);
        Assert.Contains("Brak pokrycia: 00:03", viewModel.StatusDetail);
        Assert.Equal("NIEKOMPLETNY", viewModel.CompletenessEvidence);
        Assert.Equal("03:00", viewModel.CompletenessRange);
        Assert.Equal("02:57", viewModel.CompletenessActivities);
    }

    [Fact]
    public async Task Rpt_state_03_parameter_change_marks_preview_out_of_date()
    {
        var fixture = Fixture(0, 3_000);
        var viewModel = fixture.ViewModel(() => 3_000);
        await viewModel.InitializeAsync([Profile()]);
        await viewModel.SelectPresetAsync(ReportRangePreset.Custom);

        viewModel.FromTime = "00:01";

        Assert.Equal(ReportPreviewStatus.OutOfDate, viewModel.PreviewStatus);
        Assert.True(viewModel.CanExport);
    }

    [Fact]
    public async Task Rpt_state_08_technical_toggle_does_not_recalculate()
    {
        var fixture = Fixture(0, 180);
        var viewModel = fixture.ViewModel(() => 180);
        await viewModel.InitializeAsync([Profile()]);
        var calls = fixture.Repository.CreateQueryCount;

        viewModel.ShowTechnicalData = true;

        Assert.Equal(calls, fixture.Repository.CreateQueryCount);
    }

    [Fact]
    public async Task Rpt_state_04_error_keeps_last_preview()
    {
        var fixture = Fixture(0, 180);
        var viewModel = fixture.ViewModel(() => 180);
        await viewModel.InitializeAsync([Profile()]);
        var previous = viewModel.CurrentReport;
        fixture.Repository.ThrowOnNextHistoryQuery = true;

        await viewModel.RefreshAsync();

        Assert.Equal(ReportPreviewStatus.Error, viewModel.PreviewStatus);
        Assert.Same(previous, viewModel.CurrentReport);
    }

    [Fact]
    public async Task Rpt_state_06_two_refreshes_start_only_one_report_request()
    {
        var fixture = Fixture(0, 180);
        var viewModel = fixture.ViewModel(() => 180);
        await viewModel.InitializeAsync([Profile()]);
        fixture.Repository.QueryDelay = TimeSpan.FromMilliseconds(50);
        var calls = fixture.Repository.CreateQueryCount;

        var first = viewModel.RefreshAsync();
        var second = viewModel.RefreshAsync();
        await Task.WhenAll(first, second);

        Assert.Equal(calls + 1, fixture.Repository.CreateQueryCount);
    }

    [Fact]
    public async Task Rpt_exp_02_export_refreshes_and_passes_same_report_to_exporter()
    {
        var fixture = Fixture(0, 3_000);
        var viewModel = fixture.ViewModel(() => 3_000);
        await viewModel.InitializeAsync([Profile()]);
        await viewModel.SelectPresetAsync(ReportRangePreset.Custom);
        viewModel.FromTime = "00:01";
        Assert.Equal(ReportPreviewStatus.OutOfDate, viewModel.PreviewStatus);

        await viewModel.ExportAsync(ReportExportFormat.Pdf);

        Assert.Same(viewModel.CurrentReport, fixture.ExportedReport);
        Assert.Equal(ReportExportFormat.Pdf, fixture.ExportedFormat);
        Assert.Equal(ReportPreviewStatus.Current, viewModel.PreviewStatus);
    }

    [Theory]
    [InlineData(ReportExportFormat.Pdf)]
    [InlineData(ReportExportFormat.VtcJson)]
    [InlineData(ReportExportFormat.CompensationCsv)]
    [InlineData(ReportExportFormat.RawActivityCsv)]
    public async Task Rpt_exp_01_to_05_all_formats_receive_current_preview(
        ReportExportFormat format)
    {
        var fixture = Fixture(0, 180);
        var viewModel = fixture.ViewModel(() => 180);
        await viewModel.InitializeAsync([Profile()]);

        await viewModel.ExportAsync(format);

        Assert.Same(viewModel.CurrentReport, fixture.ExportedReport);
        Assert.Equal(format, fixture.ExportedFormat);
    }

    private static FixtureState Fixture(long start, long end) =>
        new([Record(start, end)]);

    private static DriverProfileDto Profile() => new(
        Guid.Parse("10000000-0000-0000-0000-000000000001"),
        "Arkadiusz",
        true,
        DateTimeOffset.UnixEpoch,
        [new("CARD-1", "PL", new DateOnly(2020, 1, 1), new DateOnly(2030, 1, 1))]);

    private static ActivityRecord Record(long start, long end) => new()
    {
        Id = Guid.NewGuid(),
        DriverCardId = "CARD-1",
        Activity = DriverActivity.Driving,
        Start = new GameTime(start),
        EndExclusive = new GameTime(end),
        RecordedAtUtc = DateTimeOffset.UnixEpoch
    };

    private sealed class FixtureState(
        IReadOnlyList<ActivityRecord> records,
        IReadOnlyList<ActivityGap>? gaps = null)
    {
        internal TestRepository Repository { get; } = new(records, gaps ?? []);
        internal ReportDto? ExportedReport { get; private set; }
        internal ReportExportFormat? ExportedFormat { get; private set; }

        internal ReportsWorkspaceViewModel ViewModel(
            Func<long?> now,
            int offset = 0)
        {
            var service = new ReportService(Repository, new EmptyAnalyzer());
            return new ReportsWorkspaceViewModel(
                service,
                now,
                offset,
                (report, format, _) =>
                {
                    ExportedReport = report;
                    ExportedFormat = format;
                    return Task.FromResult(new ReportExportResult(true, "report.test"));
                },
                () => Task.CompletedTask);
        }
    }

    private sealed class EmptyAnalyzer : IRegulationReportAnalyzer
    {
        public RegulationReportAnalysisDto Analyze(
            GameTime now,
            IReadOnlyList<ActivityRecord> history) =>
            RegulationReportAnalysisDto.Empty;
    }

    private sealed class TestRepository(
        IReadOnlyList<ActivityRecord> records,
        IReadOnlyList<ActivityGap> gaps) : IActivityRepository
    {
        internal int CreateQueryCount { get; private set; }
        internal bool ThrowOnNextHistoryQuery { get; set; }
        internal TimeSpan QueryDelay { get; set; }

        public Task EnsureSessionAsync(
            string driverCardId,
            int sessionIndex,
            GameTime startedAt,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AppendAsync(
            string driverCardId,
            int sessionIndex,
            IReadOnlyList<ActivityRecord> added,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ApplySessionWritesAsync(
            IReadOnlyList<ActivitySessionWrite> writes,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public async Task<IReadOnlyList<ActivityRecord>> LoadDriverHistoryAsync(
            string driverCardId,
            GameTime? from = null,
            GameTime? toExclusive = null,
            CancellationToken cancellationToken = default)
        {
            CreateQueryCount++;
            if (QueryDelay > TimeSpan.Zero)
                await Task.Delay(QueryDelay, cancellationToken);
            if (ThrowOnNextHistoryQuery)
            {
                ThrowOnNextHistoryQuery = false;
                throw new InvalidOperationException("controlled report failure");
            }
            return records
                .Where(record =>
                    (from is null || record.EndExclusive > from) &&
                    (toExclusive is null || record.Start < toExclusive))
                .ToArray();
        }

        public Task<IReadOnlyList<ActivityGap>> LoadDriverGapsAsync(
            string driverCardId,
            GameTime? from = null,
            GameTime? toExclusive = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ActivityGap>>(gaps
                .Where(gap =>
                    (from is null || gap.EndExclusive is null || gap.EndExclusive > from) &&
                    (toExclusive is null || gap.Start < toExclusive))
                .ToArray());

        public Task<IReadOnlyList<StoredActivitySession>> LoadSessionsAsync(
            string driverCardId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StoredActivitySession>>([]);
    }
}

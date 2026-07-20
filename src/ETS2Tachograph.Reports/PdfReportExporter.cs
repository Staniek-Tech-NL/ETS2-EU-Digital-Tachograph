using System.Globalization;
using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.RuleEngine;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace ETS2Tachograph.Reports;

public sealed class PdfReportExporter : IPdfReportExporter
{
    private const double Left = 36;
    private const double Width = 523;
    private const double Bottom = 800;
    private readonly ReportPresentationBuilder _presentation = new();

    public Task ExportAsync(
        ReportDto report,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        GlobalFontSettings.FontResolver ??= new WindowsReportFontResolver();

        var blocks = _presentation.BuildTimelineBlocks(report.Records, report.Gaps);
        var checkpoints = _presentation.BuildCheckpoints(report.Records);
        var layouts = BuildLayouts(report, blocks, checkpoints, cancellationToken);

        using var document = new PdfDocument();
        document.Info.Title = $"Raport tachografu {report.DriverCardId}";
        document.Info.Subject = report.GapSummaryText;
        document.Info.Keywords = report.CoverageBalanceText;
        for (var index = 0; index < layouts.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DrawPage(document.AddPage(), report, layouts[index], index + 1, layouts.Count);
        }

        document.Save(destination, closeStream: false);
        return Task.CompletedTask;
    }

    private static IReadOnlyList<PageLayout> BuildLayouts(
        ReportDto report,
        IReadOnlyList<ReportTimelineBlock> blocks,
        IReadOnlyList<ReportCheckpoint> checkpoints,
        CancellationToken cancellationToken)
    {
        var pages = new List<PageLayout> { new(true) };

        PageLayout Current() => pages[^1];

        void NewPage() => pages.Add(new PageLayout(false));

        void AddSimple(RowKind kind, double height, object? data = null)
        {
            if (Current().Y + height > Bottom)
                NewPage();
            Current().Add(new LayoutRow(kind, height, data));
        }

        void StartTable(RowKind sectionKind, RowKind headerKind, double minimumBodyHeight)
        {
            const double sectionHeight = 30;
            const double headerHeight = 24;
            if (Current().Y + sectionHeight + headerHeight + minimumBodyHeight > Bottom)
                NewPage();
            Current().Add(new LayoutRow(sectionKind, sectionHeight));
            Current().Add(new LayoutRow(headerKind, headerHeight));
        }

        void ContinueTable(RowKind sectionKind, RowKind headerKind)
        {
            NewPage();
            Current().Add(new LayoutRow(sectionKind, 30, true));
            Current().Add(new LayoutRow(headerKind, 24));
        }

        StartTable(RowKind.CheckpointSection, RowKind.CheckpointHeader, checkpoints.Count == 0 ? 30 : 28);
        if (checkpoints.Count == 0)
        {
            Current().Add(new LayoutRow(RowKind.EmptyCheckpoints, 30));
        }
        else
        {
            foreach (var checkpoint in checkpoints)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (Current().Y + 28 > Bottom)
                    ContinueTable(RowKind.CheckpointSection, RowKind.CheckpointHeader);
                Current().Add(new LayoutRow(RowKind.Checkpoint, 28, checkpoint));
            }
        }

        AddSimple(RowKind.Spacer, 12);
        if (Current().Y + 30 + 18 + 24 + (blocks.Count == 0 ? 30 : 22) > Bottom)
            NewPage();
        Current().Add(new LayoutRow(RowKind.ActivitySection, 30));
        Current().Add(new LayoutRow(
            RowKind.ActivitySummary,
            18,
            $"{report.Records.Count} rekordów minutowych + {report.Gaps.Count} luk -> {blocks.Count} bloków osi czasu"));
        Current().Add(new LayoutRow(RowKind.ActivityHeader, 24));

        if (blocks.Count == 0)
        {
            Current().Add(new LayoutRow(RowKind.EmptyActivities, 30));
        }
        else
        {
            foreach (var block in blocks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (Current().Y + 22 > Bottom)
                    ContinueTable(RowKind.ActivitySection, RowKind.ActivityHeader);
                Current().Add(new LayoutRow(RowKind.Activity, 22, block));
            }
        }

        return pages;
    }

    private static void DrawPage(
        PdfPage page,
        ReportDto report,
        PageLayout layout,
        int pageNumber,
        int pageCount)
    {
        page.Size = PdfSharp.PageSize.A4;
        using var graphics = XGraphics.FromPdfPage(page);
        var regular = new XFont("ReportSans", 9, XFontStyleEx.Regular);
        var small = new XFont("ReportSans", 7.5, XFontStyleEx.Regular);
        var tiny = new XFont("ReportSans", 6.7, XFontStyleEx.Regular);
        var bold = new XFont("ReportSans", 9, XFontStyleEx.Bold);
        var section = new XFont("ReportSans", 11, XFontStyleEx.Bold);
        var title = new XFont("ReportSans", 18, XFontStyleEx.Bold);
        var dark = XColor.FromArgb(31, 38, 45);
        var accent = XColor.FromArgb(72, 200, 145);
        var pale = XColor.FromArgb(239, 243, 245);

        if (layout.IsFirstPage)
            DrawFirstPageHeader(graphics, page, report, regular, small, bold, title, dark, accent, pale);
        else
            DrawContinuationHeader(graphics, report, regular, title, dark, accent);

        var rowIndex = 0;
        foreach (var row in layout.Rows)
        {
            var y = row.Y;
            switch (row.Kind)
            {
                case RowKind.Spacer:
                    break;
                case RowKind.CheckpointSection:
                    DrawSectionTitle(graphics, y, (row.Data as bool?) == true
                        ? "PUNKTY PRZEŁOMOWE - ciąg dalszy"
                        : "PUNKTY PRZEŁOMOWE", section, accent);
                    break;
                case RowKind.CheckpointHeader:
                    DrawCheckpointHeader(graphics, y, bold, dark);
                    rowIndex = 0;
                    break;
                case RowKind.Checkpoint:
                    DrawCheckpointRow(graphics, y, (ReportCheckpoint)row.Data!, small, tiny,
                        rowIndex++ % 2 == 0 ? pale : XColors.White);
                    break;
                case RowKind.EmptyCheckpoints:
                    DrawEmptyRow(graphics, y, "Brak przerw co najmniej 15 min w zakresie raportu.", small, pale);
                    break;
                case RowKind.ActivitySection:
                    DrawSectionTitle(graphics, y, (row.Data as bool?) == true
                        ? "ZWINIĘTE BLOKI AKTYWNOŚCI - ciąg dalszy"
                        : "ZWINIĘTE BLOKI AKTYWNOŚCI", section, accent);
                    break;
                case RowKind.ActivitySummary:
                    graphics.DrawString((string)row.Data!, small, XBrushes.Gray,
                        new XRect(Left, y, Width, row.Height), XStringFormats.CenterLeft);
                    break;
                case RowKind.ActivityHeader:
                    DrawActivityHeader(graphics, y, bold, dark);
                    rowIndex = 0;
                    break;
                case RowKind.Activity:
                    DrawActivityRow(graphics, y, (ReportTimelineBlock)row.Data!, small, tiny,
                        rowIndex++ % 2 == 0 ? pale : XColors.White);
                    break;
                case RowKind.EmptyActivities:
                    DrawEmptyRow(graphics, y, "Brak aktywności w wybranym zakresie.", small, pale);
                    break;
            }
        }

        graphics.DrawString($"Strona {pageNumber}/{pageCount}", small, XBrushes.Gray,
            new XRect(Left, page.Height.Point - 35, Width, 12), XStringFormats.CenterRight);
    }

    private static void DrawFirstPageHeader(
        XGraphics graphics,
        PdfPage page,
        ReportDto report,
        XFont regular,
        XFont small,
        XFont bold,
        XFont title,
        XColor dark,
        XColor accent,
        XColor pale)
    {
        graphics.DrawRectangle(new XSolidBrush(dark), 0, 0, page.Width.Point, 92);
        graphics.DrawString("ETS2 DIGITAL TACHOGRAPH", title, XBrushes.White, new XPoint(Left, 42));
        graphics.DrawString($"Raport kierowcy - karta {report.DriverCardId}", regular,
            new XSolidBrush(accent), new XPoint(Left + 1, 67));

        var summary = new[]
        {
            ("Jazda", report.DrivingMinutes),
            ("Inna praca", report.OtherWorkMinutes),
            ("Dyspozycja", report.AvailabilityMinutes),
            ("Odpoczynek", report.RestMinutes),
            ("Naruszenia", (long)report.Violations.Count)
        };
        for (var index = 0; index < summary.Length; index++)
        {
            var x = Left + index * 105;
            graphics.DrawRectangle(new XSolidBrush(pale), x, 110, 94, 48);
            graphics.DrawString(summary[index].Item1, small, XBrushes.DimGray, new XPoint(x + 8, 127));
            var value = index == summary.Length - 1
                ? summary[index].Item2.ToString(CultureInfo.InvariantCulture)
                : FormatMinutes(summary[index].Item2);
            graphics.DrawString(value, bold, XBrushes.Black, new XPoint(x + 8, 147));
        }

        graphics.DrawString(
            $"Czas gry: {FormatGameTime(report.FromGameMinute)} - {FormatGameTime(report.ToGameMinuteExclusive)}",
            regular,
            XBrushes.Black,
            new XPoint(Left, 184));
        graphics.DrawString(
            $"Rekompensata: {FormatCompensation(report.CompensationSummary)}",
            bold,
            report.CompensationSummary.HasOverdue ? XBrushes.Red : XBrushes.Black,
            new XPoint(Left, 204));
        graphics.DrawString(
            report.GapSummaryText,
            bold,
            report.UnresolvedGapCount > 0 ? XBrushes.Red : XBrushes.Black,
            new XPoint(Left, 224));
        graphics.DrawString(
            report.CoverageBalanceText,
            small,
            report.CoverageMatchesRange ? XBrushes.DimGray : XBrushes.Red,
            new XPoint(Left, 242));
    }

    private static void DrawContinuationHeader(
        XGraphics graphics,
        ReportDto report,
        XFont regular,
        XFont title,
        XColor dark,
        XColor accent)
    {
        graphics.DrawRectangle(new XSolidBrush(dark), 0, 0, 595, 64);
        graphics.DrawString("ETS2 DIGITAL TACHOGRAPH", title, XBrushes.White, new XPoint(Left, 34));
        graphics.DrawString($"Karta {report.DriverCardId}", regular,
            new XSolidBrush(accent), new XRect(Left, 20, Width, 22), XStringFormats.CenterRight);
    }

    private static void DrawSectionTitle(XGraphics graphics, double y, string text, XFont font, XColor accent)
    {
        graphics.DrawString(text, font, XBrushes.Black,
            new XRect(Left, y, Width, 25), XStringFormats.CenterLeft);
        graphics.DrawRectangle(new XSolidBrush(accent), Left, y + 25, Width, 2);
    }

    private static void DrawCheckpointHeader(XGraphics graphics, double y, XFont font, XColor dark)
    {
        DrawHeaderCells(graphics, y, [153, 55, 105, 105, 105],
            ["Przerwa", "Czas", "Ciągła przed / po", "Dzienna przed / po", "Reset dzienny"], font, dark);
    }

    private static void DrawCheckpointRow(
        XGraphics graphics,
        double y,
        ReportCheckpoint checkpoint,
        XFont font,
        XFont tiny,
        XColor background)
    {
        graphics.DrawRectangle(new XSolidBrush(background), Left, y, Width, 28);
        var values = new[]
        {
            $"{FormatGameTime(checkpoint.Start.TotalMinutes)} - {FormatGameTime(checkpoint.EndExclusive.TotalMinutes)}",
            FormatMinutes(checkpoint.RestMinutes),
            $"{FormatMinutes(checkpoint.ContinuousDrivingBefore)} / {FormatMinutes(checkpoint.ContinuousDrivingAfter)}",
            $"{FormatMinutes(checkpoint.DailyDrivingBefore)} / {FormatMinutes(checkpoint.DailyDrivingAfter)}",
            checkpoint.DailyDrivingReset ? "TAK" : "NIE"
        };
        DrawCells(graphics, y, 28, [153, 55, 105, 105, 105], values, font, 0, tiny);
    }

    private static void DrawActivityHeader(XGraphics graphics, double y, XFont font, XColor dark)
    {
        DrawHeaderCells(graphics, y, [86, 86, 123, 174, 54],
            ["Od", "Do", "Aktywność", "Źródło", "Czas"], font, dark);
    }

    private static void DrawActivityRow(
        XGraphics graphics,
        double y,
        ReportTimelineBlock block,
        XFont font,
        XFont tiny,
        XColor background)
    {
        graphics.DrawRectangle(new XSolidBrush(background), Left, y, Width, 22);
        var values = new[]
        {
            FormatGameTime(block.Start.TotalMinutes),
            FormatGameTime(block.EndExclusive.TotalMinutes),
            block.ActivityLabel,
            block.SourceLabel,
            FormatMinutes(block.DurationMinutes)
        };
        DrawCells(graphics, y, 22, [86, 86, 123, 174, 54], values, font, 3, tiny);
    }

    private static void DrawHeaderCells(
        XGraphics graphics,
        double y,
        IReadOnlyList<double> widths,
        IReadOnlyList<string> labels,
        XFont font,
        XColor dark)
    {
        graphics.DrawRectangle(new XSolidBrush(dark), Left, y, Width, 24);
        var x = Left;
        for (var index = 0; index < widths.Count; index++)
        {
            graphics.DrawString(labels[index], font, XBrushes.White,
                new XRect(x + 5, y, widths[index] - 8, 24), XStringFormats.CenterLeft);
            x += widths[index];
        }
    }

    private static void DrawCells(
        XGraphics graphics,
        double y,
        double height,
        IReadOnlyList<double> widths,
        IReadOnlyList<string> values,
        XFont font,
        int tinyIndex,
        XFont tiny)
    {
        var x = Left;
        for (var index = 0; index < widths.Count; index++)
        {
            graphics.DrawString(values[index], index == tinyIndex ? tiny : font, XBrushes.Black,
                new XRect(x + 5, y, widths[index] - 8, height), XStringFormats.CenterLeft);
            x += widths[index];
        }
    }

    private static void DrawEmptyRow(
        XGraphics graphics,
        double y,
        string text,
        XFont font,
        XColor background)
    {
        graphics.DrawRectangle(new XSolidBrush(background), Left, y, Width, 30);
        graphics.DrawString(text, font, XBrushes.DimGray,
            new XRect(Left + 7, y, Width - 14, 30), XStringFormats.CenterLeft);
    }

    private static string FormatMinutes(long minutes) => $"{minutes / 60:00}:{minutes % 60:00}";

    private static string FormatCompensation(CompensationSummary summary)
    {
        if (summary.Count == 0)
            return "—";

        var count = summary.Count > 1 ? $" ({summary.Count})" : string.Empty;
        var status = summary.HasOverdue
            ? "PRZETERMINOWANA"
            : $"DO TYG. {summary.NearestDueByEndOfWeek!.Value.Index}";
        return $"{FormatMinutes(summary.TotalOwedMinutes)}{count} · {status}";
    }

    private static string FormatGameTime(long minutes)
    {
        var day = (minutes / 1_440) + 1;
        var minuteOfDay = minutes % 1_440;
        return $"D{day} {minuteOfDay / 60:00}:{minuteOfDay % 60:00}";
    }

    private sealed class PageLayout(bool isFirstPage)
    {
        public bool IsFirstPage { get; } = isFirstPage;
        public double Y { get; private set; } = isFirstPage ? 262 : 78;
        public List<LayoutRow> Rows { get; } = [];

        public void Add(LayoutRow row)
        {
            row.Y = Y;
            Rows.Add(row);
            Y += row.Height;
        }
    }

    private sealed class LayoutRow(RowKind kind, double height, object? data = null)
    {
        public RowKind Kind { get; } = kind;
        public double Height { get; } = height;
        public object? Data { get; } = data;
        public double Y { get; set; }
    }

    private enum RowKind
    {
        Spacer,
        CheckpointSection,
        CheckpointHeader,
        Checkpoint,
        EmptyCheckpoints,
        ActivitySection,
        ActivitySummary,
        ActivityHeader,
        Activity,
        EmptyActivities
    }

    private sealed class WindowsReportFontResolver : IFontResolver
    {
        private const string RegularFace = "ReportSans-Regular";
        private const string BoldFace = "ReportSans-Bold";

        public byte[]? GetFont(string faceName)
        {
            var fonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            return faceName switch
            {
                BoldFace => File.ReadAllBytes(Path.Combine(fonts, "arialbd.ttf")),
                RegularFace => File.ReadAllBytes(Path.Combine(fonts, "arial.ttf")),
                _ => null
            };
        }

        public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic) =>
            new(isBold ? BoldFace : RegularFace, false, isItalic);
    }
}

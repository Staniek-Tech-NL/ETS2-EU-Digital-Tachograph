using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Desktop;

public sealed record CompensationOverview(
    int OpenCount,
    string TotalDebtText,
    string NearestDueText,
    string StatusText,
    string StatusForeground)
{
    public static CompensationOverview Empty { get; } = new(
        0, "00:00", "—", "BRAK OTWARTYCH", "#5F6874");

    public static CompensationOverview From(
        IReadOnlyList<WeeklyRestCompensationDto> obligations)
    {
        var open = obligations.Where(item => item.IsOpen).ToList();
        var nearest = open.MinBy(item => item.DueAtGameMinuteExclusive);
        var status = obligations.Any(item => item.Status == WeeklyRestCompensationStatusDto.Overdue)
            ? ("ZALEGŁE", "#C43636")
            : obligations.Any(item => item.Status == WeeklyRestCompensationStatusDto.PaidLate)
                ? ("SPŁACONO PO TERMINIE", "#C46B22")
                : open.Count > 0
                    ? ("W TERMINIE", "#24754D")
                    : ("BRAK OTWARTYCH", "#5F6874");

        return new CompensationOverview(
            open.Count,
            FormatMinutes(open.Sum(item => item.RemainingMinutes)),
            nearest is null
                ? "—"
                : GameClockFormatter.Format(new GameTime(nearest.DueAtGameMinuteExclusive)),
            status.Item1,
            status.Item2);
    }

    private static string FormatMinutes(long minutes) =>
        $"{minutes / 60:00}:{minutes % 60:00}";
}

public sealed record CompensationDetailRow(
    string SlotLabel,
    string DriverCardId,
    string ObligationId,
    string ObligationIdShort,
    string SourceRestBlockId,
    string SourceRestBlockIdShort,
    string SourceRestEndText,
    string OriginalDebtText,
    string RemainingDebtText,
    string ReductionWeekText,
    long DueAtGameMinuteExclusive,
    string DueAtText,
    string StatusText,
    string StatusForeground,
    bool IsOpen,
    string? PaymentRestBlockId,
    string PaymentRestBlockIdShort,
    bool HasPaymentRestBlock,
    string PaymentRangeText,
    string SettledAtText)
{
    public static CompensationDetailRow From(
        string slotLabel,
        WeeklyRestCompensationDto obligation) => new(
            slotLabel,
            obligation.DriverCardId,
            obligation.ObligationId,
            Shorten(obligation.ObligationId),
            obligation.SourceRestBlockId,
            Shorten(obligation.SourceRestBlockId),
            GameClockFormatter.Format(new GameTime(obligation.SourceRestEndGameMinuteExclusive)),
            FormatMinutes(obligation.OriginalOwedMinutes),
            FormatMinutes(obligation.RemainingMinutes),
            $"Tydzień {obligation.ReductionWeek}",
            obligation.DueAtGameMinuteExclusive,
            GameClockFormatter.Format(new GameTime(obligation.DueAtGameMinuteExclusive)),
            StatusLabel(obligation.Status),
            StatusColor(obligation.Status),
            obligation.IsOpen,
            obligation.PaymentRestBlockId,
            obligation.PaymentRestBlockId is null ? "—" : Shorten(obligation.PaymentRestBlockId),
            obligation.PaymentRestBlockId is not null,
            obligation.PaymentRange is null
                ? "—"
                : $"{GameClockFormatter.Format(new GameTime(obligation.PaymentRange.StartGameMinute))} – " +
                  $"{GameClockFormatter.Format(new GameTime(obligation.PaymentRange.EndGameMinuteExclusive))} " +
                  $"({FormatMinutes(obligation.PaymentRange.DurationMinutes)})",
            obligation.SettledAtGameMinute is null
                ? "—"
                : GameClockFormatter.Format(new GameTime(obligation.SettledAtGameMinute.Value)));

    private static string Shorten(string value) => value.Length <= 27
        ? value
        : $"{value[..14]}…{value[^10..]}";

    private static string FormatMinutes(long minutes) =>
        $"{minutes / 60:00}:{minutes % 60:00}";

    private static string StatusLabel(WeeklyRestCompensationStatusDto status) => status switch
    {
        WeeklyRestCompensationStatusDto.OpenOnTime => "OTWARTE · W TERMINIE",
        WeeklyRestCompensationStatusDto.Overdue => "OTWARTE · ZALEGŁE",
        WeeklyRestCompensationStatusDto.PaidOnTime => "SPŁACONE W TERMINIE",
        WeeklyRestCompensationStatusDto.PaidLate => "SPŁACONE PO TERMINIE",
        _ => status.ToString().ToUpperInvariant()
    };

    private static string StatusColor(WeeklyRestCompensationStatusDto status) => status switch
    {
        WeeklyRestCompensationStatusDto.OpenOnTime => "#24754D",
        WeeklyRestCompensationStatusDto.Overdue => "#C43636",
        WeeklyRestCompensationStatusDto.PaidOnTime => "#315D8D",
        WeeklyRestCompensationStatusDto.PaidLate => "#C46B22",
        _ => "#5F6874"
    };
}

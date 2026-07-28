using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.RuleEngine;

namespace ETS2Tachograph.Desktop;

public sealed record CompensationOverview(
    int OpenCount,
    string TotalDebtText,
    string NearestDueText,
    string NearestDueCompactText,
    string StatusText,
    string StatusForeground)
{
    public static CompensationOverview Empty { get; } = new(
        0, "00:00", "—", "—", Localization.UiStrings.Get("CompensationSummary_NoOpen"), "#5F6874");

    public static CompensationOverview From(
        IReadOnlyList<WeeklyRestCompensationDto> obligations,
        GameCalendarResolver calendar)
    {
        var open = obligations.Where(item => item.IsOpen).ToList();
        var nearest = open.MinBy(item => item.DueAtGameMinuteExclusive);
        var status = obligations.Any(item => item.Status == WeeklyRestCompensationStatusDto.Overdue)
            ? (Localization.UiStrings.Get("CompensationSummary_Overdue"), "#C43636")
            : obligations.Any(item => item.Status == WeeklyRestCompensationStatusDto.PaidLate)
                ? (Localization.UiStrings.Get("CompensationSummary_PaidLate"), "#C46B22")
                : open.Count > 0
                    ? (Localization.UiStrings.Get("CompensationSummary_OnTime"), "#24754D")
                    : (Localization.UiStrings.Get("CompensationSummary_NoOpen"), "#5F6874");

        return new CompensationOverview(
            open.Count,
            FormatMinutes(open.Sum(item => item.RemainingMinutes)),
            nearest is null
                ? "—"
                : GameDeadlineFormatter.FormatFull(new DeadlinePresentation(
                    GameDeadlineSemantic.CompleteBefore,
                    calendar.Resolve(new GameTime(nearest.DueAtGameMinuteExclusive)))),
            nearest is null
                ? "—"
                : GameCalendarFormatter.FormatCompact(
                    calendar.Resolve(new GameTime(nearest.DueAtGameMinuteExclusive))),
            status.Item1,
            status.Item2);
    }

    private static string FormatMinutes(long minutes) =>
        $"{minutes / 60:00}:{minutes % 60:00}";
}

public sealed record RestAllocationChoiceRow(
    string RestBlockId,
    string RestBlockIdShort,
    string CandidateId,
    string DriverCardId,
    long EndGameMinuteExclusive,
    string RangeText,
    string PurposeText,
    string AllocationText,
    string OldDebtResultText,
    string NewDebtText,
    string WeeklyResultText)
{
    public static RestAllocationChoiceRow From(
        RestAllocationProjectionDto allocation,
        RestAllocationCandidateDto candidate,
        IReadOnlyList<WeeklyRestCompensationDto> obligations)
    {
        var compensationMinutes = obligations
            .Where(item => candidate.ObligationIds.Contains(
                item.ObligationId,
                StringComparer.Ordinal))
            .Sum(item => item.OriginalOwedMinutes);
        return new RestAllocationChoiceRow(
            allocation.RestBlockId,
            Shorten(allocation.RestBlockId),
            candidate.CandidateId,
            allocation.DriverCardId,
            allocation.EndGameMinuteExclusive,
            $"{UiGameClockFormatter.Format(new GameTime(allocation.StartGameMinute))} – " +
            UiGameClockFormatter.Format(new GameTime(allocation.EndGameMinuteExclusive)),
            PurposeLabel(candidate.Purpose),
            compensationMinutes > 0
                ? $"{FormatMinutes(candidate.HostMinimumMinutes)} + {FormatMinutes(compensationMinutes)}"
                : FormatMinutes(
                    allocation.EndGameMinuteExclusive - allocation.StartGameMinute),
            compensationMinutes > 0
                ? Localization.UiStrings.Format(
                    "RestAllocation_OldDebtPaymentFormat",
                    FormatMinutes(compensationMinutes))
                : Localization.UiStrings.Get("RestAllocation_OldDebtNoPayment"),
            candidate.NewDebtMinutes == 0
                ? Localization.UiStrings.Get("RestAllocation_NewDebtNone")
                : Localization.UiStrings.Format(
                    "RestAllocation_NewDebtFormat",
                    FormatMinutes(candidate.NewDebtMinutes)),
            candidate.SatisfiesWeeklyRestRequirement
                ? Localization.UiStrings.Get("RestAllocation_WeeklyQualified")
                : Localization.UiStrings.Get("RestAllocation_WeeklyNotQualified"));
    }

    private static string PurposeLabel(RestAllocationPurpose purpose) => purpose switch
    {
        RestAllocationPurpose.DailyRestWithCompensation =>
            Localization.UiStrings.Get("RestAllocationPurpose_DailyRestWithCompensation"),
        RestAllocationPurpose.ReducedWeeklyRestOnly =>
            Localization.UiStrings.Get("RestAllocationPurpose_ReducedWeeklyRestOnly"),
        RestAllocationPurpose.ReducedWeeklyRestWithCompensation =>
            Localization.UiStrings.Get("RestAllocationPurpose_ReducedWeeklyRestWithCompensation"),
        RestAllocationPurpose.RegularWeeklyRestOnly =>
            Localization.UiStrings.Get("RestAllocationPurpose_RegularWeeklyRestOnly"),
        RestAllocationPurpose.RegularWeeklyRestWithCompensation =>
            Localization.UiStrings.Get("RestAllocationPurpose_RegularWeeklyRestWithCompensation"),
        _ => throw new ArgumentOutOfRangeException(nameof(purpose))
    };

    private static string Shorten(string value) => value.Length <= 27
        ? value
        : $"{value[..14]}…{value[^10..]}";

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
        WeeklyRestCompensationDto obligation,
        GameCalendarResolver calendar) => new(
            slotLabel,
            obligation.DriverCardId,
            obligation.ObligationId,
            Shorten(obligation.ObligationId),
            obligation.SourceRestBlockId,
            Shorten(obligation.SourceRestBlockId),
            UiGameClockFormatter.Format(new GameTime(obligation.SourceRestEndGameMinuteExclusive)),
            FormatMinutes(obligation.OriginalOwedMinutes),
            FormatMinutes(obligation.RemainingMinutes),
            Localization.UiStrings.Format(
                "Compensation_ReductionWeekFormat",
                obligation.ReductionWeek),
            obligation.DueAtGameMinuteExclusive,
            GameDeadlineFormatter.FormatFull(new DeadlinePresentation(
                GameDeadlineSemantic.CompleteBefore,
                calendar.Resolve(new GameTime(obligation.DueAtGameMinuteExclusive)))),
            StatusLabel(obligation.Status),
            StatusColor(obligation.Status),
            obligation.IsOpen,
            obligation.PaymentRestBlockId,
            obligation.PaymentRestBlockId is null ? "—" : Shorten(obligation.PaymentRestBlockId),
            obligation.PaymentRestBlockId is not null,
            obligation.PaymentRange is null
                ? "—"
                : $"{UiGameClockFormatter.Format(new GameTime(obligation.PaymentRange.StartGameMinute))} – " +
                  $"{UiGameClockFormatter.Format(new GameTime(obligation.PaymentRange.EndGameMinuteExclusive))} " +
                  $"({FormatMinutes(obligation.PaymentRange.DurationMinutes)})",
            obligation.SettledAtGameMinute is null
                ? "—"
                : UiGameClockFormatter.Format(new GameTime(obligation.SettledAtGameMinute.Value)));

    private static string Shorten(string value) => value.Length <= 27
        ? value
        : $"{value[..14]}…{value[^10..]}";

    private static string FormatMinutes(long minutes) =>
        $"{minutes / 60:00}:{minutes % 60:00}";

    private static string StatusLabel(WeeklyRestCompensationStatusDto status) => status switch
    {
        WeeklyRestCompensationStatusDto.OpenOnTime =>
            Localization.UiStrings.Get("CompensationStatus_OpenOnTime"),
        WeeklyRestCompensationStatusDto.Overdue => Localization.UiStrings.Get("CompensationStatus_Overdue"),
        WeeklyRestCompensationStatusDto.PaidOnTime =>
            Localization.UiStrings.Get("CompensationStatus_PaidOnTime"),
        WeeklyRestCompensationStatusDto.PaidLate =>
            Localization.UiStrings.Get("CompensationStatus_PaidLate"),
        _ => throw new ArgumentOutOfRangeException(nameof(status))
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

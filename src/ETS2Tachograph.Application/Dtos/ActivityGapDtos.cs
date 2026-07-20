using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Application.Dtos;

public sealed record ActivityGapListItemDto(
    Guid GapId,
    string DriverCardId,
    int Slot,
    ActivityGapReason Reason,
    ActivityGapState State,
    long StartGameMinute,
    long? EndGameMinute,
    long DurationMinutes,
    long? ResolvedAtGameMinute)
{
    public bool IsOpen => EndGameMinute is null;
    public bool IsResolvable => State == ActivityGapState.Unresolved && !IsOpen;
    public string SlotText => $"S{Slot}";
    public string StartGameTimeText => GameClockFormatter.Format(new GameTime(StartGameMinute));
    public string EndGameTimeText => EndGameMinute is { } end
        ? GameClockFormatter.Format(new GameTime(end))
        : "TRWA";
    public string ResolvedAtGameTimeText => ResolvedAtGameMinute is { } resolvedAt
        ? GameClockFormatter.Format(new GameTime(resolvedAt))
        : string.Empty;
    public string DurationText => $"{DurationMinutes / 60:00}:{DurationMinutes % 60:00}";
    public string ReasonText => Reason switch
    {
        ActivityGapReason.ForwardTimeJump => "Skok czasu",
        ActivityGapReason.CardRemoved => "Karta wyjęta",
        ActivityGapReason.TelemetryUnavailable => "Brak telemetrii",
        _ => Reason.ToString()
    };
    public string StateText => State == ActivityGapState.Resolved
        ? $"ROZLICZONA · {ResolvedAtGameTimeText}"
        : IsOpen
            ? "TRWA"
            : "NIEROZLICZONA";
    public string OngoingHelpText => IsOpen && Reason == ActivityGapReason.CardRemoved
        ? "karta nadal wyjęta"
        : string.Empty;
    public string ActionText => IsResolvable ? "Rozlicz" : "—";
}

public sealed record ActivityGapListDto(
    IReadOnlyList<ActivityGapListItemDto> Items,
    int UnresolvedCount);

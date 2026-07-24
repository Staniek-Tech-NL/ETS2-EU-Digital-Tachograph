using System.Diagnostics.CodeAnalysis;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.RuleEngine;

/// <summary>
/// Full projection of one reduced-weekly-rest compensation obligation.
/// The canonical activity history remains the source of truth; this object is rebuilt from it.
/// </summary>
public sealed record WeeklyRestCompensation
{
    public WeeklyRestCompensation()
    {
    }

    public required int IdentitySchemeVersion { get; init; }
    public required string ObligationId { get; init; }
    public required string DriverCardId { get; init; }
    public required string SourceRestBlockId { get; init; }
    public required GameTime SourceRestEndExclusive { get; init; }
    public required long OriginalOwedMinutes { get; init; }
    public required long RemainingMinutes { get; init; }
    public required GameWeek ReductionWeek { get; init; }
    public required GameTime DueAtExclusive { get; init; }
    public string? PaymentRestBlockId { get; init; }
    public CompensationMinuteRange? PaymentRange { get; init; }
    public GameTime? SettledAt { get; init; }
    public required WeeklyRestCompensationStatus Status { get; init; }

    /// <summary>Compatibility alias for consumers that display the outstanding amount.</summary>
    public long OwedMinutes => RemainingMinutes;

    /// <summary>Compatibility projection of the last week before the exclusive deadline.</summary>
    public GameWeek DueByEndOfWeek => new(ReductionWeek.Index + 3);

    /// <summary>True for an unpaid overdue debt and for a debt eventually paid late.</summary>
    public bool IsOverdue => Status is
        WeeklyRestCompensationStatus.Overdue or
        WeeklyRestCompensationStatus.PaidLate;

    public bool IsOpen => Status is
        WeeklyRestCompensationStatus.OpenOnTime or
        WeeklyRestCompensationStatus.Overdue;

    /// <summary>
    /// Compatibility constructor retained for existing presentation tests. New RuleEngine code
    /// must use the complete contract and deterministic identifiers.
    /// </summary>
    [SetsRequiredMembers]
    public WeeklyRestCompensation(
        long owedMinutes,
        GameWeek reductionWeek,
        GameWeek dueByEndOfWeek,
        bool isOverdue)
    {
        IdentitySchemeVersion = 0;
        ObligationId = $"legacy:{reductionWeek.Index}:{owedMinutes}";
        DriverCardId = "legacy";
        SourceRestBlockId = $"legacy-rest:{reductionWeek.Index}";
        SourceRestEndExclusive = new GameTime(0);
        OriginalOwedMinutes = owedMinutes;
        RemainingMinutes = owedMinutes;
        ReductionWeek = reductionWeek;
        DueAtExclusive = new GameTime(
            dueByEndOfWeek.GetBounds().EndGameMinuteExclusive);
        Status = isOverdue
            ? WeeklyRestCompensationStatus.Overdue
            : WeeklyRestCompensationStatus.OpenOnTime;
    }
}

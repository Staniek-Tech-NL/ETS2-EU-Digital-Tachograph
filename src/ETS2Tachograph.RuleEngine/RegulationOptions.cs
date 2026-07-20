namespace ETS2Tachograph.RuleEngine;

public sealed record RegulationOptions
{
    public bool MultiManning { get; init; }
    public int WeekEpochOffsetDays { get; init; }
}

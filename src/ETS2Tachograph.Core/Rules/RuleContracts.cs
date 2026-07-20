using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Core.Rules;

public interface IRegulationRule
{
    IReadOnlyList<RuleFinding> Evaluate(RuleContext context);
}

public sealed record RuleContext(
    GameTime Now,
    IReadOnlyList<ActivityRecord> History);

public sealed record RuleFinding(
    string RuleCode,
    RuleFindingLevel Level,
    string Message,
    long? RemainingMinutes = null);

public enum RuleFindingLevel
{
    Information = 0,
    Warning = 1,
    Violation = 2
}

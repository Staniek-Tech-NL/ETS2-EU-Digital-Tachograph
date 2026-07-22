using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.RuleEngine;

public sealed record CompensationMinuteRange
{
    public CompensationMinuteRange(GameTime start, GameTime endExclusive)
    {
        if (endExclusive < start)
            throw new ArgumentOutOfRangeException(
                nameof(endExclusive),
                "Compensation range cannot end before it starts.");

        Start = start;
        EndExclusive = endExclusive;
    }

    public GameTime Start { get; }
    public GameTime EndExclusive { get; }
    public long DurationMinutes => EndExclusive - Start;
}

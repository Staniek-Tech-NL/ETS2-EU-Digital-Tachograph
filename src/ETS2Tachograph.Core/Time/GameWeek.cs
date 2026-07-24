namespace ETS2Tachograph.Core.Time;

public readonly record struct GameWeek(long Index)
{
    public const int MinutesPerDay = 1_440;
    public const int MinutesPerWeek = 10_080;

    public static GameWeek From(GameTime time, int weekEpochOffsetDays = 0)
    {
        var calibrated = checked(time.TotalMinutes + ((long)weekEpochOffsetDays * MinutesPerDay));
        return new GameWeek(FloorDivide(calibrated, MinutesPerWeek));
    }

    /// <summary>
    /// Returns the canonical half-open regulatory-week interval. Its start is
    /// Monday 00:00 in the game calendar calibrated by the raw epoch offset.
    /// </summary>
    public GameWeekBounds GetBounds(int weekEpochOffsetDays = 0)
    {
        var start = checked(
            (Index * MinutesPerWeek) -
            ((long)weekEpochOffsetDays * MinutesPerDay));
        return new GameWeekBounds(start, checked(start + MinutesPerWeek));
    }

    private static long FloorDivide(long value, long divisor)
    {
        var quotient = value / divisor;
        var remainder = value % divisor;
        return remainder < 0 ? quotient - 1 : quotient;
    }
}

public readonly record struct GameWeekBounds(
    long StartGameMinute,
    long EndGameMinuteExclusive)
{
    public bool Contains(GameTime time) =>
        time.TotalMinutes >= StartGameMinute &&
        time.TotalMinutes < EndGameMinuteExclusive;
}

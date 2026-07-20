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

    private static long FloorDivide(long value, long divisor)
    {
        var quotient = value / divisor;
        var remainder = value % divisor;
        return remainder < 0 ? quotient - 1 : quotient;
    }
}

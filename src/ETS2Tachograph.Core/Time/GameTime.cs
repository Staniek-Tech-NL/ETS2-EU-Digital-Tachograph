namespace ETS2Tachograph.Core.Time;

/// <summary>A monotonic minute on the calibrated ETS2 game clock.</summary>
public readonly record struct GameTime : IComparable<GameTime>
{
    public GameTime(long totalMinutes)
    {
        if (totalMinutes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalMinutes));
        }

        TotalMinutes = totalMinutes;
    }

    public long TotalMinutes { get; }

    public GameTime AddMinutes(long minutes) => new(checked(TotalMinutes + minutes));

    public int CompareTo(GameTime other) => TotalMinutes.CompareTo(other.TotalMinutes);

    public static long operator -(GameTime left, GameTime right) => left.TotalMinutes - right.TotalMinutes;
    public static bool operator <(GameTime left, GameTime right) => left.TotalMinutes < right.TotalMinutes;
    public static bool operator >(GameTime left, GameTime right) => left.TotalMinutes > right.TotalMinutes;
    public static bool operator <=(GameTime left, GameTime right) => left.TotalMinutes <= right.TotalMinutes;
    public static bool operator >=(GameTime left, GameTime right) => left.TotalMinutes >= right.TotalMinutes;
}

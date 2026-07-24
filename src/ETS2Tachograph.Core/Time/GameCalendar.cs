namespace ETS2Tachograph.Core.Time;

public enum GameWeekday
{
    Monday = 0,
    Tuesday = 1,
    Wednesday = 2,
    Thursday = 3,
    Friday = 4,
    Saturday = 5,
    Sunday = 6
}

public enum GameDeadlineSemantic
{
    CompleteBy,
    StartNoLaterThan,
    CompleteBefore,
    AvailableFrom
}

public readonly record struct GameCalendarContext
{
    public GameCalendarContext(int weekEpochOffsetDays)
    {
        if (weekEpochOffsetDays is < -6 or > 6)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weekEpochOffsetDays),
                "Week epoch offset must be between -6 and 6 days.");
        }

        WeekEpochOffsetDays = weekEpochOffsetDays;
    }

    public int WeekEpochOffsetDays { get; }
}

public readonly record struct GameCalendarMoment(
    GameTime GameTime,
    GameWeek Week,
    GameWeekBounds WeekBounds,
    GameWeekday Weekday,
    long DisplayedGameDay,
    int Hour,
    int Minute);

public sealed class GameCalendarResolver(GameCalendarContext context)
{
    public GameCalendarContext Context { get; } = context;

    public GameCalendarMoment Resolve(GameTime gameTime)
    {
        var week = GameWeek.From(gameTime, Context.WeekEpochOffsetDays);
        var bounds = week.GetBounds(Context.WeekEpochOffsetDays);
        var dayIndex = (gameTime.TotalMinutes - bounds.StartGameMinute) /
            GameWeek.MinutesPerDay;
        if (dayIndex is < 0 or > 6)
        {
            throw new InvalidOperationException(
                "Resolved game time is outside its canonical game week.");
        }

        var minuteOfDay = gameTime.TotalMinutes % GameWeek.MinutesPerDay;
        return new GameCalendarMoment(
            gameTime,
            week,
            bounds,
            (GameWeekday)dayIndex,
            (gameTime.TotalMinutes / GameWeek.MinutesPerDay) + 1,
            (int)(minuteOfDay / 60),
            (int)(minuteOfDay % 60));
    }
}

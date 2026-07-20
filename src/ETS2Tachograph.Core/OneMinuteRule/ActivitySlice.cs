using ETS2Tachograph.Core.Enums;

namespace ETS2Tachograph.Core.OneMinuteRule;

/// <summary>An ordered, continuous activity fragment within one calendar minute.</summary>
public sealed record ActivitySlice
{
    public ActivitySlice(DriverActivity activity, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        Activity = activity;
        Duration = duration;
    }

    public DriverActivity Activity { get; }
    public TimeSpan Duration { get; }
}

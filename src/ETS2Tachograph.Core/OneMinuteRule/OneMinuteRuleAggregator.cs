using ETS2Tachograph.Core.Enums;

namespace ETS2Tachograph.Core.OneMinuteRule;

/// <summary>
/// Applies Annex 1C requirements 051 and 052 while retaining one provisional minute.
/// </summary>
public sealed class OneMinuteRuleAggregator
{
    private MinuteActivity? _previous;
    private MinuteActivity? _pending;

    public MinuteActivity? ProvisionalActivity => _pending;

    public AggregatedMinute? Push(MinuteActivity minute)
    {
        ArgumentNullException.ThrowIfNull(minute);

        if (_pending is null)
        {
            _pending = minute;
            return null;
        }

        if (minute.Minute.TotalMinutes != _pending.Minute.TotalMinutes + 1)
        {
            throw new InvalidOperationException("Minutes must be supplied consecutively.");
        }

        var completed = CompletePending(minute);
        _previous = _pending;
        _pending = minute;
        return completed;
    }

    public AggregatedMinute? Flush()
    {
        if (_pending is null)
        {
            return null;
        }

        // No succeeding minute is known, therefore requirement 051 cannot apply.
        var completed = new AggregatedMinute(
            _pending.Minute,
            _pending.LongestContinuousActivity,
            DrivingPrecedenceApplied: false,
            _pending.Source,
            _pending.Condition);
        Reset();
        return completed;
    }

    public void Reset()
    {
        _previous = null;
        _pending = null;
    }

    private AggregatedMinute CompletePending(MinuteActivity next)
    {
        var applyDrivingPrecedence =
            _previous?.LongestContinuousActivity == DriverActivity.Driving &&
            next.LongestContinuousActivity == DriverActivity.Driving;

        return new AggregatedMinute(
            _pending!.Minute,
            applyDrivingPrecedence ? DriverActivity.Driving : _pending.LongestContinuousActivity,
            applyDrivingPrecedence,
            _pending.Source,
            _pending.Condition);
    }
}

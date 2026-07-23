using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Desktop;

public sealed record ManualEntryActivityOption(
    DriverActivity Activity,
    string DisplayName);

public sealed record ManualEntryDayOption(long DayNumber)
{
    public string DisplayName => $"Dzień {DayNumber}";
}

public sealed record ManualEntrySegmentRow(
    long FromGameMinute,
    long ToGameMinuteExclusive,
    DriverActivity Activity)
{
    public string FromText => GameClockFormatter.Format(new GameTime(FromGameMinute));
    public string ToText => GameClockFormatter.Format(new GameTime(ToGameMinuteExclusive));
    public string ActivityText => ManualEntryPlanEditor.ActivityLabel(Activity);
    public string ActivityBackground => Activity switch
    {
        DriverActivity.BreakOrRest => "#194D46",
        DriverActivity.OtherWork => "#214E78",
        DriverActivity.Availability => "#403957",
        _ => "#202C3D"
    };
    public string DurationText => ManualEntryPlanEditor.FormatDuration(
        ToGameMinuteExclusive - FromGameMinute);
    public bool CanDelete => Activity != DriverActivity.BreakOrRest;
}

/// <summary>
/// Mutable working copy used by the manual-entry dialog. It never writes
/// history; persistence still goes through ManualEntryService after approval.
/// </summary>
public sealed class ManualEntryPlanEditor
{
    private readonly List<ManualEntrySegmentRow> _segments = [];

    public ManualEntryPlanEditor(long gapStart, long gapEndExclusive)
    {
        if (gapStart < 0)
            throw new ArgumentOutOfRangeException(nameof(gapStart));
        if (gapEndExclusive <= gapStart)
            throw new ArgumentOutOfRangeException(
                nameof(gapEndExclusive),
                "Luka musi mieć dodatnią długość.");

        GapStart = gapStart;
        GapEndExclusive = gapEndExclusive;
        Reset(DriverActivity.BreakOrRest);
    }

    public long GapStart { get; }
    public long GapEndExclusive { get; }
    public long GapDuration => GapEndExclusive - GapStart;
    public IReadOnlyList<ManualEntrySegmentRow> Segments => _segments;

    public long RestMinutes => Sum(DriverActivity.BreakOrRest);
    public long OtherWorkMinutes => Sum(DriverActivity.OtherWork);
    public long AvailabilityMinutes => Sum(DriverActivity.Availability);
    public long CoveredMinutes => _segments.Sum(segment =>
        segment.ToGameMinuteExclusive - segment.FromGameMinute);
    public long UnassignedMinutes => Math.Max(0, GapDuration - CoveredMinutes);
    public bool IsComplete =>
        _segments.Count > 0 &&
        _segments[0].FromGameMinute == GapStart &&
        _segments[^1].ToGameMinuteExclusive == GapEndExclusive &&
        _segments.Zip(_segments.Skip(1)).All(pair =>
            pair.First.ToGameMinuteExclusive == pair.Second.FromGameMinute) &&
        CoveredMinutes == GapDuration;

    public void Reset(DriverActivity activity)
    {
        EnsureAllowed(activity);
        _segments.Clear();
        _segments.Add(new ManualEntrySegmentRow(GapStart, GapEndExclusive, activity));
    }

    public void Replace(
        long fromGameMinute,
        long toGameMinuteExclusive,
        DriverActivity activity)
    {
        ValidateRange(fromGameMinute, toGameMinuteExclusive);
        EnsureAllowed(activity);

        var result = new List<ManualEntrySegmentRow>();
        foreach (var segment in _segments)
        {
            if (segment.ToGameMinuteExclusive <= fromGameMinute ||
                segment.FromGameMinute >= toGameMinuteExclusive)
            {
                result.Add(segment);
                continue;
            }

            if (segment.FromGameMinute < fromGameMinute)
            {
                result.Add(segment with
                {
                    ToGameMinuteExclusive = fromGameMinute
                });
            }

            if (segment.ToGameMinuteExclusive > toGameMinuteExclusive)
            {
                result.Add(segment with
                {
                    FromGameMinute = toGameMinuteExclusive
                });
            }
        }

        result.Add(new ManualEntrySegmentRow(
            fromGameMinute,
            toGameMinuteExclusive,
            activity));
        ReplaceWithNormalized(result);
    }

    public void Edit(
        ManualEntrySegmentRow original,
        long fromGameMinute,
        long toGameMinuteExclusive,
        DriverActivity activity)
    {
        ArgumentNullException.ThrowIfNull(original);
        if (!_segments.Contains(original))
            throw new InvalidOperationException("Edytowany segment nie należy już do planu.");
        ValidateRange(fromGameMinute, toGameMinuteExclusive);
        EnsureAllowed(activity);

        if (original.Activity != DriverActivity.BreakOrRest)
        {
            Replace(
                original.FromGameMinute,
                original.ToGameMinuteExclusive,
                DriverActivity.BreakOrRest);
        }

        Replace(fromGameMinute, toGameMinuteExclusive, activity);
    }

    public void Remove(ManualEntrySegmentRow segment)
    {
        ArgumentNullException.ThrowIfNull(segment);
        if (!_segments.Contains(segment))
            throw new InvalidOperationException("Usuwany segment nie należy już do planu.");
        if (!segment.CanDelete)
            throw new InvalidOperationException("Odpoczynek jest domyślnym wypełnieniem luki.");

        Replace(
            segment.FromGameMinute,
            segment.ToGameMinuteExclusive,
            DriverActivity.BreakOrRest);
    }

    public IReadOnlyList<ManualEntrySegment> ToSegments() =>
        _segments.Select(segment => new ManualEntrySegment(
            segment.FromGameMinute,
            segment.ToGameMinuteExclusive,
            segment.Activity)).ToList();

    public static string ActivityLabel(DriverActivity activity) => activity switch
    {
        DriverActivity.BreakOrRest => "Przerwa / Odpoczynek",
        DriverActivity.OtherWork => "Inna praca",
        DriverActivity.Availability => "Dyspozycyjność",
        _ => activity.ToString()
    };

    public static string FormatDuration(long minutes) =>
        $"{minutes / 60:00}:{minutes % 60:00}";

    private long Sum(DriverActivity activity) => _segments
        .Where(segment => segment.Activity == activity)
        .Sum(segment => segment.ToGameMinuteExclusive - segment.FromGameMinute);

    private void ValidateRange(long fromGameMinute, long toGameMinuteExclusive)
    {
        if (toGameMinuteExclusive <= fromGameMinute)
            throw new InvalidOperationException("Początek segmentu musi być wcześniejszy niż koniec.");
        if (fromGameMinute < GapStart || toGameMinuteExclusive > GapEndExclusive)
            throw new InvalidOperationException("Zakres segmentu musi mieścić się w rozliczanej luce.");
    }

    private static void EnsureAllowed(DriverActivity activity)
    {
        if (activity is not (
            DriverActivity.BreakOrRest or
            DriverActivity.OtherWork or
            DriverActivity.Availability))
        {
            throw new InvalidOperationException(
                "Ta aktywność nie jest dostępna we wpisie manualnym.");
        }
    }

    private void ReplaceWithNormalized(IEnumerable<ManualEntrySegmentRow> segments)
    {
        var ordered = segments
            .Where(segment =>
                segment.ToGameMinuteExclusive > segment.FromGameMinute)
            .OrderBy(segment => segment.FromGameMinute)
            .ThenBy(segment => segment.ToGameMinuteExclusive)
            .ToList();
        var normalized = new List<ManualEntrySegmentRow>();

        foreach (var segment in ordered)
        {
            if (normalized.Count > 0 &&
                normalized[^1].ToGameMinuteExclusive == segment.FromGameMinute &&
                normalized[^1].Activity == segment.Activity)
            {
                normalized[^1] = normalized[^1] with
                {
                    ToGameMinuteExclusive = segment.ToGameMinuteExclusive
                };
            }
            else
            {
                normalized.Add(segment);
            }
        }

        _segments.Clear();
        _segments.AddRange(normalized);
        if (!IsComplete)
            throw new InvalidOperationException("Plan wpisu nie pokrywa całej luki.");
    }
}

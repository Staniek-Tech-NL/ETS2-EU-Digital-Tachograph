using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Rules;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.RuleEngine;

namespace ETS2Tachograph.Reports;

public sealed record ReportActivityBlock(
    GameTime Start,
    GameTime EndExclusive,
    DriverActivity Activity,
    IReadOnlySet<ActivitySource> Sources,
    IReadOnlySet<SpecialCondition> Conditions)
{
    public long DurationMinutes => EndExclusive - Start;

    public string SourceLabel
    {
        get
        {
            string label;
            if (Sources.Count == 1)
            {
                label = SourceName(Sources.Single());
            }
            else if (Sources.SetEquals([ActivitySource.Telemetry, ActivitySource.Reconstructed]))
            {
                label = "Telemetria / częściowo rekonstruowana";
            }
            else if (Sources.Contains(ActivitySource.Reconstructed))
            {
                label = "Mieszane / częściowo rekonstruowane";
            }
            else
            {
                label = "Mieszane";
            }

            if (Conditions.SetEquals([SpecialCondition.FerryCrossing]))
                return $"{label} - prom";
            if (Conditions.Count > 1 || Conditions.Contains(SpecialCondition.Mixed))
                return $"{label} - tryb mieszany";
            return label;
        }
    }

    private static string SourceName(ActivitySource source) => source switch
    {
        ActivitySource.Telemetry => "Telemetria",
        ActivitySource.Manual => "Ręczne",
        ActivitySource.Reconstructed => "Rekonstruowane",
        ActivitySource.Mixed => "Mieszane",
        ActivitySource.ManualEntry => "Wpis manualny",
        ActivitySource.AutomaticCrewReconstruction => "Automatyczna rekonstrukcja załogi",
        _ => source.ToString()
    };
}

public sealed record ReportTimelineBlock(
    GameTime Start,
    GameTime EndExclusive,
    ReportActivityBlock? Activity,
    ActivityGap? Gap)
{
    public long DurationMinutes => EndExclusive - Start;
    public bool IsGap => Gap is not null;
    public string ActivityLabel => Gap is null
        ? ActivityName(Activity!.Activity)
        : Gap.Reason switch
        {
            ActivityGapReason.ForwardTimeJump => "Brak danych — skok czasu",
            ActivityGapReason.CardRemoved => "Brak danych — karta wyjęta",
            ActivityGapReason.TelemetryUnavailable => "Brak danych — telemetria",
            _ => "Brak danych"
        };
    public string SourceLabel => Gap is null ? Activity!.SourceLabel : "Luka aktywności";

    private static string ActivityName(DriverActivity activity) => activity switch
    {
        DriverActivity.Driving => "Jazda",
        DriverActivity.OtherWork => "Inna praca",
        DriverActivity.Availability => "Dyspozycja",
        DriverActivity.BreakOrRest => "Przerwa / odpoczynek",
        DriverActivity.OutOfScope => "Poza zakresem (OUT)",
        _ => activity.ToString()
    };
}

public sealed record ReportCheckpoint(
    GameTime Start,
    GameTime EndExclusive,
    long RestMinutes,
    long ContinuousDrivingBefore,
    long ContinuousDrivingAfter,
    long DailyDrivingBefore,
    long DailyDrivingAfter)
{
    public bool ContinuousDrivingReset =>
        ContinuousDrivingBefore > 0 && ContinuousDrivingAfter < ContinuousDrivingBefore;

    public bool DailyDrivingReset =>
        DailyDrivingBefore > 0 && DailyDrivingAfter < DailyDrivingBefore;
}

/// <summary>
/// Builds human-readable report projections without changing the minute-level source data.
/// </summary>
public sealed class ReportPresentationBuilder(RegulationEngine? regulationEngine = null)
{
    private readonly RegulationEngine _regulationEngine = regulationEngine ?? new RegulationEngine();

    public IReadOnlyList<ReportActivityBlock> BuildBlocks(IReadOnlyList<ActivityRecord> records)
    {
        var blocks = new List<MutableBlock>();
        foreach (var record in records.OrderBy(record => record.Start))
        {
            if (blocks.Count > 0 &&
                blocks[^1].Activity == record.Activity &&
                blocks[^1].EndExclusive == record.Start)
            {
                blocks[^1].EndExclusive = record.EndExclusive;
                blocks[^1].Sources.Add(record.Source);
                blocks[^1].Conditions.Add(record.Condition);
                continue;
            }

            blocks.Add(new MutableBlock(
                record.Start,
                record.EndExclusive,
                record.Activity,
                [record.Source],
                [record.Condition]));
        }

        return blocks.Select(block => new ReportActivityBlock(
            block.Start,
            block.EndExclusive,
            block.Activity,
            new HashSet<ActivitySource>(block.Sources),
            new HashSet<SpecialCondition>(block.Conditions))).ToList();
    }

    public IReadOnlyList<ReportTimelineBlock> BuildTimelineBlocks(
        IReadOnlyList<ActivityRecord> records,
        IReadOnlyList<ActivityGap> gaps)
    {
        var activities = BuildBlocks(records).Select(block => new ReportTimelineBlock(
            block.Start,
            block.EndExclusive,
            block,
            null));
        var gapBlocks = gaps
            .Where(gap =>
                gap.State == ActivityGapState.Unresolved &&
                gap.EndExclusive is not null &&
                gap.EndExclusive.Value > gap.Start)
            .Select(gap => new ReportTimelineBlock(
                gap.Start,
                gap.EndExclusive!.Value,
                null,
                gap));
        return activities.Concat(gapBlocks)
            .OrderBy(block => block.Start)
            .ThenBy(block => block.IsGap ? 0 : 1)
            .ToList();
    }

    public IReadOnlyList<ReportCheckpoint> BuildCheckpoints(IReadOnlyList<ActivityRecord> records)
    {
        var ordered = records.OrderBy(record => record.Start).ToList();
        var restBlocks = BuildBlocks(ordered)
            .Where(block => block.Activity == DriverActivity.BreakOrRest)
            .ToList();
        var checkpoints = new List<ReportCheckpoint>();

        foreach (var rest in restBlocks)
        {
            var before = EvaluateAt(ordered, rest.Start);
            var after = EvaluateAt(ordered, rest.EndExclusive);
            var dailyDrivingAfter = rest.DurationMinutes >= 540
                ? 0
                : after.State.DailyDrivingMinutes;
            var checkpoint = new ReportCheckpoint(
                rest.Start,
                rest.EndExclusive,
                rest.DurationMinutes,
                before.State.ContinuousDrivingMinutes,
                after.State.ContinuousDrivingMinutes,
                before.State.DailyDrivingMinutes,
                dailyDrivingAfter);

            // Ignore accidental one-minute mode changes, but retain every potential
            // split break and every event that actually reset a counter.
            if (rest.DurationMinutes >= 15 ||
                checkpoint.ContinuousDrivingReset ||
                checkpoint.DailyDrivingReset)
                checkpoints.Add(checkpoint);
        }

        return checkpoints;
    }

    private RegulationEvaluation EvaluateAt(
        IReadOnlyList<ActivityRecord> records,
        GameTime endExclusive)
    {
        var history = records
            .Where(record => record.Start < endExclusive)
            .Select(record => record.EndExclusive <= endExclusive
                ? record
                : record with { EndExclusive = endExclusive })
            .Where(record => record.EndExclusive > record.Start)
            .ToList();
        return _regulationEngine.Evaluate(new RuleContext(endExclusive, history));
    }

    private sealed class MutableBlock(
        GameTime start,
        GameTime endExclusive,
        DriverActivity activity,
        HashSet<ActivitySource> sources,
        HashSet<SpecialCondition> conditions)
    {
        public GameTime Start { get; } = start;
        public GameTime EndExclusive { get; set; } = endExclusive;
        public DriverActivity Activity { get; } = activity;
        public HashSet<ActivitySource> Sources { get; } = sources;
        public HashSet<SpecialCondition> Conditions { get; } = conditions;
    }
}

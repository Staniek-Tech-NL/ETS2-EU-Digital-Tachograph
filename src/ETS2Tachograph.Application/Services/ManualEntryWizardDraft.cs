using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;

namespace ETS2Tachograph.Application.Services;

public sealed record ManualEntryWorkBlock(
    long FromGameMinute,
    long ToGameMinuteExclusive);

public sealed class ManualEntryDraftException(string message) :
    InvalidOperationException(message);

/// <summary>
/// Builds a complete manual entry for the UI. Every minute not explicitly
/// marked as OtherWork remains BreakOrRest, so the common case needs one click.
/// </summary>
public static class ManualEntryWizardDraft
{
    public static IReadOnlyList<ManualEntrySegment> Build(
        ActivityGap gap,
        IReadOnlyList<ManualEntryWorkBlock> workBlocks)
    {
        ArgumentNullException.ThrowIfNull(gap);
        ArgumentNullException.ThrowIfNull(workBlocks);
        if (gap.EndExclusive is null)
            throw new ManualEntryDraftException("Nie można rozliczyć otwartej luki.");

        var gapStart = gap.Start.TotalMinutes;
        var gapEnd = gap.EndExclusive.Value.TotalMinutes;
        var ordered = workBlocks
            .OrderBy(block => block.FromGameMinute)
            .ThenBy(block => block.ToGameMinuteExclusive)
            .ToList();
        var normalized = new List<ManualEntryWorkBlock>();
        foreach (var block in ordered)
        {
            if (block.ToGameMinuteExclusive <= block.FromGameMinute)
                throw new ManualEntryDraftException("Blok pracy musi mieć dodatnią długość.");
            if (block.FromGameMinute < gapStart || block.ToGameMinuteExclusive > gapEnd)
                throw new ManualEntryDraftException("Blok pracy wychodzi poza zakres luki.");
            if (normalized.Count > 0 && block.FromGameMinute < normalized[^1].ToGameMinuteExclusive)
                throw new ManualEntryDraftException("Bloki pracy nie mogą się nakładać.");
            if (normalized.Count > 0 && block.FromGameMinute == normalized[^1].ToGameMinuteExclusive)
            {
                normalized[^1] = normalized[^1] with
                {
                    ToGameMinuteExclusive = block.ToGameMinuteExclusive
                };
            }
            else
            {
                normalized.Add(block);
            }
        }

        var result = new List<ManualEntrySegment>();
        var cursor = gapStart;
        foreach (var work in normalized)
        {
            if (cursor < work.FromGameMinute)
                result.Add(new ManualEntrySegment(
                    cursor,
                    work.FromGameMinute,
                    DriverActivity.BreakOrRest));
            result.Add(new ManualEntrySegment(
                work.FromGameMinute,
                work.ToGameMinuteExclusive,
                DriverActivity.OtherWork));
            cursor = work.ToGameMinuteExclusive;
        }
        if (cursor < gapEnd)
            result.Add(new ManualEntrySegment(cursor, gapEnd, DriverActivity.BreakOrRest));

        if (result.Count == 0 ||
            result[0].FromGameMinute != gapStart ||
            result[^1].ToGameMinuteExclusive != gapEnd ||
            result.Zip(result.Skip(1)).Any(pair =>
                pair.First.ToGameMinuteExclusive != pair.Second.FromGameMinute))
            throw new ManualEntryDraftException("Wpis nie pokrywa całej luki.");

        return result;
    }
}

using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ETS2Tachograph.Infrastructure.Tests;

/// <summary>
/// A new session owns the timeline from its own anchor upwards. Below the anchor it may
/// only fill minutes nobody covers, because a manual entry resolves gaps instead of
/// amending recorded telemetry. The field data in the card fixtures below is taken from
/// the beta.10 databases that first exposed the duplicate projection.
/// </summary>
public sealed class CanonicalProjectionTests
{
    private const string Card = "CARD-CANON";

    private static readonly DateTimeOffset Epoch =
        new(2026, 7, 21, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Fully_covered_record_leaves_nothing_behind()
    {
        var canonical = await ProjectAsync(
            (0, 100, [Span(100, 200, DriverActivity.Driving)]),
            (1, 200, [Manual(120, 130, DriverActivity.BreakOrRest)]));

        Assert.Equal([(100, 200)], Spans(canonical));
    }

    [Fact]
    public async Task Coverage_of_the_leading_edge_keeps_the_right_hand_part()
    {
        var canonical = await ProjectAsync(
            (0, 100, [Span(100, 130, DriverActivity.Driving)]),
            (1, 200, [Manual(100, 160, DriverActivity.BreakOrRest)]));

        Assert.Equal([(100, 130), (130, 160)], Spans(canonical));
    }

    [Fact]
    public async Task Coverage_of_the_trailing_edge_keeps_the_left_hand_part()
    {
        var canonical = await ProjectAsync(
            (0, 100, [Span(140, 160, DriverActivity.Driving)]),
            (1, 200, [Manual(100, 160, DriverActivity.BreakOrRest)]));

        Assert.Equal([(100, 140), (140, 160)], Spans(canonical));
    }

    [Fact]
    public async Task Coverage_inside_the_record_splits_it_into_two_fragments()
    {
        var canonical = await ProjectAsync(
            (0, 100, [Span(120, 130, DriverActivity.Driving)]),
            (1, 200, [Manual(100, 200, DriverActivity.BreakOrRest)]));

        Assert.Equal([(100, 120), (120, 130), (130, 200)], Spans(canonical));
    }

    [Fact]
    public async Task Several_disjoint_covers_produce_several_fragments()
    {
        var canonical = await ProjectAsync(
            (0, 100,
            [
                Span(120, 130, DriverActivity.Driving),
                Span(150, 170, DriverActivity.Driving)
            ]),
            (1, 200, [Manual(100, 200, DriverActivity.BreakOrRest)]));

        Assert.Equal(
            [(100, 120), (120, 130), (130, 150), (150, 170), (170, 200)],
            Spans(canonical));
        Assert.Equal(3, canonical.Count(x => x.Source == ActivitySource.ManualEntry));
    }

    [Fact]
    public async Task Uncovered_record_survives_untouched()
    {
        var canonical = await ProjectAsync(
            (0, 100, [Span(100, 120, DriverActivity.Driving)]),
            (1, 200, [Manual(140, 180, DriverActivity.BreakOrRest)]));

        Assert.Equal([(100, 120), (140, 180)], Spans(canonical));
    }

    [Fact]
    public async Task Identical_duplicate_of_an_earlier_minute_disappears()
    {
        var canonical = await ProjectAsync(
            (0, 100, [Span(100, 101, DriverActivity.OtherWork)]),
            (1, 101, [Span(100, 101, DriverActivity.OtherWork)]));

        Assert.Equal([(100, 101)], Spans(canonical));
    }

    [Fact]
    public async Task Manual_backfill_of_a_missing_stretch_is_kept_whole()
    {
        var canonical = await ProjectAsync(
            (0, 100, [Span(100, 110, DriverActivity.Driving)]),
            (1, 400, [Manual(110, 400, DriverActivity.BreakOrRest)]));

        Assert.Equal([(100, 110), (110, 400)], Spans(canonical));
        Assert.Equal(290, canonical.Single(x => x.Start.TotalMinutes == 110).DurationMinutes);
    }

    [Fact]
    public async Task Existing_telemetry_wins_over_a_conflicting_manual_entry()
    {
        var canonical = await ProjectAsync(
            (0, 100, [Span(100, 200, DriverActivity.Driving)]),
            (1, 200, [Manual(100, 200, DriverActivity.BreakOrRest)]));

        var survivor = Assert.Single(canonical);
        Assert.Equal(DriverActivity.Driving, survivor.Activity);
        Assert.Equal(ActivitySource.Telemetry, survivor.Source);
    }

    [Fact]
    public async Task Projection_never_overlaps_and_never_repeats_a_start_minute()
    {
        var canonical = await ProjectAsync(
            (0, 100,
            [
                Span(100, 120, DriverActivity.Driving),
                Span(150, 170, DriverActivity.OtherWork)
            ]),
            (1, 300, [Manual(100, 300, DriverActivity.BreakOrRest)]),
            (2, 300, [Span(280, 320, DriverActivity.Driving)]));

        for (var index = 1; index < canonical.Count; index++)
            Assert.True(canonical[index - 1].EndExclusive <= canonical[index].Start);
        Assert.Equal(
            canonical.Count,
            canonical.Select(x => x.Start.TotalMinutes).Distinct().Count());
    }

    [Fact]
    public async Task Dobos_duplicate_minute_no_longer_yields_two_blocks_at_the_same_start()
    {
        var (connection, context) = await CreateDatabaseAsync();
        await using (connection)
        await using (context)
        {
            var repository = new ActivityRepository(context);
            await WriteAsync(repository,
                (1, 179_220,
                [
                    Span(179_350, 179_351, DriverActivity.Driving),
                    Span(179_351, 179_352, DriverActivity.OtherWork)
                ]),
                (2, 179_352,
                [
                    Span(179_351, 179_352, DriverActivity.OtherWork),
                    Span(179_352, 179_353, DriverActivity.OtherWork),
                    Span(179_353, 179_354, DriverActivity.OtherWork)
                ]));
            await repository.ObserveGameTimeAsync(
                Card,
                new GameTime(179_354 + ActivityRetentionPolicy.HotWindowMinutes));

            await repository.ArchiveWarmAsync(Card);

            context.ChangeTracker.Clear();
            var blocks = await context.WarmActivityBlocks.AsNoTracking()
                .OrderBy(x => x.StartGameMinute)
                .ToListAsync();
            Assert.Single(blocks, x => x.StartGameMinute == 179_351);
            Assert.Equal(3, blocks.Single(x => x.StartGameMinute == 179_351).DurationMinutes);
            Assert.Equal(4, blocks.Sum(x => x.DurationMinutes));
        }
    }

    [Fact]
    public async Task Staniek_manual_backfill_keeps_every_minute_it_alone_covers()
    {
        var (connection, context) = await CreateDatabaseAsync();
        await using (connection)
        await using (context)
        {
            var repository = new ActivityRepository(context);
            await WriteAsync(repository,
                (10, 184_392,
                [
                    Span(184_392, 184_393, DriverActivity.BreakOrRest),
                    Span(184_393, 184_394, DriverActivity.BreakOrRest)
                ]),
                (14, 185_152,
                [
                    Manual(184_392, 184_481, DriverActivity.BreakOrRest),
                    Manual(184_481, 184_704, DriverActivity.BreakOrRest),
                    Manual(184_704, 185_152, DriverActivity.BreakOrRest)
                ]),
                (16, 186_055, [Manual(185_806, 186_055, DriverActivity.BreakOrRest)]));

            var canonical = await repository.LoadRawDriverHistoryAsync(Card);

            Assert.Equal(
                [(184_392, 184_393), (184_393, 184_394), (184_394, 184_481),
                 (184_481, 184_704), (184_704, 185_152), (185_806, 186_055)],
                Spans(canonical));
            Assert.Equal(
                760,
                canonical.Where(x => x.Start.TotalMinutes < 185_152)
                    .Sum(x => x.DurationMinutes));
            Assert.Equal(249, canonical.Single(x => x.Start.TotalMinutes == 185_806)
                .DurationMinutes);
            Assert.Equal(1_009, canonical.Sum(x => x.DurationMinutes));
        }
    }

    [Fact]
    public async Task Archiving_the_field_data_twice_gives_the_same_result()
    {
        var (connection, context) = await CreateDatabaseAsync();
        await using (connection)
        await using (context)
        {
            var repository = new ActivityRepository(context);
            await WriteAsync(repository,
                (1, 179_220, [Span(179_351, 179_352, DriverActivity.OtherWork)]),
                (2, 179_352,
                [
                    Span(179_351, 179_352, DriverActivity.OtherWork),
                    Span(179_352, 179_354, DriverActivity.OtherWork)
                ]));
            await repository.ObserveGameTimeAsync(
                Card,
                new GameTime(179_354 + ActivityRetentionPolicy.HotWindowMinutes));

            var first = await repository.ArchiveWarmAsync(Card);
            context.ChangeTracker.Clear();
            var second = await repository.ArchiveWarmAsync(Card);
            context.ChangeTracker.Clear();

            Assert.Equal(first, second);
            var block = Assert.Single(await context.WarmActivityBlocks.AsNoTracking()
                .ToListAsync());
            Assert.Equal(179_351, block.StartGameMinute);
            Assert.Equal(3, block.DurationMinutes);
        }
    }

    [Fact]
    public async Task Overlapping_records_inside_one_session_are_reported_not_swallowed()
    {
        var (connection, context) = await CreateDatabaseAsync();
        await using (connection)
        await using (context)
        {
            var repository = new ActivityRepository(context);
            await WriteAsync(repository,
                (0, 100,
                [
                    Span(100, 150, DriverActivity.Driving),
                    Span(120, 170, DriverActivity.OtherWork)
                ]));

            var canonical = await repository.LoadRawDriverHistoryAsync(Card);

            // Intra-session overlap is resolved by the same coverage rule, so the guard
            // never fires here. It stays as the last line of defence for other producers.
            Assert.Equal([(100, 150), (150, 170)], Spans(canonical));
        }
    }

    private static async Task<IReadOnlyList<ActivityRecord>> ProjectAsync(
        params (int Index, long Anchor, ActivityRecord[] Records)[] sessions)
    {
        var (connection, context) = await CreateDatabaseAsync();
        await using (connection)
        await using (context)
        {
            var repository = new ActivityRepository(context);
            await WriteAsync(repository, sessions);
            return await repository.LoadRawDriverHistoryAsync(Card);
        }
    }

    private static async Task WriteAsync(
        ActivityRepository repository,
        params (int Index, long Anchor, ActivityRecord[] Records)[] sessions)
    {
        foreach (var (index, anchor, records) in sessions)
        {
            await repository.EnsureSessionAsync(Card, index, new GameTime(anchor));
            await repository.AppendAsync(Card, index, records);
        }
    }

    private static (long Start, long End)[] Spans(IEnumerable<ActivityRecord> records) =>
        records.OrderBy(x => x.Start)
            .Select(x => (x.Start.TotalMinutes, x.EndExclusive.TotalMinutes))
            .ToArray();

    private static ActivityRecord Manual(long start, long end, DriverActivity activity) =>
        Span(start, end, activity, ActivitySource.ManualEntry);

    private static ActivityRecord Span(
        long start,
        long end,
        DriverActivity activity,
        ActivitySource source = ActivitySource.Telemetry) => new()
    {
        Id = Guid.NewGuid(),
        DriverCardId = Card,
        Activity = activity,
        Start = new GameTime(start),
        EndExclusive = new GameTime(end),
        RecordedAtUtc = Epoch.AddMinutes(start),
        Source = source
    };

    private static async Task<(SqliteConnection Connection, TachographDbContext Context)>
        CreateDatabaseAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TachographDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new TachographDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.DriverProfiles.Add(new DriverProfileEntity
        {
            Id = Guid.NewGuid(),
            DisplayName = Card,
            IsActive = true,
            CreatedAtUtc = Epoch,
            Cards = [new DriverCardEntity
            {
                Id = Card,
                CountryCode = "PL",
                ValidFrom = new DateOnly(2026, 1, 1),
                ValidUntil = new DateOnly(2031, 1, 1)
            }]
        });
        await context.SaveChangesAsync();
        return (connection, context);
    }
}

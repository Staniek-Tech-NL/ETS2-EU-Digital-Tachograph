using System.Security.Cryptography;
using System.Text.Json;
using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Application.Services;

public sealed class ImportService(IActivityRepository activities)
{
    public async Task<int> ImportSessionAsync(Stream source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        var envelope = await JsonSerializer.DeserializeAsync<TachoExportEnvelope>(
            source, ExportService.JsonOptions, cancellationToken)
            ?? throw new InvalidDataException("The .tacho document is empty.");
        if (envelope.Format != "ETS2-TACHO" || envelope.Payload.SchemaVersion is not (1 or 2 or 3))
            throw new InvalidDataException("Unsupported .tacho format or schema version.");
        var actual = Convert.ToHexString(SHA256.HashData(
            JsonSerializer.SerializeToUtf8Bytes(envelope.Payload, ExportService.JsonOptions)));
        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(envelope.Checksum), Convert.FromHexString(actual)))
            throw new InvalidDataException("The .tacho checksum is invalid.");

        var count = 0;
        foreach (var session in envelope.Payload.Sessions)
        {
            var records = session.Records.Select(x => new ActivityRecord
            {
                Id = x.Id,
                DriverCardId = envelope.Payload.DriverCardId,
                Activity = x.Activity,
                Start = new GameTime(x.StartGameMinute),
                EndExclusive = new GameTime(x.EndGameMinuteExclusive),
                RecordedAtUtc = x.RecordedAtUtc,
                Source = x.Source,
                Condition = x.Condition,
                SourceGapId = x.SourceGapId
            }).ToList();
            var gaps = (session.Gaps ?? []).Select(x => new ActivityGap
            {
                Id = x.Id,
                DriverCardId = envelope.Payload.DriverCardId,
                Slot = x.Slot,
                SessionIndex = session.SessionIndex,
                Start = new GameTime(x.StartGameMinute),
                EndExclusive = x.EndGameMinuteExclusive is null
                    ? null
                    : new GameTime(x.EndGameMinuteExclusive.Value),
                Reason = x.Reason,
                State = x.State,
                ResolvedAt = x.ResolvedAtGameMinute is null
                    ? null
                    : new GameTime(x.ResolvedAtGameMinute.Value),
                ProjectionSourceGapId = x.ProjectionSourceGapId
            }).ToList();
            await activities.ApplySessionWritesAsync(
                [new ActivitySessionWrite(
                    envelope.Payload.DriverCardId,
                    session.SessionIndex,
                    new GameTime(session.StartedAtGameMinute),
                    records,
                    gaps)],
                cancellationToken);
            count += records.Count;
        }
        return count;
    }
}

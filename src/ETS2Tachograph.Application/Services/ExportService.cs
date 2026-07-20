using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Application.Persistence;

namespace ETS2Tachograph.Application.Services;

public sealed class ExportService(IActivityRepository activities)
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task ExportSessionAsync(
        string driverCardId,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(driverCardId);
        ArgumentNullException.ThrowIfNull(destination);
        var stored = await activities.LoadRawSessionsAsync(driverCardId, cancellationToken);
        var sessions = stored.Select(session => new ActivitySessionDto(
            session.SessionIndex,
            session.StartedAt.TotalMinutes,
            session.Records.Select(record => new ActivityRecordDto(
                record.Id, record.DriverCardId, record.Activity, record.Start.TotalMinutes,
                record.EndExclusive.TotalMinutes, record.RecordedAtUtc, record.Source, record.Condition,
                record.SourceGapId)).ToList(),
            (session.Gaps ?? []).Select(gap => new ActivityGapDto(
                gap.Id, gap.DriverCardId, gap.Slot, gap.SessionIndex,
                gap.Start.TotalMinutes, gap.EndExclusive?.TotalMinutes,
                gap.Reason, gap.State, gap.ResolvedAt?.TotalMinutes,
                gap.ProjectionSourceGapId)).ToList())).ToList();
        var payload = new TachoExportPayload(3, driverCardId, DateTimeOffset.UtcNow, sessions);
        var checksum = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions)));
        await JsonSerializer.SerializeAsync(
            destination,
            new TachoExportEnvelope("ETS2-TACHO", "SHA-256", checksum, payload),
            JsonOptions,
            cancellationToken);
    }
}

using ETS2Tachograph.Core.Enums;

namespace ETS2Tachograph.Application.Dtos;

public sealed record ActivityRecordDto(
    Guid Id,
    string DriverCardId,
    DriverActivity Activity,
    long StartGameMinute,
    long EndGameMinuteExclusive,
    DateTimeOffset RecordedAtUtc,
    ActivitySource Source,
    SpecialCondition Condition,
    Guid? SourceGapId = null);

public sealed record ActivitySessionDto(
    int SessionIndex,
    long StartedAtGameMinute,
    IReadOnlyList<ActivityRecordDto> Records,
    IReadOnlyList<ActivityGapDto>? Gaps = null);

public sealed record ActivityGapDto(
    Guid Id,
    string DriverCardId,
    int Slot,
    int SessionIndex,
    long StartGameMinute,
    long? EndGameMinuteExclusive,
    ActivityGapReason Reason,
    ActivityGapState State,
    long? ResolvedAtGameMinute,
    Guid? ProjectionSourceGapId = null);

public sealed record TachoExportPayload(
    int SchemaVersion,
    string DriverCardId,
    DateTimeOffset ExportedAtUtc,
    IReadOnlyList<ActivitySessionDto> Sessions);

public sealed record TachoExportEnvelope(
    string Format,
    string Algorithm,
    string Checksum,
    TachoExportPayload Payload);

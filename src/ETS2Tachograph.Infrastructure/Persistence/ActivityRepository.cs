using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Application.Persistence;
using ETS2Tachograph.Application.Services;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Infrastructure.Persistence;

public sealed partial class ActivityRepository :
    IActivityRepository,
    IActivityRetentionRepository,
    IManualEntryRepository
{
    private readonly TachographDbContext context;
    private readonly IActivityPersistenceDiagnostics? diagnostics;

    public ActivityRepository(
        TachographDbContext context,
        IActivityPersistenceDiagnostics? diagnostics = null)
    {
        this.context = context;
        this.diagnostics = diagnostics;
    }

    private static IReadOnlyList<StoredActivitySession> MapSessions(
        string driverCardId,
        IReadOnlyList<ActivitySessionEntity> sessions) => sessions.Select(session =>
            new StoredActivitySession(
                session.SessionIndex,
                new GameTime(session.StartedAtGameMinute),
                session.Records.OrderBy(x => x.StartGameMinute)
                    .Select(x => Map(x, driverCardId))
                    .ToList(),
                session.Gaps.OrderBy(x => x.StartGameMinute)
                    .Select(x => Map(x, session.SessionIndex))
                    .ToList())).ToList();

    private static ActivityRecord Map(ActivityRecordEntity x, string driverCardId) => new()
    {
        Id = x.Id,
        DriverCardId = driverCardId,
        Activity = x.Activity,
        Start = new GameTime(x.StartGameMinute),
        EndExclusive = new GameTime(x.EndGameMinuteExclusive),
        RecordedAtUtc = x.RecordedAtUtc,
        Source = x.Source,
        Condition = x.Condition,
        SourceGapId = x.SourceGapId
    };

    private static ActivityRecord MapWarm(WarmActivityBlockEntity x) => new()
    {
        Id = x.Id,
        DriverCardId = x.DriverCardId,
        Activity = x.Activity,
        Start = new GameTime(x.StartGameMinute),
        EndExclusive = new GameTime(x.EndGameMinuteExclusive),
        RecordedAtUtc = DateTimeOffset.UnixEpoch,
        Source = x.Source,
        Condition = x.Condition,
        SourceGapId = x.SourceGapId
    };

    private static ActivityGap Map(ActivityGapEntity x, int sessionIndex) => new()
    {
        Id = x.Id,
        DriverCardId = x.DriverCardId,
        Slot = x.Slot,
        SessionIndex = sessionIndex,
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
    };

    private sealed record MaterializedSession(
        int SessionIndex,
        GameTime StartedAt,
        IReadOnlyList<ActivityRecord> Records,
        IReadOnlyList<ActivityGap> Gaps);
}

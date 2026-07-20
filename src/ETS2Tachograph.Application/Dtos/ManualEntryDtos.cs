using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.RuleEngine;

namespace ETS2Tachograph.Application.Dtos;

public sealed record ManualEntrySegment(
    long FromGameMinute,
    long ToGameMinuteExclusive,
    DriverActivity Activity);

public enum ResolveGapStatus
{
    Resolved = 0,
    AlreadyResolved = 1
}

public sealed record ResolveGapResult(
    ResolveGapStatus Status,
    ActivityGap Gap,
    IReadOnlyList<ActivityRecord> Segments,
    RegulationEvaluation Evaluation);

public enum ManualEntryError
{
    GapNotFound = 0,
    GapNotCanonical = 1,
    ProjectedGapCannotBeResolved = 2,
    GapStillOpen = 3,
    InvalidActivity = 4,
    InvalidSegment = 5,
    IncompleteCoverage = 6,
    OutsideGap = 7,
    OverlappingSegments = 8,
    HistoryCollision = 9,
    ResolutionConflict = 10
}

public sealed class ManualEntryValidationException(
    ManualEntryError error,
    string message) : InvalidOperationException(message)
{
    public ManualEntryError Error { get; } = error;
}

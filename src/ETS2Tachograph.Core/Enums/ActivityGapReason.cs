namespace ETS2Tachograph.Core.Enums;

public enum ActivityGapReason
{
    ForwardTimeJump = 0,
    CardRemoved = 1,
    TelemetryUnavailable = 2
}

public enum ActivityGapState
{
    Unresolved = 0,
    Resolved = 1
}

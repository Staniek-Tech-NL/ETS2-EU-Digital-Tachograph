using ETS2Tachograph.Core.Enums;

namespace ETS2Tachograph.Core.Settings;

public sealed record TachographSettings
{
    public int WeekEpochOffsetDays { get; init; }
    public double DrivingSpeedThresholdKph { get; init; } = 5;
    public DriverActivity ActivityAfterStop { get; init; } = DriverActivity.OtherWork;
}

namespace ETS2Tachograph.Application.Dtos;

public sealed record SettingsDto(double DrivingSpeedThresholdKph, int WeekEpochOffsetDays);

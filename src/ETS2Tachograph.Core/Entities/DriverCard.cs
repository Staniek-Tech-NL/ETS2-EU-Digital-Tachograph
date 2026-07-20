namespace ETS2Tachograph.Core.Entities;

/// <summary>A deliberately small driver-card model mapped to an ETS2 profile.</summary>
public sealed record DriverCard
{
    public required string CardNumber { get; init; }
    public required string DriverName { get; init; }
    public DateOnly ExpiryDate { get; init; }
    public string IssuingCountry { get; init; } = "PL";
}

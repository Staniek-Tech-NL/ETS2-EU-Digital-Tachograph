namespace ETS2Tachograph.Application.Dtos;

public sealed record DriverCardDto(
    string CardNumber,
    string IssuingCountry,
    DateOnly ValidFrom,
    DateOnly ExpiryDate);

public sealed record DriverProfileDto(
    Guid Id,
    string DisplayName,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<DriverCardDto> Cards);

public sealed record CreateDriverProfileDto(
    string DisplayName,
    DriverCardDto Card);

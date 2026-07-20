using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Application.Persistence;

namespace ETS2Tachograph.Application.Services;

public sealed class DriverService(IDriverRepository drivers)
{
    public Task<IReadOnlyList<DriverProfileDto>> GetProfilesAsync(
        CancellationToken cancellationToken = default) => drivers.GetAllAsync(cancellationToken);

    public Task<DriverProfileDto?> GetActiveProfileAsync(
        CancellationToken cancellationToken = default) => drivers.GetActiveAsync(cancellationToken);

    public Task<DriverProfileDto> CreateProfileAsync(
        CreateDriverProfileDto profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (string.IsNullOrWhiteSpace(profile.DisplayName))
            throw new ArgumentException("Driver display name is required.", nameof(profile));
        if (string.IsNullOrWhiteSpace(profile.Card.CardNumber))
            throw new ArgumentException("Driver card number is required.", nameof(profile));
        if (profile.Card.ExpiryDate < profile.Card.ValidFrom)
            throw new ArgumentException("Driver card expiry must not precede its validity date.", nameof(profile));
        return drivers.CreateAsync(profile, cancellationToken);
    }

    public Task SetActiveProfileAsync(Guid profileId, CancellationToken cancellationToken = default) =>
        drivers.SetActiveAsync(profileId, cancellationToken);
}

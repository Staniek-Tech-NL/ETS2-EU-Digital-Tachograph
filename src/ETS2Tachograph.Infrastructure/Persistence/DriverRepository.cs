using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ETS2Tachograph.Infrastructure.Persistence;

public sealed class DriverRepository(TachographDbContext context) : IDriverRepository
{
    public async Task<IReadOnlyList<DriverProfileDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        (await context.DriverProfiles.AsNoTracking().Include(x => x.Cards)
            .OrderBy(x => x.DisplayName).ToListAsync(cancellationToken)).Select(Map).ToList();

    public async Task<DriverProfileDto?> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var profile = await context.DriverProfiles.AsNoTracking().Include(x => x.Cards)
            .SingleOrDefaultAsync(x => x.IsActive, cancellationToken);
        return profile is null ? null : Map(profile);
    }

    public async Task<DriverProfileDto> CreateAsync(
        CreateDriverProfileDto profile, CancellationToken cancellationToken = default)
    {
        var entity = new DriverProfileEntity
        {
            Id = Guid.NewGuid(),
            DisplayName = profile.DisplayName.Trim(),
            IsActive = false,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Cards = [new DriverCardEntity
            {
                Id = profile.Card.CardNumber, CountryCode = profile.Card.IssuingCountry,
                ValidFrom = profile.Card.ValidFrom, ValidUntil = profile.Card.ExpiryDate
            }]
        };
        context.DriverProfiles.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task SetActiveAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        var profiles = await context.DriverProfiles.ToListAsync(cancellationToken);
        if (!profiles.Any(x => x.Id == profileId)) throw new KeyNotFoundException("Driver profile not found.");
        foreach (var profile in profiles) profile.IsActive = profile.Id == profileId;
        await context.SaveChangesAsync(cancellationToken);
    }

    private static DriverProfileDto Map(DriverProfileEntity x) => new(
        x.Id, x.DisplayName, x.IsActive, x.CreatedAtUtc,
        x.Cards.Select(card => new DriverCardDto(
            card.Id, card.CountryCode, card.ValidFrom, card.ValidUntil)).ToList());
}

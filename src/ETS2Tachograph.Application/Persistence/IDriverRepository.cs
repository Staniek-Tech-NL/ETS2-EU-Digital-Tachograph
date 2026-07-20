using ETS2Tachograph.Application.Dtos;

namespace ETS2Tachograph.Application.Persistence;

public interface IDriverRepository
{
    Task<IReadOnlyList<DriverProfileDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DriverProfileDto?> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<DriverProfileDto> CreateAsync(CreateDriverProfileDto profile, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid profileId, CancellationToken cancellationToken = default);
}

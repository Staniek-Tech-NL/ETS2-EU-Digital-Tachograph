using ETS2Tachograph.Application.Dtos;

namespace ETS2Tachograph.Application.Persistence;

public interface ISettingsRepository
{
    Task<SettingsDto> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(SettingsDto settings, CancellationToken cancellationToken = default);
}

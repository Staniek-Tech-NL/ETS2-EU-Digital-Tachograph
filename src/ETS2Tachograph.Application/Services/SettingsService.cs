using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Application.Persistence;

namespace ETS2Tachograph.Application.Services;

public sealed class SettingsService(ISettingsRepository repository)
{
    public Task<SettingsDto> LoadAsync(CancellationToken cancellationToken = default) =>
        repository.LoadAsync(cancellationToken);

    public Task SaveAsync(SettingsDto settings, CancellationToken cancellationToken = default)
    {
        if (settings.DrivingSpeedThresholdKph is < 0 or > 20)
            throw new ArgumentOutOfRangeException(nameof(settings), "Driving threshold must be between 0 and 20 km/h.");
        if (settings.WeekEpochOffsetDays is < -6 or > 6)
            throw new ArgumentOutOfRangeException(nameof(settings), "Week offset must be between -6 and 6 days.");
        return repository.SaveAsync(settings, cancellationToken);
    }
}

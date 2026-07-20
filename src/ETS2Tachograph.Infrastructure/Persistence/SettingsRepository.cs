using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ETS2Tachograph.Infrastructure.Persistence;

public sealed class SettingsRepository(TachographDbContext context) : ISettingsRepository
{
    public async Task<SettingsDto> LoadAsync(CancellationToken cancellationToken = default)
    {
        var row = await context.TachographSettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == 1, cancellationToken);
        return row is null ? new SettingsDto(1, 0) : new SettingsDto(row.DrivingSpeedThresholdKph, row.WeekEpochOffsetDays);
    }

    public async Task SaveAsync(SettingsDto settings, CancellationToken cancellationToken = default)
    {
        var row = await context.TachographSettings.SingleOrDefaultAsync(x => x.Id == 1, cancellationToken);
        if (row is null)
        {
            row = new TachographSettingsEntity { Id = 1 };
            context.TachographSettings.Add(row);
        }
        row.DrivingSpeedThresholdKph = settings.DrivingSpeedThresholdKph;
        row.WeekEpochOffsetDays = settings.WeekEpochOffsetDays;
        await context.SaveChangesAsync(cancellationToken);
    }
}

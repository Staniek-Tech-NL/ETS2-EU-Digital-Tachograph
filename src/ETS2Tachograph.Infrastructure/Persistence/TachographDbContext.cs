using Microsoft.EntityFrameworkCore;

namespace ETS2Tachograph.Infrastructure.Persistence;

public sealed class TachographDbContext(DbContextOptions<TachographDbContext> options)
    : DbContext(options)
{
    public DbSet<DriverProfileEntity> DriverProfiles => Set<DriverProfileEntity>();
    public DbSet<DriverCardEntity> DriverCards => Set<DriverCardEntity>();
    public DbSet<ActivitySessionEntity> ActivitySessions => Set<ActivitySessionEntity>();
    public DbSet<ActivityRecordEntity> ActivityRecords => Set<ActivityRecordEntity>();
    public DbSet<ActivityGapEntity> ActivityGaps => Set<ActivityGapEntity>();
    public DbSet<WarmActivityBlockEntity> WarmActivityBlocks => Set<WarmActivityBlockEntity>();
    public DbSet<ActivityRetentionStateEntity> ActivityRetentionStates => Set<ActivityRetentionStateEntity>();
    public DbSet<RegulationSnapshotEntity> RegulationSnapshots => Set<RegulationSnapshotEntity>();
    public DbSet<FerryRestRecordEntity> FerryRestRecords => Set<FerryRestRecordEntity>();
    public DbSet<TachographSettingsEntity> TachographSettings => Set<TachographSettingsEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DriverProfileEntity>(entity =>
        {
            entity.ToTable("DriverProfiles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
            entity.HasIndex(x => x.IsActive);
        });
        modelBuilder.Entity<DriverCardEntity>(entity =>
        {
            entity.ToTable("DriverCards");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.CountryCode).HasMaxLength(3);
            entity.HasOne(x => x.DriverProfile).WithMany(x => x.Cards)
                .HasForeignKey(x => x.DriverProfileId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ActivitySessionEntity>(entity =>
        {
            entity.ToTable("ActivitySessions");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.DriverCardId, x.SessionIndex }).IsUnique();
            entity.HasOne(x => x.DriverCard).WithMany(x => x.Sessions)
                .HasForeignKey(x => x.DriverCardId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ActivityRecordEntity>(entity =>
        {
            entity.ToTable("ActivityRecords");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ActivitySessionId, x.StartGameMinute }).IsUnique();
            entity.HasOne(x => x.ActivitySession).WithMany(x => x.Records)
                .HasForeignKey(x => x.ActivitySessionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.SourceGapId, x.StartGameMinute }).IsUnique();
            entity.HasOne(x => x.SourceGap).WithMany(x => x.ManualEntryRecords)
                .HasForeignKey(x => x.SourceGapId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ActivityGapEntity>(entity =>
        {
            entity.ToTable("ActivityGaps");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DriverCardId).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => new { x.ActivitySessionId, x.StartGameMinute }).IsUnique();
            entity.HasIndex(x => new { x.DriverCardId, x.StartGameMinute });
            entity.HasIndex(x => x.ProjectionSourceGapId);
            entity.HasOne(x => x.ActivitySession).WithMany(x => x.Gaps)
                .HasForeignKey(x => x.ActivitySessionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ProjectionSourceGap).WithMany()
                .HasForeignKey(x => x.ProjectionSourceGapId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<WarmActivityBlockEntity>(entity =>
        {
            entity.ToTable("WarmActivityBlocks");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.DriverCardId, x.StartGameMinute }).IsUnique();
            entity.HasIndex(x => x.SourceGapId);
            entity.HasOne(x => x.DriverCard).WithMany()
                .HasForeignKey(x => x.DriverCardId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ActivityRetentionStateEntity>(entity =>
        {
            entity.ToTable("ActivityRetentionStates");
            entity.HasKey(x => x.DriverCardId);
            entity.HasOne(x => x.DriverCard).WithMany()
                .HasForeignKey(x => x.DriverCardId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<RegulationSnapshotEntity>(entity =>
        {
            entity.ToTable("RegulationSnapshots");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.DriverCardId, x.GameMinute });
        });
        modelBuilder.Entity<FerryRestRecordEntity>(entity =>
        {
            entity.ToTable("FerryRestRecords");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.DriverCardId, x.StartGameMinute });
        });
        modelBuilder.Entity<TachographSettingsEntity>(entity =>
        {
            entity.ToTable("TachographSettings");
            entity.HasKey(x => x.Id);
        });
    }
}

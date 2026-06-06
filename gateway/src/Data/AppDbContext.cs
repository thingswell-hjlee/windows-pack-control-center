using ControlCenter.Gateway.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlCenter.Gateway.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Camera> Cameras => Set<Camera>();
    public DbSet<NormalizedEvent> Events => Set<NormalizedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.DeviceId);
            entity.HasIndex(e => e.Status);
            entity.HasMany(e => e.Cameras)
                  .WithOne(c => c.Device)
                  .HasForeignKey(c => c.DeviceId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Camera>(entity =>
        {
            entity.HasKey(e => e.CameraId);
            entity.HasIndex(e => e.DeviceId);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<NormalizedEvent>(entity =>
        {
            entity.HasKey(e => e.EventId);
            entity.HasIndex(e => e.DeviceId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.Severity);
            entity.HasIndex(e => e.AckStatus);
        });
    }
}

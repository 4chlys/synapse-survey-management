using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Platform.Entities;

namespace SynapseEnqueteManagement.UI.Admin.Data;

public class AdminDbContext(DbContextOptions<AdminDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<ProjectPlatform> ProjectPlatforms { get; set; }
    public DbSet<UserWidgetPreference> UserWidgetPreferences { get; set; }
    public DbSet<PlatformSettings> PlatformSettings { get; set; }
    public DbSet<SystemAiSettings> SystemAiSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ProjectPlatforms table is owned by SynapseEnqueteContext.
        // AdminDbContext can read/write it but must not create or modify it in migrations.
        builder.Entity<ProjectPlatform>(e =>
        {
            e.ToTable("ProjectPlatforms", t => t.ExcludeFromMigrations());
        });

        // ApplicationUser → ProjectPlatform FK (no navigation on ProjectPlatform side)
        builder.Entity<ApplicationUser>(e =>
        {
            e.HasOne(u => u.ProjectPlatform)
                .WithMany()
                .HasForeignKey(u => u.ProjectPlatformId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<UserWidgetPreference>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => new { p.UserId, p.ProjectId, p.Position }).IsUnique();
            e.Property(p => p.UserId).IsRequired().HasMaxLength(450);
            e.Property(p => p.WidgetType).IsRequired().HasMaxLength(50);
        });

        builder.Entity<PlatformSettings>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.ProjectPlatformId).IsUnique();
        });

        builder.Entity<SystemAiSettings>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.AllowedModelIds).IsRequired();
        });
    }
}

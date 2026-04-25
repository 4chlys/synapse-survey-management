// UI-Admin/Data/AdminDbContext.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Platform.Entities;

namespace SynapseEnqueteManagement.UI.Admin.Data;

public class AdminDbContext(DbContextOptions<AdminDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<ProjectPlatform> ProjectPlatforms { get; set; }

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
    }
}

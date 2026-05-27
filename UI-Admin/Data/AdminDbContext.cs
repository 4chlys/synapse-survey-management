using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SynapseEnqueteManagement.UI.Admin.Data;

public class AdminDbContext(DbContextOptions<AdminDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<UserWidgetPreference> UserWidgetPreferences { get; set; }
    public DbSet<PlatformSettings> PlatformSettings { get; set; }
    public DbSet<SystemAiSettings> SystemAiSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ApplicationUser.ProjectPlatformId is a plain scalar FK to the API-owned
        // ProjectPlatforms table. AdminDbContext does NOT map ProjectPlatforms — it is
        // owned by SynapseEnqueteContext (API side). The FK column is declared here by
        // name so EF knows the column exists; no typed relationship to ProjectPlatform
        // is configured because that would require referencing the Domain entity, which
        // UI-Admin must not do. Referential integrity is enforced by application logic
        // (platform-delete endpoints, if added, must null out admin users' FK first).
        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.ProjectPlatformId).HasColumnName("ProjectPlatformId");
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

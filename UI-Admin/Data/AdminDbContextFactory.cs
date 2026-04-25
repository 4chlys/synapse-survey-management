// UI-Admin/Data/AdminDbContextFactory.cs
// Used only at design time (dotnet ef migrations) — not loaded at runtime.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SynapseEnqueteManagement.UI.Admin.Data;

public class AdminDbContextFactory : IDesignTimeDbContextFactory<AdminDbContext>
{
    public AdminDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AdminDbContext>();

        // Design-time connection string — only used by EF tooling, never at runtime.
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=SynapseAdmin_Design;Username=postgres;Password=REDACTED");

        return new AdminDbContext(optionsBuilder.Options);
    }
}

using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SynapseEnqueteManagement.UI.Admin.Data;

namespace SynapseEnqueteManagement.UI.Admin.Tests.Integration;

// Reusable WebApplicationFactory base for UI-Admin integration tests.
// Swaps AdminDbContext for an open SQLite in-memory connection (singleton so EF won't
// close it between requests). Future tests that need an authenticated client or
// CSRF token plumbing should extend this — pattern adapted from the course's
// ExtendedWebApplicationFactory<TProgram>.
public class ExtendedWebApplicationFactory<TProgram>
    : WebApplicationFactory<TProgram> where TProgram : class
{
    public SqliteConnection SqliteInMemoryConnection { get; private set; }

    public ExtendedWebApplicationFactory()
    {
        // Set BEFORE Program.cs top-level statements read configuration.
        // ConfigureAppConfiguration only fires at builder.Build(), which is too late:
        // UI-Admin/Program.cs throws on a missing ApiKey at the top of the file.
        Environment.SetEnvironmentVariable("ApiKey", "test-api-key");
        Environment.SetEnvironmentVariable("ApiBaseUrl", "http://localhost");
        Environment.SetEnvironmentVariable("SystemAdmin__Email", "admin@test.local");
        Environment.SetEnvironmentVariable("SystemAdmin__Password", "Test1234!");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");

        builder.ConfigureServices(services =>
        {
            var dbContextOptions = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AdminDbContext>));
            if (dbContextOptions is not null) services.Remove(dbContextOptions);

            var dbConnection = services.SingleOrDefault(d => d.ServiceType == typeof(DbConnection));
            if (dbConnection is not null) services.Remove(dbConnection);

            services.AddSingleton<DbConnection>(_ =>
            {
                SqliteInMemoryConnection = new SqliteConnection("DataSource=:memory:");
                SqliteInMemoryConnection.Open();
                return SqliteInMemoryConnection;
            });

            services.AddDbContext<AdminDbContext>((sp, opt) =>
                opt.UseSqlite(sp.GetRequiredService<DbConnection>()));
        });
    }
}

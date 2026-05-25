using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SynapseEnqueteManagement.UI.User.Tests.Integration;

// Reusable WebApplicationFactory for UI-User integration tests. The minimum
// env vars must be set BEFORE Program.cs top-level statements read configuration
// — UI-User/Program.cs throws on a missing ApiKey at file scope, which runs before
// ConfigureAppConfiguration would fire.
public class ExtendedWebApplicationFactory<TProgram>
    : WebApplicationFactory<TProgram> where TProgram : class
{
    public ExtendedWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("ApiKey", "test-api-key");
        Environment.SetEnvironmentVariable("ApiBaseUrl", "http://localhost");
        // Override the Production AllowedHosts whitelist so the TestServer's
        // 'localhost' Host header survives HostFilteringMiddleware.
        Environment.SetEnvironmentVariable("AllowedHosts", "*");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
    }
}

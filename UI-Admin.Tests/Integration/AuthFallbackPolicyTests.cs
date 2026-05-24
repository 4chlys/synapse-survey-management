using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SynapseEnqueteManagement.UI.Admin.Tests.Integration;

// Verifies the AddAuthorization FallbackPolicy in UI-Admin/Program.cs:
// any controller without [AllowAnonymous] / [Authorize] requires authentication,
// while the opted-out endpoints remain reachable to anonymous callers.
public class AuthFallbackPolicyTests : IClassFixture<ExtendedWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthFallbackPolicyTests(ExtendedWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task UnattributedController_AnonymousRequest_RedirectsToLogin()
    {
        var response = await _client.GetAsync("/Category/Index?projectId=1");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/Auth/Login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task LoginPage_IsReachableAnonymously()
    {
        var response = await _client.GetAsync("/Auth/Login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PrivacyPage_IsReachableAnonymously()
    {
        var response = await _client.GetAsync("/Privacy");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthEndpoint_IsReachableAnonymously()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

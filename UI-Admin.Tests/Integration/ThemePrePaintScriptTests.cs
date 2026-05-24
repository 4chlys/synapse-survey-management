using Microsoft.AspNetCore.Mvc.Testing;

namespace SynapseEnqueteManagement.UI.Admin.Tests.Integration;

// Verifies the pre-paint inline theme script in _Layout.cshtml and _LoginLayout.cshtml.
// When the visitor has no saved 'admin-theme' in localStorage the script must fall
// through to matchMedia('prefers-color-scheme: dark') so first-time OS-dark visitors
// land on dark immediately instead of flashing light before the JS module catches up.
//
// Regression guard for the FOUC fix: the previous inline script only honoured
// localStorage and silently skipped the OS-preference path. Both layouts are
// covered because the auth flow runs under _LoginLayout while the rest of the
// admin uses _Layout — a regression in either is a visible flash.
public class ThemePrePaintScriptTests : IClassFixture<ExtendedWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ThemePrePaintScriptTests(ExtendedWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    // /Privacy is reachable anonymously and uses _Layout (via _ViewStart).
    [InlineData("/Privacy")]
    // /Auth/Login is reachable anonymously and uses _LoginLayout (set in the view).
    [InlineData("/Auth/Login")]
    public async Task InlineScript_FallsThroughToMatchMediaWhenNoLocalStorage(string path)
    {
        var response = await _client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("localStorage.getItem('admin-theme')", html);
        Assert.Contains("matchMedia('(prefers-color-scheme: dark)')", html);
        Assert.Contains("synapse-dark", html);
        Assert.Contains("synapse-light", html);
    }
}

using Microsoft.AspNetCore.Mvc.Testing;

namespace SynapseEnqueteManagement.UI.Admin.Tests.Integration;

// Verifies the cookie-driven pre-paint theme rendering in _Layout.cshtml and
// _LoginLayout.cshtml. The 'no inline JS' rule forbids the old inline pre-paint
// script, so FOUC is now prevented in two ways:
//   1. If the 'admin-theme' cookie is present, the server renders data-theme=...
//      on <html> before first paint.
//   2. If no cookie is present, the server leaves data-theme OFF so daisyUI's
//      `:root:not([data-theme])` rule (with prefersdark: true on synapse-dark)
//      catches OS-dark visitors via pure CSS.
//
// Both layouts are covered because the auth flow runs under _LoginLayout while
// the rest of the admin uses _Layout — a regression in either is a visible flash.
public class ThemePrePaintScriptTests : IClassFixture<ExtendedWebApplicationFactory<Program>>
{
    private readonly ExtendedWebApplicationFactory<Program> _factory;

    public ThemePrePaintScriptTests(ExtendedWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    // /Privacy is reachable anonymously and uses _Layout (via _ViewStart).
    [InlineData("/Privacy")]
    // /Auth/Login is reachable anonymously and uses _LoginLayout (set in the view).
    [InlineData("/Auth/Login")]
    public async Task NoCookie_OmitsDataThemeSoDaisyUiPrefersDarkApplies(string path)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        // First-time visitor: no data-theme attribute on <html>, so daisyUI's
        // prefers-color-scheme CSS rule decides. The themes themselves must
        // still be registered (the stylesheet/bundle is wired up downstream).
        Assert.DoesNotContain("<html lang=\"nl\" data-theme=", html);
    }

    [Theory]
    [InlineData("/Privacy", "synapse-dark")]
    [InlineData("/Privacy", "synapse-light")]
    [InlineData("/Auth/Login", "synapse-dark")]
    [InlineData("/Auth/Login", "synapse-light")]
    public async Task Cookie_RendersDataThemeOnHtmlBeforeFirstPaint(string path, string theme)
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("Cookie", $"admin-theme={theme}");

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains($"data-theme=\"{theme}\"", html);
    }

    [Theory]
    [InlineData("/Privacy")]
    [InlineData("/Auth/Login")]
    public async Task InvalidCookieValue_IsIgnoredAndAttributeOmitted(string path)
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("Cookie", "admin-theme=not-a-real-theme");

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("data-theme=\"not-a-real-theme\"", html);
        Assert.DoesNotContain("<html lang=\"nl\" data-theme=", html);
    }
}

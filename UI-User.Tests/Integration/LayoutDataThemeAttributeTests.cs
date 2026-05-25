using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SynapseEnqueteManagement.UI.User.Tests.Integration;

// Verifies <html data-theme="..."> rendering in _Layout.cshtml:
// - first visit (no cookie) must omit the attribute entirely so daisyUI's
//   :root:not([data-theme]) selector — which carries the
//   @media (prefers-color-scheme: dark) block — can fire and OS preference wins.
// - a cookie holding a recognised value must echo that value on <html>.
// - an unrecognised cookie value must be ignored.
//
// Regression guard for the bug where data-theme="@theme" rendered
// data-theme="" for null and silently broke OS-dark first-paint detection.
public class LayoutDataThemeAttributeTests : IClassFixture<ExtendedWebApplicationFactory<Program>>
{
    private readonly ExtendedWebApplicationFactory<Program> _factory;

    public LayoutDataThemeAttributeTests(ExtendedWebApplicationFactory<Program> factory)
        => _factory = factory;

    [Fact]
    public async Task FirstVisit_NoCookie_OmitsDataThemeOnHtmlTag()
    {
        var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/");
        var openTag = OpeningHtmlTag(html);

        Assert.DoesNotContain("data-theme", openTag);
        Assert.DoesNotContain("data-theme=\"\"", html);
    }

    [Theory]
    [InlineData("synapse-light")]
    [InlineData("synapse-dark")]
    public async Task CookieWithValidTheme_RendersDataThemeOnHtmlTag(string theme)
    {
        var html = await GetWithCookie($"synapse-theme={theme}");
        var openTag = OpeningHtmlTag(html);

        Assert.Contains($"data-theme=\"{theme}\"", openTag);
    }

    [Fact]
    public async Task CookieWithUnknownTheme_OmitsDataThemeOnHtmlTag()
    {
        var html = await GetWithCookie("synapse-theme=hacker-theme");
        var openTag = OpeningHtmlTag(html);

        Assert.DoesNotContain("data-theme", openTag);
    }

    private async Task<string> GetWithCookie(string cookie)
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Cookie", cookie);
        var response = await client.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }

    // The body contains a separate data-project-theme attribute, so scoping the
    // assertion to the opening <html> tag avoids false positives.
    private static string OpeningHtmlTag(string html)
    {
        var match = Regex.Match(html, @"<html\b[^>]*>");
        Assert.True(match.Success, "Response did not contain an opening <html> tag.");
        return match.Value;
    }
}

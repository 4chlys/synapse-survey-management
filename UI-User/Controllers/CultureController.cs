using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SynapseEnqueteManagement.UI.User.Services.Project;

namespace SynapseEnqueteManagement.UI.User.Controllers;

public class CultureController(
    IOptions<RequestLocalizationOptions> localizationOptions,
    ParticipantService participantService,
    ILogger<CultureController> logger) : Controller
{
    private const string CookiePrefix = "ParticipantToken_";

    [HttpPost]
    public async Task<IActionResult> SetCulture(string culture, string returnUrl)
    {
        var supported = localizationOptions.Value.SupportedUICultures
            .Select(c => c.Name);

        if (!supported.Contains(culture, StringComparer.OrdinalIgnoreCase))
            return BadRequest();

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps
            });

        foreach (var cookieKey in Request.Cookies.Keys)
        {
            if (!cookieKey.StartsWith(CookiePrefix, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!int.TryParse(cookieKey[CookiePrefix.Length..], out var projectId))
                continue;

            try
            {
                await participantService.SetLanguageAsync(projectId, culture);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to persist language '{Culture}' for project {ProjectId}.", culture, projectId);
            }
        }

        return LocalRedirect(returnUrl ?? "/");
    }
}

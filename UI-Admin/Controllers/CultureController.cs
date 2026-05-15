using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace SynapseEnqueteManagement.UI.Admin.Controllers;

public class CultureController(IOptions<RequestLocalizationOptions> localizationOptions) : Controller
{
    [HttpPost]
    public IActionResult SetCulture(string culture, string returnUrl)
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

        return LocalRedirect(returnUrl ?? "/");
    }
}

using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace SynapseEnqueteManagement.UI.User.Controllers;

public class CultureController : Controller
{
    // TODO (step 7): load supported cultures dynamically from ILanguageManager instead of hardcoding.
    private static readonly HashSet<string> _supported =
        new(StringComparer.OrdinalIgnoreCase) { "nl", "en" };

    [HttpPost]
    public IActionResult SetCulture(string culture, string returnUrl)
    {
        if (!_supported.Contains(culture))
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

using Microsoft.AspNetCore.Mvc;

namespace SynapseEnqueteManagement.UI.User.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();

    public IActionResult ShortFormNotFound()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("~/Views/LandingPage/NotFound.cshtml");
    }

    public IActionResult ShortFormInactive()
        => View("~/Views/LandingPage/Inactive.cshtml");
}

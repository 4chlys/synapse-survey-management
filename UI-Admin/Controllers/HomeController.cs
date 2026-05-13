using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SynapseEnqueteManagement.UI.Admin.Controllers;

[Authorize]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.IsInRole("SystemAdmin"))
            return RedirectToAction("SystemDashboard", "Platform");
        return RedirectToAction("Index", "Project");
    }
}
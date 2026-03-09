using Microsoft.AspNetCore.Mvc;

namespace SynapseEnqueteManagement.UI.User.Controllers;

public class IdeationController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}

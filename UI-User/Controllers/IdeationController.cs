using Microsoft.AspNetCore.Mvc;

namespace SynapseEnqueteManagement.UI.User.Controllers;

public class IdeationController : Controller
{
    public IActionResult Index(int projectId, int id)
    {
        ViewData["ProjectId"] = projectId;
        ViewData["CategoryId"] = id;
        return View();
    }
}

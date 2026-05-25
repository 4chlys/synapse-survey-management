using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SynapseEnqueteManagement.UI.Admin.Controllers;

[AllowAnonymous]
public class PrivacyController : Controller
{
    public IActionResult Index() => View();
}

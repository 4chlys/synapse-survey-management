using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.Admin.Data;
using SynapseEnqueteManagement.UI.Admin.Models.Platform;
using SynapseEnqueteManagement.UI.Admin.Services.Platform;

namespace SynapseEnqueteManagement.UI.Admin.Controllers;

[Authorize(Roles = "ProjectPlatformAdmin")]
public class AdminController(
    PlatformAdminService platformService,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToAction("Login", "Auth");

        if (user.ProjectPlatformId is null)
            return Forbid();

        var admins = await platformService.GetAdminsForPlatformAsync(user.ProjectPlatformId.Value);
        var platform = await platformService.GetPlatformByIdAsync(user.ProjectPlatformId.Value);

        return View(new PlatformDetailViewModel
        {
            ProjectPlatformId = platform.ProjectPlatformId,
            Name = platform.Name,
            LogoUrl = platform.LogoUrl,
            WebsiteUrl = platform.WebsiteUrl,
            Admins = admins.Select(a => new AdminSummaryViewModel
            {
                UserName = a.UserName,
                Email = a.Email
            }).ToList()
        });
    }

    public async Task<IActionResult> Add()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToAction("Login", "Auth");

        if (user.ProjectPlatformId is null)
            return Forbid();

        var platform = await platformService.GetPlatformByIdAsync(user.ProjectPlatformId.Value);

        return View(new AddAdminViewModel
        {
            ProjectPlatformId = user.ProjectPlatformId.Value,
            PlatformName = platform.Name
        });
    }

    [HttpPost]
    public async Task<IActionResult> Add(AddAdminViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToAction("Login", "Auth");

        var platformId = user.ProjectPlatformId!.Value;

        var result = await platformService.AddAdminAsync(platformId, model.Email, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }
}

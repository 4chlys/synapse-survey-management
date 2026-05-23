using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.Admin.Data;
using SynapseEnqueteManagement.UI.Admin.Models.Admin;
using SynapseEnqueteManagement.UI.Admin.Models.Platform;
using SynapseEnqueteManagement.UI.Admin.Services.Platform;

namespace SynapseEnqueteManagement.UI.Admin.Controllers;

[Authorize(Roles = "SystemAdmin,ProjectPlatformAdmin")]
public class AdminController(
    PlatformAdminService platformService,
    UserManager<ApplicationUser> userManager,
    AdminInviteService adminInviteService) : Controller
{
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> Index(int? platformId = null)
    {
        var platforms = await platformService.GetAllPlatformsAsync();
        var allAdmins = await platformService.GetAllAdminsAsync();

        var filtered = platformId.HasValue
            ? allAdmins.Where(a => a.ProjectPlatformId == platformId).ToList()
            : allAdmins;

        ViewData["ActiveSection"] = "Admins";

        return View(new AdminListViewModel(
            filtered.Select(a => new AdminWithPlatformViewModel(
                a.Id,
                a.UserName ?? a.Email ?? string.Empty,
                a.Email ?? string.Empty,
                a.ProjectPlatformId ?? 0,
                a.ProjectPlatform?.Name ?? "—"
            )).ToList(),
            platforms.Select(p => new PlatformViewModel
            {
                ProjectPlatformId = p.ProjectPlatformId,
                Name = p.Name
            }).ToList(),
            platformId
        ));
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
    public async Task<IActionResult> Add(AddAdminViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToAction("Login", "Auth");

        var platformId = user.ProjectPlatformId!.Value;

        var platform = await platformService.GetPlatformByIdAsync(platformId);

        var result = await adminInviteService.InviteAdminAsync(platformId, platform.Name, model.Email,
            token => Url.Action("ResetPassword", "Auth", new { email = model.Email, token }, Request.Scheme), ct);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        return RedirectToAction(nameof(Add));
    }
}
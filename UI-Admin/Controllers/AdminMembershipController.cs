using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using SynapseEnqueteManagement.UI.Admin.Data;
using SynapseEnqueteManagement.UI.Admin.Models.Platform;
using SynapseEnqueteManagement.UI.Admin.Services.Platform;

namespace SynapseEnqueteManagement.UI.Admin.Controllers;

// Manages the admin-membership of a single platform: list, invite, remove.
// Hosted in a dedicated controller so PPAs can be whitelisted only for these
// three actions without loosening any role attribute on PlatformController.
[Authorize(Roles = "SystemAdmin,ProjectPlatformAdmin")]
public class AdminMembershipController(
    PlatformAdminService platformService,
    UserManager<ApplicationUser> userManager,
    AdminInviteService adminInviteService,
    IStringLocalizer<SharedResource> localizer) : Controller
{
    public async Task<IActionResult> Index(int? id)
    {
        var scopedId = await ResolveScopedPlatformIdAsync(id);
        if (scopedId is null) return Forbid();

        var platform = await platformService.GetPlatformByIdAsync(scopedId.Value);
        if (platform is null) return NotFound();

        ViewData["PlatformId"] = scopedId.Value;
        ViewData["PlatformName"] = platform.Name;
        ViewData["ActiveSection"] = "Admins";

        return View(await BuildViewModel(scopedId.Value, platform.Name));
    }

    [HttpPost]
    public async Task<IActionResult> AddAdmin(int? id, AddAdminViewModel model, CancellationToken ct)
    {
        var scopedId = await ResolveScopedPlatformIdAsync(id);
        if (scopedId is null) return Forbid();

        var platform = await platformService.GetPlatformByIdAsync(scopedId.Value);
        if (platform is null) return NotFound();

        ViewData["PlatformId"] = scopedId.Value;
        ViewData["PlatformName"] = platform.Name;
        ViewData["ActiveSection"] = "Admins";

        if (!ModelState.IsValid)
            return View("Index", await BuildViewModel(scopedId.Value, platform.Name));

        string inviteLink = null;
        var result = await adminInviteService.InviteAdminAsync(scopedId.Value, platform.Name, model.Email,
            token =>
            {
                inviteLink = Url.Action("ResetPassword", "Auth", new { email = model.Email, token }, Request.Scheme);
                return inviteLink;
            }, model.SendEmail, ct);

        if (!result.Identity.Succeeded)
        {
            foreach (var error in result.Identity.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View("Index", await BuildViewModel(scopedId.Value, platform.Name));
        }

        TempData["Success"] = localizer["Platform_AdminAdded"].Value;
        TempData["InviteLink"] = inviteLink;
        if (result.EmailAttempted && !result.EmailSent)
            TempData["EmailFailed"] = true;
        return RedirectToAction(nameof(Index), new { id = scopedId.Value });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveAdmin(int? id, string userId)
    {
        var scopedId = await ResolveScopedPlatformIdAsync(id);
        if (scopedId is null) return Forbid();

        // A platform must never end up with zero admins via a PPA action.
        // SystemAdmins can override via the stronger client-side confirm dialog.
        var currentUser = await userManager.GetUserAsync(User);
        if (currentUser?.Id == userId)
        {
            TempData["Error"] = localizer["Platform_SelfRemoveBlocked"].Value;
            return RedirectToAction(nameof(Index), new { id = scopedId.Value });
        }

        var admins = await platformService.GetAdminsForPlatformAsync(scopedId.Value);
        var wouldBeLast = !admins.Any(a => a.Id != userId);
        if (wouldBeLast && !User.IsInRole("SystemAdmin"))
        {
            TempData["Error"] = localizer["Platform_LastAdminBlocked"].Value;
            return RedirectToAction(nameof(Index), new { id = scopedId.Value });
        }

        await platformService.RemoveAdminAsync(scopedId.Value, userId);
        TempData["Success"] = localizer["Platform_AdminRemoved"].Value;
        return RedirectToAction(nameof(Index), new { id = scopedId.Value });
    }

    // SystemAdmin: any id passed through. PPA: id (if provided) must match their own
    // ProjectPlatformId; if id is null, falls back to their own platform.
    private async Task<int?> ResolveScopedPlatformIdAsync(int? requestedId)
    {
        if (User.IsInRole("SystemAdmin"))
            return requestedId;

        var currentUser = await userManager.GetUserAsync(User);
        if (currentUser?.ProjectPlatformId is null) return null;
        if (requestedId.HasValue && requestedId != currentUser.ProjectPlatformId) return null;
        return currentUser.ProjectPlatformId;
    }

    private async Task<PlatformAdminsViewModel> BuildViewModel(int id, string platformName)
    {
        var admins = await platformService.GetAdminsForPlatformAsync(id);
        return new PlatformAdminsViewModel(
            id,
            platformName,
            admins.Select(a => new AdminSummaryViewModel
            {
                UserId = a.Id,
                UserName = a.UserName ?? a.Email ?? string.Empty,
                Email = a.Email ?? string.Empty
            }).ToList()
        );
    }
}

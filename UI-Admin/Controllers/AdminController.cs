using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.Admin.Models.Admin;
using SynapseEnqueteManagement.UI.Admin.Models.Platform;
using SynapseEnqueteManagement.UI.Admin.Services.Platform;

namespace SynapseEnqueteManagement.UI.Admin.Controllers;

[Authorize(Roles = "SystemAdmin")]
public class AdminController(PlatformAdminService platformService) : Controller
{
    public async Task<IActionResult> Index(int? platformId = null)
    {
        var platforms = await platformService.GetAllPlatformsAsync();
        var allAdmins = await platformService.GetAllAdminsAsync();

        var filtered = platformId.HasValue
            ? allAdmins.Where(a => a.ProjectPlatformId == platformId).ToList()
            : allAdmins;

        // Resolve platform names through the already-fetched platform list — UI-Admin
        // no longer has an EF navigation from ApplicationUser to ProjectPlatform.
        var platformNameById = platforms.ToDictionary(p => p.ProjectPlatformId, p => p.Name);

        ViewData["ActiveSection"] = "Admins";

        return View(new AdminListViewModel(
            filtered.Select(a => new AdminWithPlatformViewModel(
                a.Id,
                a.UserName ?? a.Email ?? string.Empty,
                a.Email ?? string.Empty,
                a.ProjectPlatformId ?? 0,
                a.ProjectPlatformId is int pid && platformNameById.TryGetValue(pid, out var name) ? name : "—"
            )).ToList(),
            platforms.Select(p => new PlatformViewModel
            {
                ProjectPlatformId = p.ProjectPlatformId,
                Name = p.Name
            }).ToList(),
            platformId
        ));
    }

}
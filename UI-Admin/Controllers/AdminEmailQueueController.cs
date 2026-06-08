using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.Admin.Data;
using SynapseEnqueteManagement.UI.Admin.Models.Admin;
using SynapseEnqueteManagement.UI.Admin.Services.Email;

namespace SynapseEnqueteManagement.UI.Admin.Controllers;

// EmailJob queue view. SystemAdmin sees every platform's jobs; a
// ProjectPlatformAdmin sees only their own platform's jobs and can retry only
// rows belonging to it. This controller is the authorization tier: it resolves
// the caller's platform from their identity and passes it as the scope to the
// API, which authenticates by a shared key and has no per-user context.
// Participant recipients live in BCC and are never surfaced; the visible "To"
// is the sending/invited admin.
[Authorize(Roles = "SystemAdmin,ProjectPlatformAdmin")]
public class AdminEmailQueueController(
    EmailApiService emailApi,
    UserManager<ApplicationUser> userManager) : Controller
{
    private const int Take = 100;

    // null => SystemAdmin (all platforms). A PPA is pinned to their own
    // platform; Ok is false when a PPA has no platform assigned.
    private async Task<(bool Ok, int? PlatformId)> ResolveScopeAsync()
    {
        if (User.IsInRole("SystemAdmin")) return (true, null);
        var user = await userManager.GetUserAsync(User);
        if (user?.ProjectPlatformId is null) return (false, null);
        return (true, user.ProjectPlatformId);
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var (ok, platformId) = await ResolveScopeAsync();
        if (!ok) return Forbid();

        // Three API calls in parallel — independent reads, scoped by status
        // and (for a PPA) by platform.
        var pendingTask  = emailApi.ListQueueAsync("Pending", platformId, take: Take, ct);
        var givenUpTask  = emailApi.ListQueueAsync("Failed",  platformId, take: Take, ct);
        var recentTask   = emailApi.ListQueueAsync("Sent",    platformId, take: Take, ct);
        await Task.WhenAll(pendingTask, givenUpTask, recentTask);

        ViewData["ActiveSection"] = "EmailQueue";
        return View(new EmailQueueViewModel(
            Pending:    await pendingTask,
            GivenUp:    await givenUpTask,
            RecentSent: await recentTask));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Retry(int id, CancellationToken ct)
    {
        var (ok, platformId) = await ResolveScopeAsync();
        if (!ok) return Forbid();

        await emailApi.RetryAsync(id, platformId, ct);
        return RedirectToAction(nameof(Index));
    }
}

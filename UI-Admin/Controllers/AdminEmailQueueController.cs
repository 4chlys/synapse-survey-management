using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.Admin.Models.Admin;
using SynapseEnqueteManagement.UI.Admin.Services.Email;

namespace SynapseEnqueteManagement.UI.Admin.Controllers;

// Operator-only view of the EmailJob queue. SystemAdmin scope keeps the
// retry button (and recipient visibility) out of platform-admin hands; a
// PPA-scoped variant can land later.
[Authorize(Roles = "SystemAdmin")]
public class AdminEmailQueueController(EmailApiService emailApi) : Controller
{
    private const int Take = 100;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        // Three API calls in parallel — independent reads, scoped by status.
        var pendingTask  = emailApi.ListQueueAsync("Pending", projectPlatformId: null, take: Take, ct);
        var givenUpTask  = emailApi.ListQueueAsync("Failed",  projectPlatformId: null, take: Take, ct);
        var recentTask   = emailApi.ListQueueAsync("Sent",    projectPlatformId: null, take: Take, ct);
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
        await emailApi.RetryAsync(id, ct);
        return RedirectToAction(nameof(Index));
    }
}

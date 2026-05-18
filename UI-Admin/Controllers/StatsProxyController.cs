using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.Admin.Data;
using SynapseEnqueteManagement.UI.Admin.Services.Dashboard;
using SynapseEnqueteManagement.UI.Admin.Services.Project;

namespace SynapseEnqueteManagement.UI.Admin.Controllers;

[Authorize]
[Route("api/projects/{projectId:int}")]
public class StatsProxyController(
    StatsProxyService proxy,
    ProjectAdminService projectService,
    UserManager<ApplicationUser> userManager) : Controller
{
    private async Task<bool> CanAccessProjectAsync(int projectId)
    {
        if (User.IsInRole("SystemAdmin"))
            return true;

        var user = await userManager.GetUserAsync(User);
        if (user?.ProjectPlatformId is null)
            return false;

        var project = await projectService.GetProjectByIdAsync(projectId);
        return project?.PlatformId == user.ProjectPlatformId;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(int projectId)
    {
        if (!await CanAccessProjectAsync(projectId))
            return Forbid();

        var (status, body) = await proxy.ProxyGetAsync($"api/projects/{projectId}/dashboard");
        return new ContentResult { StatusCode = status, Content = body, ContentType = "application/json" };
    }

    [HttpGet("stats/timeline")]
    public async Task<IActionResult> GetTimeline(int projectId, [FromQuery] int days = 30)
    {
        if (!await CanAccessProjectAsync(projectId))
            return Forbid();

        var (status, body) = await proxy.ProxyGetAsync($"api/projects/{projectId}/stats/timeline?days={days}");
        return new ContentResult { StatusCode = status, Content = body, ContentType = "application/json" };
    }

    [HttpGet("stats/idea-feed")]
    public async Task<IActionResult> GetIdeaFeed(int projectId, [FromQuery] int count = 10)
    {
        if (!await CanAccessProjectAsync(projectId))
            return Forbid();

        var (status, body) = await proxy.ProxyGetAsync($"api/projects/{projectId}/stats/idea-feed?count={count}");
        return new ContentResult { StatusCode = status, Content = body, ContentType = "application/json" };
    }

    [HttpGet("stats/moderation-queue")]
    public async Task<IActionResult> GetModerationQueue(int projectId)
    {
        if (!await CanAccessProjectAsync(projectId))
            return Forbid();

        var (status, body) = await proxy.ProxyGetAsync($"api/projects/{projectId}/stats/moderation-queue");
        return new ContentResult { StatusCode = status, Content = body, ContentType = "application/json" };
    }

    [HttpGet("stats/export")]
    public async Task<IActionResult> ExportStats(
        int projectId,
        [FromQuery] string format,
        [FromQuery] string sections,
        [FromQuery] bool includeGraphics = false)
    {
        if (!await CanAccessProjectAsync(projectId))
            return Forbid();

        var parts = new List<string>();
        if (!string.IsNullOrEmpty(format))
            parts.Add($"format={Uri.EscapeDataString(format)}");
        if (!string.IsNullOrEmpty(sections))
            parts.Add($"sections={Uri.EscapeDataString(sections)}");
        if (includeGraphics)
            parts.Add("includeGraphics=true");
        var qs = parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;

        var (status, stream, contentType, fileName) =
            await proxy.ProxyGetFileAsync($"api/projects/{projectId}/stats/export{qs}");

        if (status != 200)
            return StatusCode(status);

        return new FileStreamResult(stream, contentType) { FileDownloadName = fileName };
    }
}

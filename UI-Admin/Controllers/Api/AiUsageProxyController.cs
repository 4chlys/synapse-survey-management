using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.Admin.Data;
using SynapseEnqueteManagement.UI.Admin.Services.Dashboard;
using SynapseEnqueteManagement.UI.Admin.Services.Project;

namespace SynapseEnqueteManagement.UI.Admin.Controllers.Api;

[Authorize]
[Route("api/admin/ai-usage")]
public class AiUsageProxyController(
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

    [HttpGet("projects")]
    public async Task<IActionResult> GetProjects([FromQuery] string from = null)
    {
        if (!User.IsInRole("SystemAdmin"))
            return Forbid();

        var path = string.IsNullOrEmpty(from)
            ? "api/admin/ai-usage/projects"
            : $"api/admin/ai-usage/projects?from={Uri.EscapeDataString(from)}";
        var (status, body) = await proxy.ProxyGetAsync(path);
        return new ContentResult { StatusCode = status, Content = body, ContentType = "application/json" };
    }

    [HttpGet("platforms")]
    public async Task<IActionResult> GetPlatforms([FromQuery] string from = null)
    {
        if (!User.IsInRole("SystemAdmin"))
            return Forbid();

        var path = string.IsNullOrEmpty(from)
            ? "api/admin/ai-usage/platforms"
            : $"api/admin/ai-usage/platforms?from={Uri.EscapeDataString(from)}";
        var (status, body) = await proxy.ProxyGetAsync(path);
        return new ContentResult { StatusCode = status, Content = body, ContentType = "application/json" };
    }

    [HttpGet("projects/{id:int}/by-service")]
    public async Task<IActionResult> GetByService(int id, [FromQuery] string from = null)
    {
        if (!await CanAccessProjectAsync(id))
            return Forbid();

        var path = string.IsNullOrEmpty(from)
            ? $"api/admin/ai-usage/projects/{id}/by-service"
            : $"api/admin/ai-usage/projects/{id}/by-service?from={Uri.EscapeDataString(from)}";
        var (status, body) = await proxy.ProxyGetAsync(path);
        return new ContentResult { StatusCode = status, Content = body, ContentType = "application/json" };
    }
}

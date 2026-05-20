using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.Admin.Data;
using SynapseEnqueteManagement.UI.Admin.Services.Dashboard;

namespace SynapseEnqueteManagement.UI.Admin.Controllers;

[Authorize(Roles = "SystemAdmin,ProjectPlatformAdmin")]
[Route("api/system-stats")]
public class SystemStatsProxyController(
    StatsProxyService proxy,
    UserManager<ApplicationUser> userManager) : Controller
{
    private async Task<(bool Allowed, int? PlatformId)> ResolvePlatformIdAsync(int? requested)
    {
        if (User.IsInRole("SystemAdmin"))
            return (true, requested);

        var user = await userManager.GetUserAsync(User);
        if (user?.ProjectPlatformId is null)
            return (false, null);

        return (true, user.ProjectPlatformId);
    }

    [HttpGet]
    public async Task<IActionResult> GetSystemStats([FromQuery] int? platformId, [FromQuery] DateTime? aiFrom)
    {
        var (allowed, resolvedPlatformId) = await ResolvePlatformIdAsync(platformId);
        if (!allowed)
            return Forbid();

        var qs = BuildStatsQuery(resolvedPlatformId, aiFrom);
        var (status, body) = await proxy.ProxyGetAsync($"api/admin/platform-stats/system{qs}");
        return new ContentResult { StatusCode = status, Content = body, ContentType = "application/json" };
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportSystemStats(
        [FromQuery] int? platformId,
        [FromQuery] string format,
        [FromQuery] string sections,
        [FromQuery] bool includeGraphics = false,
        [FromQuery] DateTime? aiFrom = null)
    {
        var (allowed, resolvedPlatformId) = await ResolvePlatformIdAsync(platformId);
        if (!allowed)
            return Forbid();

        var qs = BuildExportQuery(resolvedPlatformId, format, sections, includeGraphics, aiFrom);
        var (status, stream, contentType, fileName) =
            await proxy.ProxyGetFileAsync($"api/admin/platform-stats/system/export{qs}");

        if (status != 200)
            return StatusCode(status);

        return new FileStreamResult(stream, contentType) { FileDownloadName = fileName };
    }

    private static string BuildStatsQuery(int? platformId, DateTime? aiFrom)
    {
        var parts = new List<string>();
        if (platformId.HasValue)
            parts.Add($"platformId={platformId.Value}");
        if (aiFrom.HasValue)
            parts.Add($"aiFrom={Uri.EscapeDataString(aiFrom.Value.ToString("o"))}");
        return parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
    }

    private static string BuildExportQuery(
        int? platformId, string format, string sections,
        bool includeGraphics, DateTime? aiFrom)
    {
        var parts = new List<string>();
        if (platformId.HasValue)
            parts.Add($"platformId={platformId.Value}");
        if (!string.IsNullOrEmpty(format))
            parts.Add($"format={Uri.EscapeDataString(format)}");
        if (!string.IsNullOrEmpty(sections))
            parts.Add($"sections={Uri.EscapeDataString(sections)}");
        if (includeGraphics)
            parts.Add("includeGraphics=true");
        if (aiFrom.HasValue)
            parts.Add($"aiFrom={Uri.EscapeDataString(aiFrom.Value.ToString("o"))}");
        return parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
    }
}

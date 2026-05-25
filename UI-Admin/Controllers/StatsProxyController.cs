using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.Admin.Filters;
using SynapseEnqueteManagement.UI.Admin.Services.Dashboard;

namespace SynapseEnqueteManagement.UI.Admin.Controllers;

[Authorize(Roles = "ProjectPlatformAdmin")]
[RequireProjectPlatformOwnership]
[Route("api/projects/{projectId:int}")]
public class StatsProxyController(StatsProxyService proxy) : Controller
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(int projectId)
    {
        var (status, body) = await proxy.ProxyGetAsync($"api/projects/{projectId}/dashboard");
        return new ContentResult { StatusCode = status, Content = body, ContentType = "application/json" };
    }

    [HttpGet("stats/timeline")]
    public async Task<IActionResult> GetTimeline(int projectId, [FromQuery] int days = 30)
    {
        var (status, body) = await proxy.ProxyGetAsync($"api/projects/{projectId}/stats/timeline?days={days}");
        return new ContentResult { StatusCode = status, Content = body, ContentType = "application/json" };
    }

    [HttpGet("stats/idea-feed")]
    public async Task<IActionResult> GetIdeaFeed(int projectId, [FromQuery] int count = 10)
    {
        var (status, body) = await proxy.ProxyGetAsync($"api/projects/{projectId}/stats/idea-feed?count={count}");
        return new ContentResult { StatusCode = status, Content = body, ContentType = "application/json" };
    }

    [HttpGet("stats/moderation-queue")]
    public async Task<IActionResult> GetModerationQueue(int projectId)
    {
        var (status, body) = await proxy.ProxyGetAsync($"api/projects/{projectId}/stats/moderation-queue");
        return new ContentResult { StatusCode = status, Content = body, ContentType = "application/json" };
    }

    [HttpGet("stats/questions")]
    public async Task<IActionResult> GetQuestionLookup(int projectId, [FromQuery] string format = null)
    {
        var qs = format is not null ? $"?format={Uri.EscapeDataString(format)}" : string.Empty;
        var (status, body) = await proxy.ProxyGetAsync($"api/projects/{projectId}/questions{qs}");
        return new ContentResult { StatusCode = status, Content = body, ContentType = "application/json" };
    }

    [HttpGet("stats/questions/{questionId:int}/single-stats")]
    public async Task<IActionResult> GetQuestionSingleStats(int projectId, int questionId)
    {
        var (status, body) = await proxy.ProxyGetAsync($"api/projects/{projectId}/questions/{questionId}/single-stats");
        return new ContentResult { StatusCode = status, Content = body, ContentType = "application/json" };
    }

    [HttpGet("stats/questions/{questionId:int}/multi-stats")]
    public async Task<IActionResult> GetQuestionMultiStats(int projectId, int questionId)
    {
        var (status, body) = await proxy.ProxyGetAsync($"api/projects/{projectId}/questions/{questionId}/multi-stats");
        return new ContentResult { StatusCode = status, Content = body, ContentType = "application/json" };
    }

    [HttpGet("stats/questions/{questionId:int}/likert-stats")]
    public async Task<IActionResult> GetQuestionLikertStats(int projectId, int questionId)
    {
        var (status, body) = await proxy.ProxyGetAsync($"api/projects/{projectId}/questions/{questionId}/likert-stats");
        return new ContentResult { StatusCode = status, Content = body, ContentType = "application/json" };
    }

    [HttpGet("stats/questions/{questionId:int}/open-answers")]
    public async Task<IActionResult> GetQuestionOpenAnswers(
        int projectId,
        int questionId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var (status, body) = await proxy.ProxyGetAsync(
            $"api/projects/{projectId}/questions/{questionId}/open-answers?skip={skip}&take={take}");
        return new ContentResult { StatusCode = status, Content = body, ContentType = "application/json" };
    }

    [HttpGet("stats/export")]
    public async Task<IActionResult> ExportStats(
        int projectId,
        [FromQuery] string format,
        [FromQuery] string sections,
        [FromQuery] bool includeGraphics = false)
    {
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

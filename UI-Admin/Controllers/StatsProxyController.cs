using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.Admin.Services.Dashboard;

namespace SynapseEnqueteManagement.UI.Admin.Controllers;

[Authorize]
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
}

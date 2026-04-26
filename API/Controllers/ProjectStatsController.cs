using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Stats;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/stats")]
public class ProjectStatsController(
    IParticipantManager participantManager,
    ICategoryManager categoryManager,
    IProjectManager projectManager) : ControllerBase
{
    [HttpGet("timeline")]
    public async Task<ActionResult<TimelineDto>> GetTimeline(int projectId, [FromQuery] int days = 30)
    {
        if (await projectManager.GetProjectByIdReadOnlyAsync(projectId) is null)
            return NotFound();

        var result = await participantManager.GetTimelineAsync(projectId, Math.Clamp(days, 7, 90));
        return Ok(new TimelineDto
        {
            Dates = result.Dates,
            Starts = result.Starts,
            Completions = result.Completions
        });
    }

    [HttpGet("idea-feed")]
    public async Task<ActionResult<List<IdeaFeedItemDto>>> GetIdeaFeed(int projectId, [FromQuery] int count = 10)
    {
        if (await projectManager.GetProjectByIdReadOnlyAsync(projectId) is null)
            return NotFound();

        var items = await categoryManager.GetIdeaFeedAsync(projectId, Math.Clamp(count, 1, 20));
        return Ok(items.Select(i => new IdeaFeedItemDto
        {
            IdeaId = i.IdeaId,
            Description = i.Description,
            CategoryName = i.CategoryName,
            ReactionCount = i.ReactionCount,
            CreatedAt = i.CreatedAt
        }));
    }

    [HttpGet("moderation-queue")]
    public async Task<ActionResult<ModerationQueueDto>> GetModerationQueue(int projectId)
    {
        if (await projectManager.GetProjectByIdReadOnlyAsync(projectId) is null)
            return NotFound();

        var result = await categoryManager.GetModerationQueueAsync(projectId);
        return Ok(new ModerationQueueDto
        {
            Approved = result.Approved,
            Declined = result.Declined,
            Flagged = result.Flagged,
            Unflagged = result.Unflagged
        });
    }
}

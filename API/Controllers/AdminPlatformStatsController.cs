using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Stats;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/admin/platform-stats")]
public class AdminPlatformStatsController(
    IProjectManager projectManager,
    IParticipantManager participantManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PlatformStatsDto>> GetStats([FromQuery] int platformId)
    {
        var projects = (await projectManager.GetAllProjectsAsync(platformId)).ToList();
        var counts = await Task.WhenAll(
            projects.Select(p => participantManager.CountCompletionsByProjectAsync(p.ProjectId)));

        return Ok(new PlatformStatsDto
        {
            ActiveCount   = projects.Count(p => p.ProjectState == ProjectState.Active),
            DraftCount    = projects.Count(p => p.ProjectState == ProjectState.Draft),
            ArchivedCount = projects.Count(p => p.ProjectState == ProjectState.Archived),
            ParticipantCount = counts.Sum()
        });
    }
}
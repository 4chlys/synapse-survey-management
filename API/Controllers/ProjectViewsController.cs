using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/view")]
public class ProjectViewsController(IProjectManager projectManager) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> TrackView(int projectId)
    {
        await projectManager.IncrementViewCountAsync(projectId);
        return NoContent();
    }
}

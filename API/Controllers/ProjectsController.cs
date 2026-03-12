using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs;
using SynapseEnqueteManagement.BL;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectManager _projectManager;

    public ProjectsController(IProjectManager projectManager)
    {
        _projectManager = projectManager;
    }

    [HttpGet("{projectId}")]
    public async Task<ActionResult<ProjectDto>> GetProject(int projectId)
    {
        var project = await _projectManager.GetProjectByIdAsync(projectId);
        if (project is null)
            return NotFound();

        return Ok(new ProjectDto
        {
            ProjectId = project.ProjectId,
            ProjectState = project.ProjectState.ToString(),
            FormType = project.FormType.ToString(),
            OrganisationName = project.OrganisationName ?? string.Empty,
            OrganisationLogoUrl = project.OrganisationLogoUrl ?? string.Empty,
            OrganisationWebsiteUrl = project.OrganisationWebsiteUrl ?? string.Empty,
        });
    }
}

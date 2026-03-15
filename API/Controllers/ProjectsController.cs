using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Project;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController(IProjectManager projectManager) : ControllerBase
{
    [HttpGet("{projectId}")]
    public async Task<ActionResult<ProjectDto>> GetProject(int projectId)
    {
        var project = await projectManager.GetProjectByIdAsync(projectId);
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
            Introduction = project.Introduction?.Text ?? string.Empty
        });
    }
}
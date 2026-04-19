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
            Name = project.Name ?? string.Empty,
            ProjectImage = project.ImageUrl ?? string.Empty,
            ProjectState = project.ProjectState.ToString(),
            FormType = project.FormType.ToString(),
            OrganisationName = project.OrganisationName ?? string.Empty,
            OrganisationLogoUrl = project.OrganisationLogoUrl ?? string.Empty,
            OrganisationWebsiteUrl = project.OrganisationWebsiteUrl ?? string.Empty,
            Introduction = project.Introduction?.Text ?? string.Empty,
            RequestContactInfo = project.RequestContactInfo
        });
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetAllProjects()
    {
        var projects = await projectManager.GetAllProjectsAsync();
        return Ok(projects.Select(p => new ProjectDto
        {
            ProjectId = p.ProjectId,
            Name = p.Name ?? string.Empty,
            ProjectImage = p.ImageUrl ?? string.Empty,
            ProjectState = p.ProjectState.ToString(),
            FormType = p.FormType.ToString(),
            OrganisationName = p.OrganisationName ?? string.Empty,
            OrganisationLogoUrl = p.OrganisationLogoUrl ?? string.Empty,
            OrganisationWebsiteUrl = p.OrganisationWebsiteUrl ?? string.Empty,
            Introduction = p.Introduction?.Text ?? string.Empty,
            RequestContactInfo = p.RequestContactInfo
        }));
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> CreateProject([FromBody] 
        CreateProjectDto dto)
    { var project = await projectManager.CreateProjectAsync(dto.Name,
            dto.ImageUrl);
        var result = new ProjectDto
        {
            ProjectId = project.ProjectId,
            ProjectState = project.ProjectState.ToString(),
            FormType = project.FormType.ToString(),
            OrganisationName = project.OrganisationName ?? string.Empty,
            OrganisationLogoUrl = project.OrganisationLogoUrl ?? string.Empty,
            OrganisationWebsiteUrl = string.Empty,
            Introduction = string.Empty,
            RequestContactInfo = false
        };
        return Created(string.Empty, result);
        
    }
}
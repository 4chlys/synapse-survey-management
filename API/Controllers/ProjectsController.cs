using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Project;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController(IProjectManager projectManager) : ControllerBase
{
    private static FormType ParseFormType(string interactionMode) =>
        interactionMode?.ToLower() == "chat" ? FormType.Chat : FormType.VerticalScroll;

    private ProjectDto MapToDto(BL.Domain.Project.Entities.Project p) => new()
    {
        ProjectId = p.ProjectId,
        Name = p.Name ?? string.Empty,
        Topic = p.Topic ?? string.Empty,
        ProjectImage = p.ImageUrl ?? string.Empty,
        ProjectState = p.ProjectState.ToString(),
        FormType = p.FormType.ToString(),
        OrganisationName = p.OrganisationName ?? string.Empty,
        OrganisationLogoUrl = p.OrganisationLogoUrl ?? string.Empty,
        OrganisationWebsiteUrl = p.OrganisationWebsiteUrl ?? string.Empty,
        Introduction = p.Introduction?.Text ?? string.Empty,
        AiAssistance = p.AiAssistance ?? "ProActive",
        RequestContactInfo = p.RequestContactInfo,
        MinAge = p.MinAge,
        MaxAge = p.MaxAge
    };

    [HttpGet("{projectId}")]
    public async Task<ActionResult<ProjectDto>> GetProject(int projectId)
    {
        var project = await projectManager.GetProjectByIdAsync(projectId);
        if (project is null)
            return NotFound();

        return Ok(MapToDto(project));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetAllProjects()
    {
        var projects = await projectManager.GetAllProjectsAsync();
        return Ok(projects.Select(MapToDto));
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> CreateProject([FromBody] CreateProjectDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var project = await projectManager.CreateProjectAsync(
            dto.Name,
            dto.Topic ?? string.Empty,
            dto.Introduction ?? string.Empty,
            dto.ImageUrl ?? string.Empty,
            ParseFormType(dto.InteractionMode),
            dto.AiAssistance ?? "ProActive",
            dto.MinAge,
            dto.MaxAge,
            dto.RequestContactInfo);

        return Created(string.Empty, MapToDto(project));
    }

    [HttpPut("{projectId}")]
    public async Task<ActionResult<ProjectDto>> UpdateProject(int projectId, [FromBody] UpdateProjectDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var project = await projectManager.UpdateProjectAsync(
            projectId,
            dto.Name,
            dto.Topic ?? string.Empty,
            dto.Introduction ?? string.Empty,
            dto.ImageUrl ?? string.Empty,
            ParseFormType(dto.InteractionMode),
            dto.AiAssistance ?? "ProActive",
            dto.MinAge,
            dto.MaxAge,
            dto.RequestContactInfo);

        return Ok(MapToDto(project));
    }
}

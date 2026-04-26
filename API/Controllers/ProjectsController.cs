using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Project;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController(IProjectManager projectManager) : ControllerBase
{
    private static FormType ParseFormType(string interactionMode) =>
        Enum.TryParse<FormType>(interactionMode, ignoreCase: true, out var formResult) ? formResult : FormType.VerticalScroll;

    private static AiAssistanceType ParseAiAssistanceType(string aiAssistance) =>
        Enum.TryParse<AiAssistanceType>(aiAssistance, ignoreCase: true, out var aiResult) ? aiResult : AiAssistanceType.ProActive;

    private ProjectDto MapToDto(Project p) => new()
    {
        ProjectId = p.ProjectId,
        Name = p.Name ?? string.Empty,
        ShortForm = p.ShortForm ?? string.Empty,
        Topic = p.Topic ?? string.Empty,
        ProjectState = p.ProjectState.ToString(),
        FormType = p.FormType.ToString(),
        OrganisationName = p.OrganisationName ?? string.Empty,
        ProjectImage = p.ImageUrl ?? string.Empty,
        OrganisationLogoUrl = p.OrganisationLogoUrl ?? string.Empty,
        OrganisationWebsiteUrl = p.OrganisationWebsiteUrl ?? string.Empty,
        Introduction = p.Introduction?.Text ?? string.Empty,
        AiAssistance = p.AiAssistance.ToString(),
        RequestContactInfo = p.RequestContactInfo,
        MinAge = p.MinAge,
        MaxAge = p.MaxAge
    };

    [HttpGet("{projectId}")]
    public async Task<ActionResult<ProjectDto>> GetProject(int projectId)
    {
        var project = await projectManager.GetProjectByIdWithIntroductionReadOnlyAsync(projectId);
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

    [HttpGet("by-shortform/{shortForm}")]
    public async Task<ActionResult<ProjectShortFormLookupDto>> GetByShortForm(string shortForm)
    {
        var project = await projectManager.GetProjectByShortFormAsync(shortForm.ToLowerInvariant());
        if (project is null)
            return NotFound();

        return Ok(new ProjectShortFormLookupDto
        {
            ProjectId = project.ProjectId,
            ShortForm = project.ShortForm,
            ProjectState = project.ProjectState.ToString()
        });
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> CreateProject([FromBody] CreateProjectDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var project = await projectManager.CreateProjectAsync(
            dto.Name,
            dto.Topic ?? string.Empty,
            dto.Introduction ?? string.Empty,
            dto.ImageUrl ?? string.Empty,
            ParseFormType(dto.InteractionMode),
            ParseAiAssistanceType(dto.AiAssistance),
            dto.MinAge,
            dto.MaxAge,
            dto.RequestContactInfo,
            dto.ShortForm,
            ct);

        return Created(string.Empty, MapToDto(project));
    }

    [HttpPut("{projectId}")]
    public async Task<ActionResult<ProjectDto>> UpdateProject(int projectId, [FromBody] UpdateProjectDto dto, CancellationToken ct)
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
            ParseAiAssistanceType(dto.AiAssistance),
            dto.MinAge,
            dto.MaxAge,
            dto.RequestContactInfo,
            dto.ShortForm,
            ct);

        return Ok(MapToDto(project));
    }
}

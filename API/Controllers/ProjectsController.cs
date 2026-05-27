using Ganss.Xss;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Project;
using SynapseEnqueteManagement.API.Services;
using SynapseEnqueteManagement.BL;
using SynapseEnqueteManagement.BL.Exceptions;
using SynapseEnqueteManagement.BL.AiImageGeneration;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController(
    IProjectManager projectManager,
    IAiImageGenerationService imageGenerationService,
    IStorageService storageService) : ControllerBase
{
    private static FormType ParseFormType(string interactionMode) =>
        Enum.TryParse<FormType>(interactionMode, ignoreCase: true, out var formResult)
            ? formResult
            : FormType.VerticalScroll;

    private static ProjectTheme ParseTheme(string theme) =>
        Enum.TryParse<ProjectTheme>(theme, ignoreCase: true, out var themeResult)
            ? themeResult
            : ProjectTheme.Minimal;

    private static IEnumerable<ProjectTranslationInput> MapTranslations(ICollection<ProjectTranslationDto> dtos) =>
        dtos.Select(t => new ProjectTranslationInput(t.LanguageCode, t.Name, t.Topic ?? string.Empty));

    // Shared sanitizer instance — HtmlSanitizer is thread-safe and lightweight to reuse.
    // Default allowlist retains the tags Quill emits (p, br, strong, em, a, ul, ol, li, u)
    // and strips <script>, event-handler attributes, and javascript: hrefs.
    private static readonly HtmlSanitizer _introSanitizer = new();

    private static IEnumerable<IntroductionTranslationInput> MapIntroductions(ICollection<ProjectTranslationDto> dtos) =>
        dtos.Where(t => !string.IsNullOrEmpty(t.Introduction))
            .Select(t => new IntroductionTranslationInput(t.LanguageCode, _introSanitizer.Sanitize(t.Introduction)));

    [HttpGet("{projectId}")]
    public async Task<ActionResult<ProjectDto>> GetProject(int projectId)
    {
        var project = await projectManager.GetProjectByIdWithIntroductionReadOnlyAsync(projectId);
        if (project is null)
            return NotFound();

        return Ok(ProjectDtoMapper.ToParticipantDto(project));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetAllProjects(
        [FromQuery] int? platformId = null)
    {
        var projects = await projectManager.GetAllProjectsAsync(platformId);
        return Ok(projects.Select(ProjectDtoMapper.ToParticipantDto));
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
    public async Task<ActionResult<AdminProjectDto>> CreateProject(CreateProjectDto dto, CancellationToken ct)
    {
        var imageUrl = dto.ImageUrl ?? string.Empty;

        var project = await projectManager.CreateProjectAsync(
            MapTranslations(dto.Translations),
            MapIntroductions(dto.Translations),
            dto.DefaultLanguageCode,
            dto.SupportedLanguageCodes,
            imageUrl,
            ParseFormType(dto.InteractionMode),
            dto.MinAge,
            dto.MaxAge,
            dto.RequestContactInfo,
            dto.PlatformId,
            dto.ShortForm,
            dto.IdeaPageSize,
            dto.ExtraIdeasReplaceExisting,
            dto.MaxExtraIdeaRequests,
            dto.AllowCrossLanguageIdeas,
            ParseTheme(dto.Theme),
            dto.PrimaryColor,
            dto.CustomCss);

        return Created(string.Empty, ProjectDtoMapper.ToAdminDto(project));
    }

    [HttpPut("{projectId}")]
    public async Task<ActionResult<AdminProjectDto>> UpdateProject(int projectId, UpdateProjectDto dto)
    {
        var project = await projectManager.UpdateProjectAsync(
            projectId,
            MapTranslations(dto.Translations),
            MapIntroductions(dto.Translations),
            dto.DefaultLanguageCode,
            dto.SupportedLanguageCodes,
            dto.ImageUrl ?? string.Empty,
            ParseFormType(dto.InteractionMode),
            dto.MinAge,
            dto.MaxAge,
            dto.RequestContactInfo,
            dto.ShortForm,
            dto.IdeaPageSize,
            dto.ExtraIdeasReplaceExisting,
            dto.MaxExtraIdeaRequests,
            dto.AllowCrossLanguageIdeas,
            ParseTheme(dto.Theme),
            dto.PrimaryColor,
            dto.CustomCss);

        return Ok(ProjectDtoMapper.ToAdminDto(project));
    }

    [HttpPost("{projectId:int}/copy")]
    public async Task<ActionResult<AdminProjectDto>> CopyProject(int projectId, CopyProjectDto dto, CancellationToken ct)
    {
        var source = await projectManager.GetProjectByIdReadOnlyAsync(projectId);
        if (source is null) return NotFound();
        if (source.ProjectPlatformId != dto.PlatformId) return Forbid();

        var copy = await projectManager.DuplicateProjectAsync(
            projectId, MapTranslations(dto.Translations), dto.PlatformId, dto.NewShortForm, ct);
        return CreatedAtAction(nameof(GetProject), new { projectId = copy.ProjectId }, ProjectDtoMapper.ToAdminDto(copy));
    }

    [HttpDelete("{projectId}")]
    public async Task<IActionResult> DeleteProject(int projectId)
    {
        await projectManager.DeleteProjectAsync(projectId);
        return NoContent();
    }

    [HttpPatch("{projectId}/state")]
    public async Task<ActionResult<AdminProjectDto>> UpdateProjectState(int projectId, UpdateProjectStateDto dto)
    {
        if (!Enum.TryParse<ProjectState>(dto.State, ignoreCase: true, out var newState))
            return BadRequest(new { error = "Invalid project state." });

        try
        {
            var project = await projectManager.UpdateProjectStateAsync(projectId, newState);
            return Ok(ProjectDtoMapper.ToAdminDto(project));
        }
        catch (ProjectActivationException ex)
        {
            return BadRequest(new
            {
                problems = ex.Problems.Select(p => new { p.EntityType, p.EntityId, p.LanguageCode })
            });
        }
    }

    [HttpPatch("{projectId}/wizard-step")]
    public async Task<IActionResult> UpdateWizardStep(int projectId, UpdateWizardStepDto dto)
    {
        await projectManager.UpdateLastWizardStepAsync(projectId, dto.Step);
        return NoContent();
    }

    [HttpPost("generate-image")]
    public async Task<IActionResult> GenerateImage(
        GenerateImageRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request?.Name))
            throw new LocalizedValidationException("Validation.Project.NameRequired");

        var result = await imageGenerationService.GenerateProjectImageAsync(request.Name, 0, request.Topic, request.Intro, ct: ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        var extension = result.MimeType == "image/jpeg" ? ".jpg" : ".png";
        var url = await storageService.UploadBytesAsync(
            result.ImageBytes, "projects", $"generated{extension}", result.MimeType);

        return Ok(new { url });
    }

}

public sealed record GenerateImageRequest(string Name, string Topic, string Intro);

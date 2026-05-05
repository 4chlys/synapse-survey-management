using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Project;
using SynapseEnqueteManagement.API.Services;
using SynapseEnqueteManagement.BL.AiImageGeneration;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
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

    private static AiAssistanceType ParseAiAssistanceType(string aiAssistance) =>
        Enum.TryParse<AiAssistanceType>(aiAssistance, ignoreCase: true, out var aiResult)
            ? aiResult
            : AiAssistanceType.ProActive;

    private static string ActiveCulture() =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

    private static ProjectTranslation ResolveTranslation(Project p)
    {
        var active = ActiveCulture();
        return p.Translations.FirstOrDefault(t => t.Language?.Code == active)
               ?? p.Translations.FirstOrDefault(t => t.Language?.Code == p.DefaultLanguage?.Code)
               ?? p.Translations.FirstOrDefault();
    }

    private ProjectDto MapToParticipantDto(Project p)
    {
        var t = ResolveTranslation(p);
        return new ProjectDto
        {
            ProjectId = p.ProjectId,
            PlatformId = p.ProjectPlatformId,
            Name = t?.Name ?? string.Empty,
            ShortForm = p.ShortForm ?? string.Empty,
            Topic = t?.Topic ?? string.Empty,
            ProjectState = p.ProjectState.ToString(),
            FormType = p.FormType.ToString(),
            OrganisationName = p.ProjectPlatform?.Name ?? string.Empty,
            ProjectImage = p.ImageUrl ?? string.Empty,
            OrganisationLogoUrl = p.ProjectPlatform?.LogoUrl ?? string.Empty,
            OrganisationWebsiteUrl = p.ProjectPlatform?.WebsiteUrl ?? string.Empty,
            Introduction = p.Introduction?.Text ?? string.Empty,
            AiAssistance = p.AiAssistance.ToString(),
            RequestContactInfo = p.RequestContactInfo,
            IdeaPageSize = p.IdeaPageSize,
            ExtraIdeasReplaceExisting = p.ExtraIdeasReplaceExisting,
            MaxExtraIdeaRequests = p.MaxExtraIdeaRequests,
            MinAge = p.MinAge,
            MaxAge = p.MaxAge,
            LanguageCode = t?.Language?.Code ?? p.DefaultLanguage?.Code,
            DefaultLanguageCode = p.DefaultLanguage?.Code,
            SupportedLanguageCodes = p.SupportedLanguages.Select(l => l.Code).ToList(),
        };
    }

    private AdminProjectDto MapToAdminDto(Project p) => new()
    {
        ProjectId = p.ProjectId,
        PlatformId = p.ProjectPlatformId,
        ShortForm = p.ShortForm ?? string.Empty,
        ProjectState = p.ProjectState.ToString(),
        FormType = p.FormType.ToString(),
        OrganisationName = p.ProjectPlatform?.Name ?? string.Empty,
        ProjectImage = p.ImageUrl ?? string.Empty,
        OrganisationLogoUrl = p.ProjectPlatform?.LogoUrl ?? string.Empty,
        OrganisationWebsiteUrl = p.ProjectPlatform?.WebsiteUrl ?? string.Empty,
        AiAssistance = p.AiAssistance.ToString(),
        RequestContactInfo = p.RequestContactInfo,
        IdeaPageSize = p.IdeaPageSize,
        ExtraIdeasReplaceExisting = p.ExtraIdeasReplaceExisting,
        MaxExtraIdeaRequests = p.MaxExtraIdeaRequests,
        MinAge = p.MinAge,
        MaxAge = p.MaxAge,
        DefaultLanguageCode = p.DefaultLanguage?.Code,
        SupportedLanguageCodes = p.SupportedLanguages.Select(l => l.Code).ToList(),
        Translations = p.Translations.Select(t => new ProjectTranslationDto
        {
            LanguageCode = t.Language?.Code,
            Name = t.Name,
            Topic = t.Topic,
            Introduction = t.Language?.Code == p.DefaultLanguage?.Code ? p.Introduction?.Text : null,
        }).ToList(),
    };

    private static IEnumerable<ProjectTranslationInput> MapTranslations(ICollection<ProjectTranslationDto> dtos) =>
        dtos.Select(t => new ProjectTranslationInput(t.LanguageCode, t.Name, t.Topic ?? string.Empty));

    private static IEnumerable<IntroductionTranslationInput> MapIntroductions(ICollection<ProjectTranslationDto> dtos) =>
        dtos.Where(t => !string.IsNullOrEmpty(t.Introduction))
            .Select(t => new IntroductionTranslationInput(t.LanguageCode, t.Introduction));

    [HttpGet("{projectId}")]
    public async Task<ActionResult<ProjectDto>> GetProject(int projectId)
    {
        var project = await projectManager.GetProjectByIdWithIntroductionReadOnlyAsync(projectId);
        if (project is null)
            return NotFound();

        return Ok(MapToParticipantDto(project));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetAllProjects(
        [FromQuery] int? platformId = null)
    {
        var projects = await projectManager.GetAllProjectsAsync(platformId);
        return Ok(projects.Select(MapToParticipantDto));
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
        var defaultName = dto.Translations
            .FirstOrDefault(t => t.LanguageCode == dto.DefaultLanguageCode)?.Name
            ?? dto.Translations.FirstOrDefault()?.Name
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            var result = await imageGenerationService.GenerateProjectImageAsync(defaultName, ct);
            if (result.IsSuccess)
            {
                var extension = result.MimeType == "image/jpeg" ? ".jpg" : ".png";
                imageUrl = await storageService.UploadBytesAsync(
                    result.ImageBytes, "projects", $"generated{extension}", result.MimeType);
            }
        }

        var project = await projectManager.CreateProjectAsync(
            MapTranslations(dto.Translations),
            MapIntroductions(dto.Translations),
            dto.DefaultLanguageCode,
            dto.SupportedLanguageCodes,
            imageUrl,
            ParseFormType(dto.InteractionMode),
            ParseAiAssistanceType(dto.AiAssistance),
            dto.MinAge,
            dto.MaxAge,
            dto.RequestContactInfo,
            dto.PlatformId,
            dto.ShortForm,
            dto.IdeaPageSize,
            dto.ExtraIdeasReplaceExisting,
            dto.MaxExtraIdeaRequests);

        return Created(string.Empty, MapToAdminDto(project));
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
            ParseAiAssistanceType(dto.AiAssistance),
            dto.MinAge,
            dto.MaxAge,
            dto.RequestContactInfo,
            dto.ShortForm,
            dto.IdeaPageSize,
            dto.ExtraIdeasReplaceExisting,
            dto.MaxExtraIdeaRequests);

        return Ok(MapToAdminDto(project));
    }

    [HttpPost("{projectId:int}/copy")]
    public async Task<ActionResult<AdminProjectDto>> CopyProject(int projectId, CopyProjectDto dto, CancellationToken ct)
    {
        var source = await projectManager.GetProjectByIdReadOnlyAsync(projectId);
        if (source is null) return NotFound();
        if (source.ProjectPlatformId != dto.PlatformId) return Forbid();

        var copy = await projectManager.DuplicateProjectAsync(
            projectId, MapTranslations(dto.Translations), dto.PlatformId, dto.NewShortForm, ct);
        return CreatedAtAction(nameof(GetProject), new { projectId = copy.ProjectId }, MapToAdminDto(copy));
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

        var project = await projectManager.UpdateProjectStateAsync(projectId, newState);
        return Ok(MapToAdminDto(project));
    }
}

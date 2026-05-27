using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.Ai;
using SynapseEnqueteManagement.API.DTOs.Ideation;
using SynapseEnqueteManagement.API.Services;
using SynapseEnqueteManagement.BL;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/categories/{categoryId}/ideas")]
public class IdeasController(
    IIdeaManager ideaManager,
    ICommentManager commentManager,
    IParticipantManager participantManager,
    IIdeaSubmissionManager ideaSubmissionManager,
    IProjectManager projectManager,
    IStorageService storageService,
    IPlatformAiConfigService platformAiConfig)
    : ParticipantControllerBase(participantManager, projectManager)
{
    [HttpGet]
    public async Task<ActionResult<IdeaIndexResponseDto>> GetIdeasByCategory(
        int projectId, int categoryId,
        [FromQuery][Range(1, int.MaxValue)] int page = 1,
        [FromQuery] string sort = null,
        [FromQuery] bool? showAllLanguages = null,
        CancellationToken ct = default)
    {
        var (participantId, participantLanguageCode) = await ResolveParticipantWithLanguageAsync();
        var project = await GetProjectByIdReadOnlyAsync(projectId);
        var pageSize = project?.IdeaPageSize ?? 5;

        var result = await ideaManager.GetIdeasIndexAsync(categoryId, projectId, participantId, pageSize, page, sort, participantLanguageCode, showAllLanguages ?? false, ct);

        return Ok(new IdeaIndexResponseDto
        {
            Items = result.Items.Select(i => IdeaDtoMapper.ToDto(i, participantId)),
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            ResolvedSort = result.ResolvedSort,
            HasOwnIdeas = result.HasOwnIdeas
        });
    }

    [HttpPost]
    public async Task<ActionResult<CreateIdeaResultDto>> CreateIdea(int projectId, int categoryId, CreateIdeaDto dto,
        CancellationToken ct)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        var project = await GetProjectByIdReadOnlyAsync(projectId);
        var platformConfig = await platformAiConfig.ResolveAsync(project?.ProjectPlatformId ?? 0, ct);

        var result = await ideaSubmissionManager.AddModeratedIdeaAsync(
            project, categoryId, dto.Description, dto.ModerationToken,
            dto.ImageUrl, dto.ImageModerationToken, dto.LanguageCode,
            participant, platformConfig,
            storageService.PromoteAsync,
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, ct);

        return Created(string.Empty, new CreateIdeaResultDto
        {
            IdeaId = result.IdeaId,
            ShouldAskForContactInfo = result.ShouldAskForContactInfo
        });
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IEnumerable<IdeaDto>>> GetMyIdeas(int projectId, int categoryId)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        var ideas = await ideaManager.GetParticipantIdeasByCategoryWithCommentsReadOnlyAsync(categoryId,
            participant.ParticipantId);
        return Ok(ideas.Select(i => IdeaDtoMapper.ToDto(i, participant.ParticipantId)));
    }

    [HttpGet("{ideaId}")]
    public async Task<ActionResult<IdeaDto>> GetIdeaById(int categoryId, int ideaId)
    {
        var participantId = await ResolveParticipantIdAsync();

        var idea = await ideaManager.GetIdeaByIdWithCommentsReadOnlyAsync(ideaId);
        if (idea == null) return NotFound();

        return Ok(IdeaDtoMapper.ToDto(idea, participantId));
    }

    [HttpGet("{ideaId}/comments")]
    public async Task<ActionResult<PagedResult<TextCommentDto>>> GetCommentsByIdea(
        int projectId, int ideaId, [FromQuery] [Range(1, int.MaxValue)] int page = 1, [FromQuery] string sort = null)
    {
        var project = await GetProjectByIdReadOnlyAsync(projectId);
        if (project is null) return NotFound();

        var participantId = await ResolveParticipantIdAsync();
        var ideaAuthorParticipantId = await ideaManager.GetIdeaAuthorParticipantIdReadOnlyAsync(ideaId);
        var paged = await commentManager.GetCommentsWithOwnFirstReadOnlyAsync(projectId, ideaId, page, sort, project.CommentPageSize, participantId);

        return Ok(new PagedResult<TextCommentDto>
        {
            Items = paged.Items.Select(c => IdeaDtoMapper.ToCommentDto(c, participantId, ideaAuthorParticipantId)),
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount
        });
    }


    private async Task<int?> ResolveParticipantIdAsync()
    {
        if (!TryGetParticipantToken(out var token)) return null;
        var p = await GetParticipantByTokenAsync(token);
        return p?.ParticipantId;
    }

    private async Task<(int? participantId, string participantLanguageCode)> ResolveParticipantWithLanguageAsync()
    {
        if (!TryGetParticipantToken(out var token)) return (null, null);
        var p = await GetParticipantByTokenAsync(token);
        return (p?.ParticipantId, p?.Language?.Code);
    }

}
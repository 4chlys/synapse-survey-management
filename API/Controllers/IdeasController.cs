using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.Ai;
using SynapseEnqueteManagement.API.DTOs.Ideation;
using SynapseEnqueteManagement.API.Services;
using SynapseEnqueteManagement.BL;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using Microsoft.Extensions.Logging;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/categories/{categoryId}/ideas")]
public class IdeasController(
    IIdeaManager ideaManager,
    ICommentManager commentManager,
    IParticipantManager participantManager,
    IModerationResolver moderationResolver,
    IProjectManager projectManager,
    IStorageService storageService,
    IModerationTokenService moderationTokenService,
    IPlatformAiConfigService platformAiConfig,
    ILogger<IdeasController> logger)
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
            Items = result.Items.Select(i => MapIdeaToDto(i, participantId)),
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

        var resolveProject = await GetProjectByIdReadOnlyAsync(projectId);
        var platformConfig = await platformAiConfig.ResolveAsync(resolveProject?.ProjectPlatformId ?? 0, ct);
        var status = await moderationResolver.ResolveStatusAsync(dto.Description, dto.ModerationToken, projectId, platformConfig, ct, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);

        string permanentImageUrl = null;
        if (!string.IsNullOrEmpty(dto.ImageUrl))
        {
            var imageStatus = string.IsNullOrEmpty(dto.ImageModerationToken)
                ? null
                : moderationTokenService.ValidateImageToken(dto.ImageModerationToken, projectId);

            if (imageStatus is null)
            {
                logger.LogWarning("Invalid or missing image moderation token for project {ProjectId}", projectId);
                return BadRequest(new { error = "Ongeldige afbeeldingstoken." });
            }

            permanentImageUrl = await storageService.PromoteAsync(dto.ImageUrl);

            // A Flagged image means the AI rejected it but the user chose to submit anyway.
            // Override text status so an admin must review the idea before it becomes visible.
            if (imageStatus == ModerationStatus.Flagged)
                status = ModerationStatus.Flagged;
        }

        string submissionLanguageCode = null;
        if (!string.IsNullOrEmpty(dto.LanguageCode))
        {
            var project = await GetProjectByIdReadOnlyAsync(projectId);
            if (project?.AllowCrossLanguageIdeas == true)
            {
                if (project.SupportedLanguages.Any(l => string.Equals(l.Code, dto.LanguageCode, StringComparison.OrdinalIgnoreCase)))
                    submissionLanguageCode = dto.LanguageCode;
                else
                    return BadRequest(new { error = "Invalid submission language code." });
            }
        }

        var result = await ideaManager.AddIdeaAsync(categoryId, projectId, dto.Description, status, participant,
            permanentImageUrl, submissionLanguageCode, ct);

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
        return Ok(ideas.Select(i => MapIdeaToDto(i, participant.ParticipantId)));
    }

    [HttpGet("{ideaId}")]
    public async Task<ActionResult<IdeaDto>> GetIdeaById(int categoryId, int ideaId)
    {
        var participantId = await ResolveParticipantIdAsync();

        var idea = await ideaManager.GetIdeaByIdWithCommentsReadOnlyAsync(ideaId);
        if (idea == null) return NotFound();

        return Ok(MapIdeaToDto(idea, participantId));
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
            Items = paged.Items.Select(c => MapCommentToDto(c, participantId, ideaAuthorParticipantId)),
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

    private static IdeaDto MapIdeaToDto(Idea i, int? participantId) => new()
    {
        IdeaId = i.IdeaId,
        Description = i.Description,
        ImageUrl = i.ImageUrl,
        CreatedAt = i.CreatedAt,
        CommentsCount = i.Comments.OfType<TextComment>().Count(),
        IsOwnedByCurrentUser = participantId.HasValue && i.Participant?.ParticipantId == participantId.Value,
        IsFlagged = i.Status == ModerationStatus.Flagged,
        LanguageCode = i.Language?.Code,
        Emojis = GroupEmojiReactions(i.Comments.OfType<IconComment>(), participantId)
    };

    private static TextCommentDto MapCommentToDto(TextComment c, int? participantId, int? ideaAuthorParticipantId = null) => new()
    {
        CommentId = c.CommentId,
        Text = c.Text,
        CreatedAt = c.CreatedAt,
        IsOwnedByCurrentUser = participantId.HasValue && c.Participant?.ParticipantId == participantId.Value,
        IsPostedByIdeaAuthor = ideaAuthorParticipantId.HasValue && c.Participant?.ParticipantId == ideaAuthorParticipantId.Value,
        IsFlagged = c.ModerationStatus == ModerationStatus.Flagged,
        LanguageCode = c.Language?.Code,
        Emojis = GroupEmojiReactions(c.EmojiReactions.Where(r => r.Emoji != null), participantId)
    };

    private static List<IconCommentDto> GroupEmojiReactions(IEnumerable<IconComment> reactions, int? participantId) =>
        reactions
            .GroupBy(r => r.Emoji.Symbol)
            .Select(g => new IconCommentDto
            {
                EmojiId = g.First().Emoji.EmojiId,
                Symbol = g.Key,
                Count = g.Count(),
                PostedByCurrentUser = participantId.HasValue &&
                                      g.Any(r => r.Participant?.ParticipantId == participantId.Value)
            }).ToList();
}
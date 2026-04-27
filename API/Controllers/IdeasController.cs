using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Ideation;
using SynapseEnqueteManagement.BL;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/categories/{categoryId}/ideas")]
public class IdeasController(
    IIdeaManager ideaManager,
    ICommentManager commentManager,
    IParticipantManager participantManager,
    IModerationResolver moderationResolver,
    IProjectManager projectManager)
    : ParticipantControllerBase(participantManager, projectManager)
{
    [HttpGet]
    public async Task<ActionResult<IdeaIndexResponseDto>> GetIdeasByCategory(
        int projectId, int categoryId,
        [FromQuery][Range(1, int.MaxValue)] int page = 1,
        [FromQuery] string sort = null,
        CancellationToken ct = default)
    {
        var participantId = await ResolveParticipantIdAsync();
        var project = await GetProjectByIdReadOnlyAsync(projectId);
        var pageSize = project?.IdeaPageSize ?? 5;

        var result = await ideaManager.GetIdeasIndexAsync(categoryId, participantId, pageSize, page, sort, ct);

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
    public async Task<ActionResult<CreateIdeaResultDto>> CreateIdea(int projectId, int categoryId, [FromBody] CreateIdeaDto dto, CancellationToken ct)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        var status = await moderationResolver.ResolveStatusAsync(dto.Description, dto.ModerationToken, projectId, ct);
        var result = await ideaManager.AddIdeaAsync(categoryId, projectId, dto.Description, status, participant, ct);

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

        var ideas = await ideaManager.GetParticipantIdeasByCategoryWithCommentsReadOnlyAsync(categoryId, participant.ParticipantId);
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
        int projectId, int ideaId, [FromQuery][Range(1, int.MaxValue)] int page = 1, [FromQuery] string sort = null)
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

    private static IdeaDto MapIdeaToDto(Idea i, int? participantId) => new()
    {
        IdeaId = i.IdeaId,
        Description = i.Description,
        CreatedAt = i.CreatedAt,
        CommentsCount = i.Comments.OfType<TextComment>().Count(),
        IsOwnedByCurrentUser = participantId.HasValue && i.Participant?.ParticipantId == participantId.Value,
        IsFlagged = i.Status == ModerationStatus.Flagged,
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
        Emojis = GroupEmojiReactions(c.EmojiReactions.Where(r => r.Emoji != null), participantId)
    };

    private static List<IconCommentDto> GroupEmojiReactions(IEnumerable<IconComment> reactions, int? participantId) =>
        reactions
            .GroupBy(r => r.Emoji.Symbol)
            .Select(g => new IconCommentDto
            {
                EmojiId = g.First().EmojiId,
                Symbol = g.Key,
                Count = g.Count(),
                PostedByCurrentUser = participantId.HasValue && g.Any(r => r.Participant?.ParticipantId == participantId.Value)
            }).ToList();
}

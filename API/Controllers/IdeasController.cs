using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Ideation;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.BL.Implementation;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/categories/{categoryId}/ideas")]
public class IdeasController(
    IIdeaManager ideaManager,
    ICommentManager commentManager,
    IProjectManager projectManager,
    IParticipantManager participantManager,
    IModerationResolver moderationResolver)
    : ParticipantControllerBase(participantManager)
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<IdeaDto>>> GetIdeasByCategory(
        int projectId, int categoryId, [FromQuery] int page = 1, [FromQuery] string sort = null)
    {
        var participantId = await ResolveParticipantIdAsync();

        var project = await projectManager.GetProjectByIdAsync(projectId);
        if (project == null) return NotFound();

        var paged = await ideaManager.GetIdeasWithOwnFirstAsync(categoryId, page, project.IdeaPageSize, sort, participantId);

        return Ok(new PagedResult<IdeaDto>
        {
            Items = paged.Items.Select(i => MapIdeaToDto(i, participantId)),
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount
        });
    }

    [HttpPost]
    public async Task<ActionResult<CreateIdeaResultDto>> CreateIdea(int projectId, int categoryId, [FromBody] CreateIdeaDto dto)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        try
        {
            var status = await moderationResolver.ResolveStatusAsync(dto.Description, dto.ModerationToken, projectId);
            var idea = await ideaManager.AddIdeaAsync(categoryId, projectId, dto.Description, status, participant);

            var ideaCount = await ideaManager.GetIdeaCountByParticipantAsync(participant.ParticipantId);
            var project = await projectManager.GetProjectByIdAsync(projectId);

            return Created(string.Empty, new CreateIdeaResultDto
            {
                IdeaId = idea.IdeaId,
                IsFirstIdea = ideaCount == 1,
                RequestContactInfo = project?.RequestContactInfo ?? false
            });
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpGet("{ideaId}")]
    public async Task<ActionResult<IdeaDto>> GetIdeaById(int categoryId, int ideaId)
    {
        var participantId = await ResolveParticipantIdAsync();

        var idea = await ideaManager.GetIdeaByIdAsync(ideaId);
        if (idea == null) return NotFound();

        return Ok(MapIdeaToDto(idea, participantId));
    }

    [HttpGet("{ideaId}/comments")]
    public async Task<ActionResult<PagedResult<TextCommentDto>>> GetCommentsByIdea(
        int projectId, int ideaId, [FromQuery] int page = 1, [FromQuery] string sort = null)
    {
        var participantId = await ResolveParticipantIdAsync();

        var project = await projectManager.GetProjectByIdAsync(projectId);
        if (project == null) return NotFound();

        var paged = await commentManager.GetCommentsWithOwnFirstAsync(ideaId, page, project.CommentPageSize, sort, participantId);

        return Ok(new PagedResult<TextCommentDto>
        {
            Items = paged.Items.Select(c => MapCommentToDto(c, participantId)),
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

    private static TextCommentDto MapCommentToDto(TextComment c, int? participantId) => new()
    {
        CommentId = c.CommentId,
        Text = c.Text,
        CreatedAt = c.CreatedAt,
        IsOwnedByCurrentUser = participantId.HasValue && c.Participant?.ParticipantId == participantId.Value,
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
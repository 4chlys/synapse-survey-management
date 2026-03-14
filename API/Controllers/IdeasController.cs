using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs;
using SynapseEnqueteManagement.BL;
using SynapseEnqueteManagement.BL.AiModeration;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/categories/{categoryId}/ideas")]
public class IdeasController(
    IIdeaManager manager,
    ICommentManager commentManager,
    IProjectManager projectManager,
    IParticipantManager participantManager,
    IModerationTokenService tokenService,
    IAiModerationService moderationService)
    : ParticipantControllerBase(participantManager)
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<IdeaDto>>> GetIdeasByCategory(
        int projectId, int categoryId, [FromQuery] int page = 1, [FromQuery] string sort = null)
    {
        int? participantId = null;
        if (TryGetParticipantToken(out var token))
        {
            var p = await GetParticipantByTokenAsync(token);
            participantId = p?.ParticipantId;
        }

        var project = await projectManager.GetProjectByIdAsync(projectId);
        if (project == null) return NotFound();

        var paged = await manager.GetOkIdeasByCategoryAsync(categoryId, page, project.IdeaPageSize, sort);
        var items = paged.Items.Select(i => new IdeaDto
        {
            IdeaId = i.IdeaId,
            Description = i.Description,
            CreatedAt = i.CreatedAt,
            CommentsCount = i.Comments.OfType<TextComment>().Count(),
            Emojis = i.Comments.OfType<IconComment>()
                .GroupBy((IconComment ic) => ic.Emoji.Symbol)
                .Select(g => new IdeaEmojiDto
                {
                    EmojiId = g.First().EmojiId,
                    Symbol = g.Key,
                    Count = g.Count(),
                    PostedByCurrentUser = participantId.HasValue && g.Any(ic => ic.Participant.ParticipantId == participantId.Value)
                }).ToList()
        }).ToList();

        return Ok(new PagedResult<IdeaDto>
        {
            Items = items,
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount
        });
    }

    [HttpPost]
    public async Task<ActionResult<IdeaDto>> CreateIdea(int projectId, int categoryId, [FromBody] CreateIdeaDto dto)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        try
        {
            var status = await ResolveModerationStatusAsync(dto.Description, dto.ModerationToken, projectId);
            var idea = await manager.AddIdeaAsync(categoryId, projectId, dto.Description, status, participant);
            var ideaDto = new IdeaDto
            {
                IdeaId = idea.IdeaId,
                Description = idea.Description,
                CommentsCount = 0
            };
            return Created(string.Empty, ideaDto);
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpGet("{ideaId}")]
    public async Task<ActionResult<IdeaDto>> GetIdeaById(int categoryId, int ideaId)
    {
        int? participantId = null;
        if (TryGetParticipantToken(out var token))
        {
            var p = await GetParticipantByTokenAsync(token);
            participantId = p?.ParticipantId;
        }

        var idea = await manager.GetIdeaByIdAsync(ideaId);
        if (idea == null) return NotFound();

        return Ok(new IdeaDto
        {
            IdeaId = idea.IdeaId,
            Description = idea.Description,
            CommentsCount = idea.Comments.OfType<TextComment>().Count(),
            Emojis = idea.Comments.OfType<IconComment>()
                .GroupBy((IconComment ic) => ic.Emoji.Symbol)
                .Select(g => new IdeaEmojiDto
                {
                    EmojiId = g.First().EmojiId,
                    Symbol = g.Key,
                    Count = g.Count(),
                    PostedByCurrentUser = participantId.HasValue &&
                                          g.Any(ic => ic.Participant.ParticipantId == participantId.Value)
                }).ToList()
        });
    }

    [HttpGet("{ideaId}/comments")]
    public async Task<ActionResult<PagedResult<TextCommentDto>>> GetCommentsByIdea(
        int projectId, int ideaId, [FromQuery] int page = 1, [FromQuery] string sort = null)
    {
        int? participantId = null;
        if (TryGetParticipantToken(out var token))
        {
            var p = await GetParticipantByTokenAsync(token);
            participantId = p?.ParticipantId;
        }

        var project = await projectManager.GetProjectByIdAsync(projectId);
        if (project == null) return NotFound();

        var paged = await commentManager.GetOkCommentsByIdeaAsync(ideaId, page, project.CommentPageSize, sort);
        var items = paged.Items.Select(c => new TextCommentDto
        {
            CommentId = c.CommentId,
            Text = c.Text,
            CreatedAt = c.CreatedAt,
            Emojis = c.EmojiReactions
                .Where(r => r.Emoji != null)
                .GroupBy((IconComment r) => r.Emoji.Symbol)
                .Select(g => new IconCommentDto
                {
                    EmojiId = g.First().EmojiId,
                    Symbol = g.Key,
                    Count = g.Count(),
                    PostedByCurrentUser = participantId.HasValue && g.Any(r => r.Participant.ParticipantId == participantId.Value)
                }).ToList()
        }).ToList();

        return Ok(new PagedResult<TextCommentDto>
        {
            Items = items,
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount
        });
    }

    /// <summary>
    /// Verifies the signed moderation token. If valid and the text matches,
    /// uses the encoded status (no AI call). Otherwise falls back to a full
    /// server-side moderation check.
    /// </summary>
    private async Task<ModerationStatus> ResolveModerationStatusAsync(string text, string token, int projectId)
    {
        if (!string.IsNullOrEmpty(token))
        {
            var cached = tokenService.ValidateToken(text, token, projectId);
            if (cached.HasValue) return cached.Value;
        }

        // Fallback: run moderation server-side
        var result = await moderationService.CheckWithAiAsync(text);
        return result.IsAcceptable ? ModerationStatus.Unflagged : ModerationStatus.Flagged;
    }
}
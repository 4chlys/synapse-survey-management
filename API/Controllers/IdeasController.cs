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
    IParticipantManager participantManager,
    IModerationTokenService tokenService,
    IAiModerationService moderationService)
    : ParticipantControllerBase(participantManager)
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<IdeaDto>>> GetIdeasByCategory(int categoryId)
    {
        int? participantId = null;
        if (TryGetParticipantToken(out var token))
        {
            var p = await GetParticipantByTokenAsync(token);
            participantId = p?.ParticipantId;
        }

        var ideas = await manager.GetIdeasByCategoryAsync(categoryId);
        var dtos = ideas.Select(i => new IdeaDto
        {
            IdeaId = i.IdeaId,
            Description = i.Description,
            CommentsCount = i.Comments.OfType<TextComment>().Count(),
            Emojis = i.Comments.OfType<IconComment>()
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
        return Ok(dtos);
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
    public async Task<ActionResult<IEnumerable<TextCommentDto>>> GetCommentsByIdea(int ideaId)
    {
        int? participantId = null;
        if (TryGetParticipantToken(out var token))
        {
            var p = await GetParticipantByTokenAsync(token);
            participantId = p?.ParticipantId;
        }

        var idea = await manager.GetIdeaByIdAsync(ideaId);
        if (idea == null) return NotFound();

        var comments = idea.Comments
            .OfType<TextComment>()
            .Select(c => new TextCommentDto
            {
                CommentId = c.CommentId,
                Text = c.Text,
                Emojis = c.EmojiReactions
                    .Where(r => r.Emoji != null)
                    .GroupBy((IconComment r) => r.Emoji.Symbol)
                    .Select(g => new IconCommentDto
                    {
                        EmojiId = g.First().EmojiId,
                        Symbol = g.Key,
                        Count = g.Count(),
                        PostedByCurrentUser = participantId.HasValue &&
                                              g.Any(r => r.Participant.ParticipantId == participantId.Value)
                    }).ToList()
            });

        return Ok(comments);
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
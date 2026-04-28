using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Ideation;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/categories/{categoryId}/ideas/{ideaId}")]
public class CommentsController(
    ICommentManager manager,
    IParticipantManager participantManager,
    IModerationResolver moderationResolver,
    IProjectManager projectManager)
    : ParticipantControllerBase(participantManager, projectManager)
{
    [HttpPost("comments")]
    public async Task<ActionResult> AddComment(int projectId, int ideaId, CreateCommentDto dto, CancellationToken ct)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        try
        {
            var status = await moderationResolver.ResolveStatusAsync(dto.Text, dto.ModerationToken, projectId, ct);
            var comment = await manager.AddTextCommentAsync(ideaId, projectId, dto.Text, status, participant, ct);
            return Ok(new CreatedTextCommentDto { CommentId = comment.CommentId, Text = comment.Text });
        }
        catch (KeyNotFoundException)
        {
            return NotFound("The requested resource was not found.");
        }
    }

    [HttpPost("iconcomments")]
    public async Task<ActionResult> AddIconComment(int projectId, int ideaId, AddIconCommentDto dto,
        CancellationToken ct)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;
        var iconComment = await manager.AddIconCommentAsync(ideaId, projectId, dto.EmojiId, participant, ct);
        if (iconComment == null) return Conflict("Participant already reacted with this emoji.");
        return Ok(new CreatedIconCommentDto { CommentId = iconComment.CommentId, EmojiId = iconComment.Emoji.EmojiId });
    }

    [HttpDelete("iconcomments/{emojiId}")]
    public async Task<ActionResult> RemoveIconComment(int projectId, int ideaId, int emojiId, CancellationToken ct)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        var removed = await manager.RemoveIconCommentAsync(ideaId, projectId, emojiId, participant, ct);
        if (!removed) return NotFound("Reaction not found.");
        return NoContent();
    }

    [HttpPost("comments/{commentId}/emoji")]
    public async Task<ActionResult> AddCommentEmoji(int projectId, int ideaId, int commentId,
        AddIconCommentDto dto, CancellationToken ct)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        try
        {
            var reaction = await manager.AddCommentReactionAsync(commentId, projectId, dto.EmojiId, participant, ct);
            if (reaction == null) return Conflict("Participant already reacted with this emoji.");
            return Ok(new CreatedIconCommentDto { CommentId = reaction.CommentId, EmojiId = reaction.Emoji.EmojiId });
        }
        catch (KeyNotFoundException)
        {
            return NotFound("The requested resource was not found.");
        }
    }

    [HttpDelete("comments/{commentId}/emoji/{emojiId}")]
    public async Task<ActionResult> RemoveCommentEmoji(int projectId, int ideaId, int commentId,
        int emojiId, CancellationToken ct)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        var removed = await manager.RemoveCommentReactionAsync(commentId, projectId, emojiId, participant, ct);
        if (!removed) return NotFound("Reaction not found.");
        return NoContent();
    }
}
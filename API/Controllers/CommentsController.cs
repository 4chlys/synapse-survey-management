using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Ideation;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/categories/{categoryId}/ideas/{ideaId}")]
public class CommentsController(
    ICommentManager manager,
    IParticipantManager participantManager,
    IModerationResolver moderationResolver)
    : ParticipantControllerBase(participantManager)
{
    [HttpPost("comments")]
    public async Task<ActionResult> AddComment(int projectId, int ideaId, [FromBody] CreateCommentDto dto)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        try
        {
            var status = await moderationResolver.ResolveStatusAsync(dto.Text, dto.ModerationToken, projectId);
            var comment = await manager.AddTextCommentAsync(ideaId, projectId, dto.Text, status, participant);
            return Ok(new { comment.CommentId, comment.Text });
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpPost("iconcomments")]
    public async Task<ActionResult> AddIconComment(int projectId, int ideaId, [FromBody] AddIconCommentDto dto)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        try
        {
            var iconComment = await manager.AddIconCommentAsync(ideaId, projectId, dto.EmojiId, participant);
            if (iconComment == null) return Conflict("Participant already reacted with this emoji.");
            return Ok(new { iconComment.CommentId, iconComment.EmojiId });
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpDelete("iconcomments")]
    public async Task<ActionResult> RemoveIconComment(int projectId, int ideaId, [FromBody] AddIconCommentDto dto)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        var removed = await manager.RemoveIconCommentAsync(ideaId, projectId, dto.EmojiId, participant);
        if (!removed) return NotFound("Reaction not found.");
        return NoContent();
    }

    [HttpPost("comments/{commentId}/emoji")]
    public async Task<ActionResult> AddCommentEmoji(int projectId, int ideaId, int commentId,
        [FromBody] AddIconCommentDto dto)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        try
        {
            var reaction = await manager.AddCommentReactionAsync(commentId, projectId, dto.EmojiId, participant);
            if (reaction == null) return Conflict("Participant already reacted with this emoji.");
            return Ok(new { reaction.CommentId, reaction.EmojiId });
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpDelete("comments/{commentId}/emoji")]
    public async Task<ActionResult> RemoveCommentEmoji(int projectId, int ideaId, int commentId,
        [FromBody] AddIconCommentDto dto)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        var removed = await manager.RemoveCommentReactionAsync(commentId, projectId, dto.EmojiId, participant);
        if (!removed) return NotFound("Reaction not found.");
        return NoContent();
    }

}
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs;
using SynapseEnqueteManagement.BL;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/categories/{categoryId}/ideas/{ideaId}")]
public class CommentsController(ICommentManager manager, IParticipantManager participantManager)
    : ParticipantControllerBase(participantManager)
{
    [HttpPost("comments")]
    public async Task<ActionResult> AddComment(int ideaId, [FromBody] CreateCommentDto dto)
    {
        var (participant, error) = await GetParticipantAsync();
        if (error is not null) return error;

        try
        {
            var comment = await manager.AddTextCommentAsync(ideaId, dto.Text, dto.ParsedModerationStatus, participant);
            return Ok(new { comment.CommentId, comment.Text });
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpPost("iconcomments")]
    public async Task<ActionResult> AddIconComment(int ideaId, [FromBody] AddIconCommentDto dto)
    {
        var (participant, error) = await GetParticipantAsync();
        if (error is not null) return error;

        try
        {
            var iconComment = await manager.AddIconCommentAsync(ideaId, dto.EmojiId, participant);
            if (iconComment == null) return Conflict("Participant already reacted with this emoji.");
            return Ok(new { iconComment.CommentId, iconComment.EmojiId });
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpDelete("iconcomments")]
    public async Task<ActionResult> RemoveIconComment(int ideaId, [FromBody] AddIconCommentDto dto)
    {
        var (participant, error) = await GetParticipantAsync();
        if (error is not null) return error;

        var removed = await manager.RemoveIconCommentAsync(ideaId, dto.EmojiId, participant);
        if (!removed) return NotFound("Reaction not found.");
        return NoContent();
    }

    [HttpPost("comments/{commentId}/emoji")]
    public async Task<ActionResult> AddCommentEmoji(int ideaId, int commentId, [FromBody] AddIconCommentDto dto)
    {
        var (participant, error) = await GetParticipantAsync();
        if (error is not null) return error;

        try
        {
            var reaction = await manager.AddCommentReactionAsync(commentId, dto.EmojiId, participant);
            if (reaction == null) return Conflict("Participant already reacted with this emoji.");
            return Ok(new { reaction.CommentId, reaction.EmojiId });
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpDelete("comments/{commentId}/emoji")]
    public async Task<ActionResult> RemoveCommentEmoji(int ideaId, int commentId, [FromBody] AddIconCommentDto dto)
    {
        var (participant, error) = await GetParticipantAsync();
        if (error is not null) return error;

        var removed = await manager.RemoveCommentReactionAsync(commentId, dto.EmojiId, participant);
        if (!removed) return NotFound("Reaction not found.");
        return NoContent();
    }
}

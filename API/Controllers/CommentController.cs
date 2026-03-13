using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs;
using SynapseEnqueteManagement.BL;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/categories/{categoryId}/ideas/{ideaId}/comments")]
public class CommentController(ICommentManager manager) : ControllerBase
{
    [HttpPost("{ideaId}/comments")]
    public async Task<ActionResult> AddComment(int ideaId, [FromBody] CreateCommentDto dto)
    {
        try
        {
            var comment = await manager.AddTextCommentAsync(ideaId, dto.Text, TODO);
            return Ok(new { comment.CommentId, comment.Text });
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
    
    [HttpPost("{ideaId}/iconcomments")]
    public async Task<ActionResult> AddIconComment(int ideaId, [FromBody] AddIconCommentDto dto)
    {
        try
        {
            var iconComment = await manager.AddIconCommentAsync(ideaId, dto.EmojiId);
            return Ok(new { iconComment.CommentId, iconComment.EmojiId });
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
    
    [HttpPost("{ideaId}/comments/{commentId}/emoji")]
    public async Task<ActionResult> AddCommentEmoji(int ideaId, int commentId, [FromBody] AddIconCommentDto dto)
    {
        try
        {
            var reaction = await manager.AddCommentReactionAsync(commentId, dto.EmojiId);
            return Ok(new { reaction.CommentId, reaction.EmojiId });
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}
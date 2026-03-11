using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs;
using SynapseEnqueteManagement.BL;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/categories/{categoryId}/ideas")]
public class IdeasController : ControllerBase
{
    private readonly IManager _manager;

    public IdeasController(IManager manager)
    {
        _manager = manager;
    }

    [HttpGet]
    public ActionResult<IEnumerable<IdeaDto>> GetIdeasByCategory(int categoryId)
    {
        var ideas = _manager.GetIdeasByCategory(categoryId);
        var dtos = ideas.Select(i => new IdeaDto
        {
            IdeaId = i.IdeaId,
            Description = i.Description,
            EmojiCount = i.EmojiReactions.Count,
            CommentsCount = i.Comments.Count
        });
        return Ok(dtos);
    }

    [HttpPost]
    public ActionResult<IdeaDto> CreateIdea(int categoryId, [FromBody] CreateIdeaDto dto)
    {
        var idea = _manager.AddIdea(categoryId, dto.Description);
        
        var ideaDto = new IdeaDto
        {
            IdeaId = idea.IdeaId,
            Description = idea.Description,
            EmojiCount = 0,
            CommentsCount = 0
        };
        
        return CreatedAtAction(nameof(GetIdeasByCategory), new { categoryId }, ideaDto);
    }
    
    [HttpPost("{ideaId}/comments")]
    public async Task<ActionResult> AddComment(int ideaId, [FromBody] CreateCommentDto dto)
    {
        var comment = await _manager.AddCommentAsync(ideaId, dto.Text);
        return Ok(new { comment.CommentId, comment.Text });
    }
    
    [HttpGet("{ideaId}")]
    public async Task<ActionResult<IdeaDto>> GetIdeaById(int categoryId, int ideaId)
    {
        var idea = await _manager.GetIdeaByIdAsync(ideaId);
        if (idea == null) return NotFound();

        return Ok(new IdeaDto
        {
            IdeaId = idea.IdeaId,
            Description = idea.Description,
            EmojiCount = idea.EmojiReactions.Count,
            CommentsCount = idea.Comments.Count
        });
    }
    
    [HttpGet("{ideaId}/comments")]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetCommentsByIdea(int ideaId)
    {
        var idea = await _manager.GetIdeaByIdAsync(ideaId);
        if (idea == null) return NotFound();

        var comments = idea.Comments.OfType<TextComment>().Select(c => new CommentDto
        {
            CommentId = c.CommentId,
            Text = c.Text
        });

        return Ok(comments);
    }
}
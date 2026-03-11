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
            CommentsCount = i.Comments.OfType<TextComment>().Count(),
            Emojis = i.Comments.OfType<IconComment>()
                .GroupBy(ic => ic.Emoji.Symbol)
                .Select(g => new IdeaEmojiDto
                {
                    EmojiId = g.First().EmojiId,
                    Symbol = g.Key,
                    Count = g.Count()
                }).ToList()
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
            CommentsCount = idea.Comments.OfType<TextComment>().Count(),
            Emojis = idea.Comments.OfType<IconComment>()
                .GroupBy(ic => ic.Emoji.Symbol)
                .Select(g => new IdeaEmojiDto
                {
                    EmojiId = g.First().EmojiId,
                    Symbol = g.Key,
                    Count = g.Count()
                }).ToList()
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
            Text = c.Text,
            Emojis = c.EmojiReactions
                .GroupBy(e => e.Emoji.Symbol)
                .Select(g => new CommentEmojiDto
                {
                    EmojiId = g.First().EmojiId,
                    Symbol = g.Key,
                    Count = g.Count()
                }).ToList()
        });

        return Ok(comments);
    }
    
    [HttpPost("{ideaId}/iconcomments")]
    public async Task<ActionResult> AddIconComment(int ideaId, [FromBody] AddIconCommentDto dto)
    {
        var iconComment = await _manager.AddIconCommentAsync(ideaId, dto.EmojiId);
        return Ok(new { iconComment.CommentId, iconComment.EmojiId });
    }
    
    [HttpPost("{ideaId}/comments/{commentId}/emoji")]
    public async Task<ActionResult> AddCommentEmoji(int ideaId, int commentId, [FromBody] AddIconCommentDto dto)
    {
        var commentEmoji = await _manager.AddCommentEmojiAsync(commentId, dto.EmojiId);
        return Ok(new { commentEmoji.CommentEmojiId, commentEmoji.EmojiId });
    }
}
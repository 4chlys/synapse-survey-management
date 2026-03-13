using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs;
using SynapseEnqueteManagement.BL;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/categories/{categoryId}/ideas")]
public class IdeasController(IIdeaManager manager, IParticipantManager participantManager)
    : ParticipantControllerBase(participantManager)
{

    [HttpGet]
    public async Task<ActionResult<IEnumerable<IdeaDto>>> GetIdeasByCategory(int categoryId)
    {
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
                    Count = g.Count()
                }).ToList()
        });
        return Ok(dtos);
    }

    [HttpPost]
    public async Task<ActionResult<IdeaDto>> CreateIdea(int categoryId, [FromBody] CreateIdeaDto dto)
    {
        var (participant, error) = await GetParticipantAsync();
        if (error is not null) return error;

        try
        {
            var status = dto.ParsedModerationStatus;
            var idea = await manager.AddIdeaAsync(categoryId, dto.Description, status, participant);
            var ideaDto = new IdeaDto
            {
                IdeaId = idea.IdeaId,
                Description = idea.Description,
                CommentsCount = 0
            };
            return CreatedAtAction(nameof(GetIdeasByCategory), new { categoryId }, ideaDto);
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpGet("{ideaId}")]
    public async Task<ActionResult<IdeaDto>> GetIdeaById(int categoryId, int ideaId)
    {
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
                    Count = g.Count()
                }).ToList()
        });
    }

    [HttpGet("{ideaId}/comments")]
    public async Task<ActionResult<IEnumerable<TextCommentDto>>> GetCommentsByIdea(int ideaId)
    {
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
                        Count = g.Count()
                    }).ToList()
            });

        return Ok(comments);
    }
}

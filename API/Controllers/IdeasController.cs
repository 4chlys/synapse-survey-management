using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs;
using SynapseEnqueteManagement.BL;

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
            EmojiCount = i.EmojiCount,
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
}
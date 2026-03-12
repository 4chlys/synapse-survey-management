using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.BL;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/emojis")]
public class EmojisController : ControllerBase
{
    private readonly IIdeaManager _manager;

    public EmojisController(IIdeaManager manager)
    {
        _manager = manager;
    }

    [HttpGet]
    public async Task<ActionResult> GetEmojis()
    {
        var emojis = await _manager.GetEmojisAsync();
        var result = emojis.Select(e => new
        {
            e.EmojiId,
            e.Symbol,
            e.Name
        });
        return Ok(result);
    }
}
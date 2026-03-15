using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/emojis")]
public class EmojisController(ICommentManager manager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> GetEmojis()
    {
        var emojis = await manager.GetEmojisAsync();
        var result = emojis.Select(e => new
        {
            e.EmojiId,
            e.Symbol,
            e.Name
        });
        return Ok(result);
    }
}
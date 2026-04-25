using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
public class EmojisController(IProjectManager projectManager) : ControllerBase
{
    [HttpGet("api/projects/{projectId}/emojis")]
    public async Task<ActionResult> GetProjectEmojis(int projectId)
    {
        var emojis = await projectManager.GetEmojisForProjectAsync(projectId);
        var result = emojis.Select(e => new
        {
            e.EmojiId,
            e.Symbol,
            e.Name
        });
        return Ok(result);
    }

    [HttpGet("api/emojis")]
    public async Task<ActionResult> GetAllEmojis()
    {
        var emojis = await projectManager.GetAllEmojisAsync();
        var result = emojis.Select(e => new
        {
            e.EmojiId,
            e.Symbol,
            e.Name
        });
        return Ok(result);
    }

    [HttpPost("api/projects/{projectId}/emojis/{emojiId}")]
    public async Task<ActionResult> EnableEmoji(int projectId, int emojiId, CancellationToken ct)
    {
        var success = await projectManager.EnableEmojiForProjectAsync(projectId, emojiId, ct);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpDelete("api/projects/{projectId}/emojis/{emojiId}")]
    public async Task<ActionResult> DisableEmoji(int projectId, int emojiId, CancellationToken ct)
    {
        var success = await projectManager.DisableEmojiForProjectAsync(projectId, emojiId, ct);
        if (!success) return NotFound();
        return NoContent();
    }
}

using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.User.Services;

namespace SynapseEnqueteManagement.UI.User.Controllers.Api;

[ApiController]
[Route("{projectId:int}/api/[controller]")]
public class ModerationApiController(ModerationService moderationService) : ControllerBase
{
    [HttpPost("regex")]
    public async Task<ActionResult<ModerationResponse>> Regex(int projectId, [FromBody] ModerationRequest request)
    {
        var result = await moderationService.CheckRegexAsync(projectId, request.Text);
        return Ok(new
        {
            result.IsAcceptable,
            result.Reason,
            result.FlaggedBy,
            result.ModerationToken,
        });
    }

    [HttpPost("ai")]
    [EnableRateLimiting("ai-moderation")]
    public async Task<ActionResult<ModerationResponse>> Ai(int projectId, [FromBody] ModerationRequest request)
    {
        var result = await moderationService.CheckAiAsync(projectId, request.Text);
        return Ok(new
        {
            result.IsAcceptable,
            result.Reason,
            result.FlaggedBy,
            result.ModerationToken,
        });
    }

    [HttpPost("suggest")]
    [EnableRateLimiting("ai-moderation")]
    public async Task<ActionResult<SuggestResponse>> Suggest(int projectId, [FromBody] ModerationRequest request)
    {
        var result = await moderationService.SuggestAsync(projectId, request.Text);
        return Ok(new
        {
            result.Suggestion,
            result.ModerationToken,
        });
    }
}

public class ModerationRequest
{
    public string Text { get; set; }
}
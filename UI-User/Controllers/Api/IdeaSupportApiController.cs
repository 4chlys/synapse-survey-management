using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.User.Services;
using SynapseEnqueteManagement.UI.User.Services.Ideation;

namespace SynapseEnqueteManagement.UI.User.Controllers.Api;

/// <summary>
/// Thin browser-facing API controller that proxies idea-support calls from
/// the TypeScript front-end to the backend API.
///
/// The browser cannot call the backend API directly (API key, CORS).
/// This controller acts as a same-origin relay, exactly as
/// <see cref="ModerationApiController"/> does for moderation.
///
/// Routes: /{projectId}/api/IdeaSupportApi/{chat|refine}
/// </summary>
[ApiController]
[Route("{projectId:int}/api/[controller]")]
public class IdeaSupportApiController(IdeaSupportService ideaSupportService) : ControllerBase
{
    [HttpPost("chat")]
    public async Task<ActionResult<IdeaSupportChatResponseBody>> Chat(
        int projectId,
        [FromBody] IdeaSupportChatRequestBody body)
    {
        var result = await ideaSupportService.ChatAsync(
            projectId,
            body.CategoryId,
            body.History,
            body.UserMessage);

        return Ok(new IdeaSupportChatResponseBody
        {
            AssistantMessage = result.AssistantMessage,
            SuggestedDraft   = result.SuggestedDraft,
        });
    }

    [HttpPost("refine")]
    public async Task<ActionResult<IdeaSupportRefineResponseBody>> Refine(
        int projectId,
        [FromBody] IdeaSupportRefineRequestBody body)
    {
        var result = await ideaSupportService.RefineAsync(projectId, body.DraftText);

        return Ok(new IdeaSupportRefineResponseBody { RefinedText = result.RefinedText });
    }
}

// Request / response shapes (same-origin only, no need to share DTOs)

public sealed class IdeaSupportChatRequestBody
{
    public IReadOnlyList<IdeaSupportMessage> History { get; set; } = [];
    public string UserMessage { get; set; } = string.Empty;
    public int    CategoryId  { get; set; }
}

public sealed class IdeaSupportChatResponseBody
{
    public string  AssistantMessage { get; set; } = string.Empty;
    public string SuggestedDraft   { get; set; }
}

public sealed class IdeaSupportRefineRequestBody
{
    public string DraftText { get; set; } = string.Empty;
}

public sealed class IdeaSupportRefineResponseBody
{
    public string RefinedText { get; set; }
}
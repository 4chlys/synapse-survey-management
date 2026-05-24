using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using SynapseEnqueteManagement.UI.User.Filters;
using SynapseEnqueteManagement.UI.User.Models.Ideation;
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
[RequireParticipantSession]
public class IdeaSupportApiController(
    IdeaSupportService ideaSupportService,
    IStringLocalizer<SharedResource> localizer,
    ILogger<IdeaSupportApiController> logger) : ControllerBase
{
    [HttpPost("chat")]
    public async Task<ActionResult<IdeaSupportChatResponse>> Chat(
        int projectId,
        IdeaSupportChatRequest body,
        CancellationToken ct)
    {
        IdeaSupportChatResponse result;
        try
        {
            result = await ideaSupportService.ChatAsync(
                projectId,
                body.CategoryId,
                body.History,
                body.UserMessage,
                ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI IdeaSupport call failed for project {ProjectId}", projectId);
            return Ok(new IdeaSupportChatResponse
            {
                AssistantMessage = localizer["AiSupport_IdeaFallback"],
                Phase = "Error",
            });
        }

        return Ok(result);
    }

    [HttpPost("refine")]
    public async Task<ActionResult<IdeaSupportRefineResponse>> Refine(
        int projectId,
        IdeaSupportRefineRequest body)
    {
        var result = await ideaSupportService.RefineAsync(projectId, body.DraftText);
        return Ok(result);
    }
}
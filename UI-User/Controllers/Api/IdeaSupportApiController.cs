using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using SynapseEnqueteManagement.UI.User.Models.Ideation;
using SynapseEnqueteManagement.UI.User.Services.Ideation;
using SynapseEnqueteManagement.UI.User.Services.Session;

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
public class IdeaSupportApiController(
    IdeaSupportService ideaSupportService,
    ISessionService sessionService,
    IStringLocalizer<SharedResource> localizer) : ControllerBase
{
    [HttpPost("chat")]
    public async Task<ActionResult<IdeaSupportChatResponse>> Chat(
        int projectId,
        IdeaSupportChatRequest body)
    {
        // Gate behind a valid participant session (survey must have been completed)
        var token = sessionService.GetToken(projectId);
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized("Participant session required.");

        IdeaSupportChatResponse result;
        try
        {
            result = await ideaSupportService.ChatAsync(
                projectId,
                body.CategoryId,
                body.History,
                body.UserMessage);
        }
        catch (Exception)
        {
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
        var token = sessionService.GetToken(projectId);
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized("Participant session required.");

        var result = await ideaSupportService.RefineAsync(projectId, body.DraftText);
        return Ok(result);
    }
}
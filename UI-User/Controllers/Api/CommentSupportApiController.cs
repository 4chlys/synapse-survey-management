using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using SynapseEnqueteManagement.UI.User.Models.Ideation;
using SynapseEnqueteManagement.UI.User.Services.Ideation;
using SynapseEnqueteManagement.UI.User.Services.Session;

namespace SynapseEnqueteManagement.UI.User.Controllers.Api;

/// <summary>
/// Browser-facing proxy for comment-support AI calls.
/// Routes: /{projectId}/api/CommentSupportApi/chat
/// </summary>
[ApiController]
[Route("{projectId:int}/api/[controller]")]
public class CommentSupportApiController(
    CommentSupportService commentSupportService,
    ISessionService sessionService,
    IStringLocalizer<SharedResource> localizer,
    ILogger<CommentSupportApiController> logger) : ControllerBase
{
    [HttpPost("chat")]
    public async Task<ActionResult<CommentSupportChatResponse>> Chat(
        int projectId,
        CommentSupportChatRequest body,
        CancellationToken ct)
    {
        var token = sessionService.GetToken(projectId);
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized("Participant session required.");

        CommentSupportChatResponse result;
        try
        {
            result = await commentSupportService.ChatAsync(
                projectId,
                body.History,
                body.UserMessage,
                body.IdeaText,
                body.CategoryName,
                ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI CommentSupport call failed for project {ProjectId}", projectId);
            return Ok(new CommentSupportChatResponse
            {
                AssistantMessage = localizer["AiSupport_CommentFallback"],
                IsFinal          = true,
            });
        }

        return Ok(result);
    }
}

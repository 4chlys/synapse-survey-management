using Microsoft.AspNetCore.Mvc;
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
    ISessionService sessionService) : ControllerBase
{
    [HttpPost("chat")]
    public async Task<ActionResult<CommentSupportChatResponse>> Chat(
        int projectId,
        [FromBody] CommentSupportChatRequest body)
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
                body.CategoryName);
        }
        catch (Exception)
        {
            return Ok(new CommentSupportChatResponse
            {
                AssistantMessage = "Sorry, de assistent is even niet beschikbaar. Je kunt je reactie direct hieronder intypen.",
                IsFinal          = true,
            });
        }

        return Ok(result);
    }
}

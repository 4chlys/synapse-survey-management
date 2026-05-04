using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.Admin.Models.QuestionSupport;
using SynapseEnqueteManagement.UI.Admin.Services.QuestionSupport;

namespace SynapseEnqueteManagement.UI.Admin.Controllers.Api;

[ApiController]
[Route("api/admin/projects/{projectId:int}/questionsupport")]
public class QuestionSupportApiController(QuestionSupportService questionSupportService) : ControllerBase
{
    [HttpPost("chat")]
    public async Task<ActionResult<QuestionSupportChatResponse>> Chat(
        int projectId,
        QuestionSupportChatRequest body)
    {
        try
        {
            var result = await questionSupportService.ChatAsync(
                projectId,
                body.History,
                body.UserMessage,
                body.IsFirstTurn);
            return Ok(result);
        }
        catch
        {
            return Ok(new QuestionSupportChatResponse
            {
                AssistantMessage = "Sorry, de AI-assistent is even niet beschikbaar.",
            });
        }
    }

    [HttpPatch("mode")]
    public async Task<IActionResult> UpdateMode(int projectId, [FromBody] string mode)
    {
        try
        {
            await questionSupportService.UpdateModeAsync(projectId, mode);
            return NoContent();
        }
        catch
        {
            return StatusCode(502);
        }
    }
}

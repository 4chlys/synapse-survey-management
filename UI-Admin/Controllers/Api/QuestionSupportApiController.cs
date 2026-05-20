using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using SynapseEnqueteManagement.UI.Admin.Models.QuestionSupport;
using SynapseEnqueteManagement.UI.Admin.Services.QuestionSupport;

namespace SynapseEnqueteManagement.UI.Admin.Controllers.Api;

[ApiController]
[Route("api/admin/projects/{projectId:int}/questionsupport")]
public class QuestionSupportApiController(
    QuestionSupportService questionSupportService,
    IStringLocalizer<SharedResource> localizer) : ControllerBase
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
                AssistantMessage = localizer["AiSupport_QuestionFallback"],
            });
        }
    }

    [HttpPatch("mode")]
    public async Task<IActionResult> UpdateMode(int projectId, string mode)
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

using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.QuestionSupport;
using SynapseEnqueteManagement.API.Helpers;
using SynapseEnqueteManagement.BL.AiQuestionSupport;
using SynapseEnqueteManagement.BL.AiSupport;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/questionsupport")]
public class QuestionSupportController(
    IAiQuestionSupportService questionSupportService,
    IProjectManager projectManager,
    IQuestionnaireManager questionnaireManager)
    : ControllerBase
{
    [HttpPost("chat")]
    public async Task<ActionResult<QuestionSupportChatResponseDto>> Chat(
        int projectId,
        [FromBody] QuestionSupportChatRequestDto dto,
        CancellationToken ct)
    {
        var project = await projectManager.GetProjectByIdReadOnlyAsync(projectId);
        if (project is null) return NotFound($"Project {projectId} not found.");

        IEnumerable<BL.Domain.Project.Entities.Question> questions = [];
        try { questions = await questionnaireManager.GetQuestionsForProjectAsync(projectId); }
        catch { /* non-fatal — continue without question context */ }

        var request = new QuestionSupportChatRequest
        {
            ProjectName        = project.ProjectPlatform?.Name ?? string.Empty,
            ProjectDescription = project.Introduction?.Text    ?? string.Empty,
            ExistingQuestions  = questions.Select(q => q.Text).ToList(),
            History = dto.History
                .Select(h => new AiSupportMessage(h.Role, h.Content))
                .ToList(),
            UserMessage = dto.UserMessage ?? string.Empty,
            IsFirstTurn = dto.IsFirstTurn,
        };

        var reply = await questionSupportService.ChatAsync(request, ct);

        return Ok(new QuestionSupportChatResponseDto { AssistantMessage = reply.AssistantMessage });
    }

    [HttpPatch("mode")]
    public async Task<IActionResult> UpdateMode(int projectId, [FromBody] AiAssistanceType mode)
    {
        await projectManager.UpdateAiAssistanceModeAsync(projectId, mode);
        return NoContent();
    }
}

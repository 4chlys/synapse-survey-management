using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.Ai;
using SynapseEnqueteManagement.API.DTOs.QuestionSupport;
using SynapseEnqueteManagement.API.Helpers;
using SynapseEnqueteManagement.BL.AiQuestionSupport;
using SynapseEnqueteManagement.BL.AiSupport;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/questionsupport")]
public class QuestionSupportController(
    IAiQuestionSupportService questionSupportService,
    IPlatformAiConfigService platformAiConfig,
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

        var lang        = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var defaultLang = project.DefaultLanguage?.Code;

        var projectDescription =
            (project.Introduction?.Translations.FirstOrDefault(t => t.Language?.Code == lang)
             ?? project.Introduction?.Translations.FirstOrDefault(t => t.Language?.Code == defaultLang)
             ?? project.Introduction?.Translations.FirstOrDefault())?.Text
            ?? string.Empty;

        var existingQuestions = questions
            .Select(q =>
                (q.Translations.FirstOrDefault(t => t.Language?.Code == lang)
                 ?? q.Translations.FirstOrDefault(t => t.Language?.Code == defaultLang)
                 ?? q.Translations.FirstOrDefault())?.Text)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        var request = new QuestionSupportChatRequest
        {
            ProjectName        = project.ProjectPlatform?.Name ?? string.Empty,
            ProjectDescription = projectDescription,
            ExistingQuestions  = existingQuestions,
            History = dto.History
                .Select(h => new AiSupportMessage(h.Role, h.Content))
                .ToList(),
            UserMessage      = dto.UserMessage ?? string.Empty,
            IsFirstTurn      = dto.IsFirstTurn,
            ChatLanguage     = lang,
            // Modal passes the active tab's language; fall back to project default
            // so chat outside the multi-language modal still gets a sensible target.
            QuestionLanguage = !string.IsNullOrWhiteSpace(dto.QuestionLanguage)
                ? dto.QuestionLanguage
                : defaultLang ?? lang,
        };

        var platformConfig = await platformAiConfig.ResolveAsync(project.ProjectPlatformId, ct);
        var reply = await questionSupportService.ChatAsync(request, platformConfig, projectId, ct);

        return Ok(new QuestionSupportChatResponseDto { AssistantMessage = reply.AssistantMessage });
    }
}

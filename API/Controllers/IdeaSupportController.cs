using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.Ai;
using SynapseEnqueteManagement.API.DTOs.IdeaSupport;
using SynapseEnqueteManagement.API.Helpers;
using SynapseEnqueteManagement.BL.AiIdeaSupport;
using SynapseEnqueteManagement.BL.AiSupport;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.InputValidation;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/ideasupport")]
public class IdeaSupportController(
    IAiIdeaSupportService ideaSupportService,
    IPlatformAiConfigService platformAiConfig,
    IInputValidationService inputValidation,
    ICategoryManager categoryManager,
    IProjectManager projectManager,
    IParticipantManager participantManager)
    : ParticipantControllerBase(participantManager, projectManager)
{

    [HttpPost("chat")]
    public async Task<ActionResult<IdeaSupportChatResponseDto>> Chat(
        int projectId,
        IdeaSupportChatRequestDto dto,
        CancellationToken ct)
    {
        var (_, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        var category = await categoryManager.GetCategoryByIdReadOnlyAsync(dto.CategoryId);
        if (category is null)
            return NotFound($"Category {dto.CategoryId} not found.");

        var isFirstTurn = string.IsNullOrWhiteSpace(dto.UserMessage)
                          && dto.History.Count == 0;

        // Reject obvious violations deterministically — no AI token cost incurred.
        // Validation runs on every non-empty message, including first-turn messages
        // sent directly (e.g. bypassing the opening turn via a direct API call).
        if (!string.IsNullOrWhiteSpace(dto.UserMessage))
        {
            var validation = inputValidation.Validate(dto.UserMessage);
            if (!validation.IsValid)
                return BadRequest(new IdeaSupportChatResponseDto
                {
                    AssistantMessage = validation.Reason,
                    Phase = "Error",
                });
        }

        // Fetch project for introduction context (first turn) and age range (every turn).
        // Non-fatal: a DB error here must not block the response; continue without context.
        Project project = null;
        try { project = await GetProjectByIdReadOnlyAsync(projectId); }
        catch { /* intentionally swallowed */ }

        var platformConfig = await platformAiConfig.ResolveAsync(project?.ProjectPlatformId ?? 0, ct);

        var lang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        var projectName         = isFirstTurn ? (project?.ProjectPlatform?.Name ?? string.Empty) : string.Empty;
        var projectIntroduction = isFirstTurn
            ? (project?.Introduction?.Translations
                   .FirstOrDefault(t => t.Language?.Code == lang)?.Text
               ?? project?.Introduction?.Translations
                   .FirstOrDefault(t => t.Language?.Code == project?.DefaultLanguage?.Code)?.Text
               ?? project?.Introduction?.Translations.FirstOrDefault()?.Text
               ?? string.Empty)
            : string.Empty;
        if (projectIntroduction.Length > platformConfig.IdeaSupport.MaxIntroductionLength)
            projectIntroduction = projectIntroduction[..platformConfig.IdeaSupport.MaxIntroductionLength] + "…";

        var catTranslation = category.Translations?.FirstOrDefault(t => t.Language?.Code == lang)
            ?? category.Translations?.FirstOrDefault();

        var request = new IdeaSupportChatRequest
        {
            History = dto.History
                .Select(h => new AiSupportMessage(h.Role, h.Content))
                .ToList(),
            UserMessage         = dto.UserMessage ?? string.Empty,
            ProjectName         = projectName,
            ProjectIntroduction = projectIntroduction,
            CategoryName        = catTranslation?.Name ?? string.Empty,
            CategoryDescription = catTranslation?.Description ?? string.Empty,
            AgeRange            = AiSupportHelpers.FormatAgeRange(project?.MinAge, project?.MaxAge),
            Language            = lang,
            IsFirstTurn         = isFirstTurn,
        };

        var reply = await ideaSupportService.ChatAsync(request, platformConfig, projectId, ct);

        return Ok(new IdeaSupportChatResponseDto
        {
            AssistantMessage = reply.AssistantMessage,
            SuggestedDraft = reply.SuggestedDraft,
            Phase = reply.Phase.ToString(),
        });
    }

    [HttpPost("refine")]
    public async Task<ActionResult<IdeaSupportRefineResponseDto>> Refine(
        int projectId,
        IdeaSupportRefineRequestDto dto,
        CancellationToken ct)
    {
        var (_, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        var lang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var platformConfig = await platformAiConfig.ResolveGlobalAsync(ct);
        var result = await ideaSupportService.RefineAsync(dto.DraftText, lang, platformConfig, projectId, ct);
        return Ok(new IdeaSupportRefineResponseDto { RefinedText = result.RefinedText });
    }
}
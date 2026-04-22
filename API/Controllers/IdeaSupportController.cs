using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.IdeaSupport;
using SynapseEnqueteManagement.BL.AiIdeaSupport;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.InputValidation;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/ideasupport")]
public class IdeaSupportController(
    IAiIdeaSupportService ideaSupportService,
    AiIdeaSupportConfig ideaSupportConfig,
    IInputValidationService inputValidation,
    ICategoryManager categoryManager,
    IProjectManager projectManager,
    IParticipantManager participantManager)
    : ParticipantControllerBase(participantManager)
{

    [HttpPost("chat")]
    public async Task<ActionResult<IdeaSupportChatResponseDto>> Chat(
        int projectId,
        [FromBody] IdeaSupportChatRequestDto dto,
        CancellationToken ct)
    {
        var (_, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        var category = await categoryManager.GetCategoryByIdAsync(dto.CategoryId);
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
                    Phase            = "Error",
                });
        }

        // Fetch project to give the AI introduction context on the opening turn.
        // Non-fatal: a DB error here must not block the greeting; continue without context.
        Project project = null;
        if (isFirstTurn)
        {
            try { project = await projectManager.GetProjectByIdAsync(projectId); }
            catch { /* intentionally swallowed — greeting still works without project text */ }
        }

        var projectName         = project?.OrganisationName ?? string.Empty;
        var projectIntroduction = project?.Introduction?.Text ?? string.Empty;
        if (projectIntroduction.Length > ideaSupportConfig.MaxIntroductionLength)
            projectIntroduction = projectIntroduction[..ideaSupportConfig.MaxIntroductionLength] + "…";

        var request = new IdeaSupportChatRequest
        {
            History             = dto.History
                                    .Select(h => new IdeaSupportChatMessage
                                    {
                                        Role    = h.Role,
                                        Content = h.Content,
                                    })
                                    .ToList(),
            UserMessage         = dto.UserMessage ?? string.Empty,
            ProjectName         = projectName,
            ProjectIntroduction = projectIntroduction,
            CategoryName        = category.Name,
            CategoryDescription = category.Description ?? string.Empty,
            IsFirstTurn         = isFirstTurn,
        };

        var reply = await ideaSupportService.ChatAsync(request, ct);

        return Ok(new IdeaSupportChatResponseDto
        {
            AssistantMessage = reply.AssistantMessage,
            SuggestedDraft   = reply.SuggestedDraft,
            Phase            = reply.Phase.ToString(),
        });
    }

    [HttpPost("refine")]
    public async Task<ActionResult<IdeaSupportRefineResponseDto>> Refine(
        int projectId,
        [FromBody] IdeaSupportRefineRequestDto dto,
        CancellationToken ct)
    {
        var (_, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        var result = await ideaSupportService.RefineAsync(dto.DraftText, ct);
        return Ok(new IdeaSupportRefineResponseDto { RefinedText = result.RefinedText });
    }
}

using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs;
using SynapseEnqueteManagement.API.DTOs.IdeaSupport;
using SynapseEnqueteManagement.BL;
using SynapseEnqueteManagement.BL.AiIdeaSupport;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

/// <summary>
/// Conversational AI endpoints that help a participant articulate and refine
/// an idea before submitting it.
///
/// All endpoints require a valid participant token so the feature is gated
/// behind survey completion (same contract as ideas/comments).
///
/// Endpoints
/// ─────────
///   POST {projectId}/ideasupport/chat
///       Full stateless chat turn. The client maintains and forwards the
///       complete conversation history on every call.
///
///   POST {projectId}/ideasupport/refine
///       One-shot polish of a draft idea text without any chat context.
/// </summary>
[ApiController]
[Route("api/projects/{projectId}/ideasupport")]
public class IdeaSupportController(
    IAiIdeaSupportService ideaSupportService,
    ICategoryManager categoryManager,
    IParticipantManager participantManager)
    : ParticipantControllerBase(participantManager)
{
    // Chat

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

        var request = new IdeaSupportChatRequest
        {
            History             = dto.History.Select(h => new IdeaSupportChatMessage { Role = h.Role, Content = h.Content }).ToList(),
            UserMessage         = dto.UserMessage,
            CategoryName        = category.Name,
            CategoryDescription = category.Description ?? string.Empty,
            IsFirstTurn         = dto.History.Count == 0,
        };

        var reply = await ideaSupportService.ChatAsync(request, ct);

        return Ok(new IdeaSupportChatResponseDto
        {
            AssistantMessage = reply.AssistantMessage,
            SuggestedDraft   = reply.SuggestedDraft,
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
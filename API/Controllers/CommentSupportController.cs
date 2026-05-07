using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.CommentSupport;
using SynapseEnqueteManagement.API.Helpers;
using SynapseEnqueteManagement.BL.AiCommentSupport;
using SynapseEnqueteManagement.BL.AiSupport;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.InputValidation;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/commentsupport")]
public class CommentSupportController(
    IAiCommentSupportService commentSupportService,
    IInputValidationService inputValidation,
    IParticipantManager participantManager, IProjectManager projectManager)
    : ParticipantControllerBase(participantManager, projectManager)
{
    [HttpPost("chat")]
    public async Task<ActionResult<CommentSupportChatResponseDto>> Chat(
        int projectId,
        CommentSupportChatRequestDto dto,
        CancellationToken ct)
    {
        var (_, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        if (!string.IsNullOrWhiteSpace(dto.UserMessage))
        {
            var validation = inputValidation.Validate(dto.UserMessage);
            if (!validation.IsValid)
                return BadRequest(new CommentSupportChatResponseDto
                {
                    AssistantMessage = validation.Reason,
                    IsFinal          = true,
                });
        }

        var isFirstTurn = string.IsNullOrWhiteSpace(dto.UserMessage)
                          && dto.History.Count == 0;

        var lang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        Project project = null;
        try { project = await GetProjectByIdReadOnlyAsync(projectId); }
        catch { /* intentionally swallowed */ }

        var request = new CommentSupportRequest
        {
            IdeaText     = dto.IdeaText,
            CategoryName = dto.CategoryName,
            History = dto.History
                .Select(h => new AiSupportMessage(h.Role, h.Content))
                .ToList(),
            UserMessage  = dto.UserMessage ?? string.Empty,
            AgeRange     = AiSupportHelpers.FormatAgeRange(project?.MinAge, project?.MaxAge),
            Language     = lang,
            IsFirstTurn  = isFirstTurn,
        };

        var reply = await commentSupportService.ChatAsync(request, ct);

        return Ok(new CommentSupportChatResponseDto
        {
            AssistantMessage = reply.AssistantMessage,
            Suggestion       = reply.Suggestion,
            IsFinal          = reply.IsFinal,
        });
    }
}

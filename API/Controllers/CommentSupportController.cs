using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.CommentSupport;
using SynapseEnqueteManagement.BL.AiCommentSupport;
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
        [FromBody] CommentSupportChatRequestDto dto,
        CancellationToken ct)
    {
        var (_, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        // Validate the user's comment draft before sending to the AI.
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

        Project project = null;
        try { project = await GetProjectByIdReadOnlyAsync(projectId); }
        catch { /* intentionally swallowed */ }

        var request = new CommentSupportRequest
        {
            IdeaText     = dto.IdeaText,
            CategoryName = dto.CategoryName,
            History      = dto.History
                             .Select(h => new CommentSupportMessage
                             {
                                 Role    = h.Role,
                                 Content = h.Content,
                             })
                             .ToList(),
            UserMessage  = dto.UserMessage ?? string.Empty,
            AgeRange     = FormatAgeRange(project?.MinAge, project?.MaxAge),
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

    private static string FormatAgeRange(int? minAge, int? maxAge) => (minAge, maxAge) switch
    {
        (null, null)       => null,
        (var min, null)    => $"{min}+",
        (null, var max)    => $"0–{max}",
        (var min, var max) => $"{min}–{max}",
    };
}

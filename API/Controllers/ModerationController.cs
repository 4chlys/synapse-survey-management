using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.BL;
using SynapseEnqueteManagement.BL.AiModeration;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.API.Controllers;

/// <summary>
/// Three moderation endpoints consumed by the front-end:
///
///   POST /api/moderation/regex    — instant regex scan (call on every debounced keystroke)
///   POST /api/moderation/ai       — full AI scan (call on submit or after typing stops)
///   POST /api/moderation/suggest  — returns an AI-rewritten version of flagged content
///
/// The /ai endpoint returns a signed moderationToken that encodes the text hash
/// and moderation result. The submit endpoints (ideas, comments) verify this token
/// so the AI check is not repeated, yet the client cannot forge a result.
/// </summary>
[ApiController]
[Route("api/projects/{projectId}/moderation")]
public class ModerationController(IAiModerationService moderation, IModerationTokenService tokenService, IParticipantManager participantManager)
    : ParticipantControllerBase(participantManager)
{
    [HttpPost("regex")]
    public ActionResult<ModerationResponseDto> CheckRegex(int projectId,[FromBody] ModerationRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto?.Text))
            return Ok(new ModerationResponseDto { IsAcceptable = true });

        var result = moderation.CheckWithRegex(dto.Text);
        return Ok(MapToDto(result, dto.Text, projectId));
    }

    [HttpPost("ai")]
    public async Task<ActionResult<ModerationResponseDto>> CheckAi(int projectId,
        [FromBody] ModerationRequestDto dto,
        CancellationToken ct)
    {
        if (!TryGetParticipantToken(out _))
            return Unauthorized("Participant token required.");

        if (string.IsNullOrWhiteSpace(dto?.Text))
            return Ok(new ModerationResponseDto { IsAcceptable = true });

        var result = await moderation.CheckWithAiAsync(dto.Text, ct);
        return Ok(MapToDto(result, dto.Text, projectId));
    }

    [HttpPost("suggest")]
    public async Task<ActionResult<SuggestResponseDto>> Suggest(int projectId,
        [FromBody] ModerationRequestDto dto,
        CancellationToken ct)
    {
        if (!TryGetParticipantToken(out _))
            return Unauthorized("Participant token required.");

        if (string.IsNullOrWhiteSpace(dto?.Text))
            return Ok(new SuggestResponseDto { Suggestion = null });

        var suggestion = await moderation.SuggestRewriteAsync(dto.Text, ct);

        // Generate a token for the rewritten text (Unflagged since it was AI-cleaned)
        string suggestionToken = null;
        if (suggestion != null)
            suggestionToken = tokenService.GenerateToken(suggestion, ModerationStatus.Unflagged, projectId);

        return Ok(new SuggestResponseDto { Suggestion = suggestion, ModerationToken = suggestionToken });
    }

    private ModerationResponseDto MapToDto(ModerationResult result, string text, int projectId)
    {
        var status = result.IsAcceptable ? ModerationStatus.Unflagged : ModerationStatus.Flagged;
        var token = tokenService.GenerateToken(text, status, projectId);

        return new ModerationResponseDto
        {
            IsAcceptable    = result.IsAcceptable,
            Reason          = result.Reason,
            FlaggedBy       = result.FlaggedBy,
            ModerationToken = token,
        };
    }
}

public sealed class ModerationRequestDto
{
    public string Text { get; set; }
}

public sealed class ModerationResponseDto
{
    public bool   IsAcceptable    { get; set; }
    public string Reason          { get; set; }
    public string FlaggedBy       { get; set; }

    /// <summary>
    /// Signed token encoding the moderation result for the checked text.
    /// Pass this token when submitting the idea/comment so the API can
    /// verify the result without re-running the AI check.
    /// </summary>
    public string ModerationToken { get; set; }
}

public sealed class SuggestResponseDto
{
    public string Suggestion { get; set; }

    /// <summary>
    /// Token for the suggested (rewritten) text — pre-approved as Unflagged.
    /// Only present when Suggestion is not null.
    /// </summary>
    public string ModerationToken { get; set; }
}
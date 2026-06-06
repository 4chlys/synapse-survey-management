using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.Ai;
using SynapseEnqueteManagement.API.DTOs.Moderation;
using SynapseEnqueteManagement.BL.Interfaces;
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
public class ModerationController(
    IRegexModerationService regexModeration,
    IAiModerationService aiModeration,
    IModerationTokenService tokenService,
    IParticipantManager participantManager,
    IProjectManager projectManager,
    IPlatformAiConfigService platformAiConfig)
    : ParticipantControllerBase(participantManager, projectManager)
{
    [HttpPost("regex")]
    public ActionResult<ModerationResponseDto> CheckRegex(int projectId, ModerationRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto?.Text))
            return Ok(new ModerationResponseDto { IsAcceptable = true });

        var result = regexModeration.Check(dto.Text, dto.ContentType ?? "idea");
        return Ok(MapToDto(result, dto.Text, projectId));
    }

    [HttpPost("ai")]
    public async Task<ActionResult<ModerationResponseDto>> CheckAi(
        int projectId,
        ModerationRequestDto dto,
        CancellationToken ct)
    {
        if (!TryGetParticipantToken(out _)) return Unauthorized("Participant token required.");
        if (string.IsNullOrWhiteSpace(dto?.Text))
            return Ok(new ModerationResponseDto { IsAcceptable = true });

        var project = await GetProjectByIdReadOnlyAsync(projectId);
        var config = await platformAiConfig.ResolveAsync(project?.ProjectPlatformId ?? 0, ct);
        var result = await aiModeration.CheckWithAiAsync(dto.Text, config, projectId, ct, dto.ContentType ?? "idea", CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
        return Ok(MapToDto(result, dto.Text, projectId));
    }

    [HttpPost("suggest")]
    public async Task<ActionResult<SuggestResponseDto>> Suggest(
        int projectId,
        ModerationRequestDto dto,
        CancellationToken ct)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        if (string.IsNullOrWhiteSpace(dto?.Text))
            return Ok(new SuggestResponseDto { Suggestion = null });

        var project = await GetProjectByIdReadOnlyAsync(projectId);
        var config = await platformAiConfig.ResolveAsync(project?.ProjectPlatformId ?? 0, ct);
        var language = ResolveContentLanguageCode(participant, project, dto.Language);
        var suggestion = await aiModeration.SuggestRewriteAsync(dto.Text, config, projectId, ct, language);

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
            IsAcceptable = result.IsAcceptable,
            Reason = result.Reason,
            FlaggedBy = result.FlaggedBy,
            ModerationToken = token,
        };
    }
}
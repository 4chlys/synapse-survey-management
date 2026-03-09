using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.BL.AiModeration;

namespace SynapseEnqueteManagement.API.Controllers;

/// <summary>
/// Three moderation endpoints consumed by the front-end:
///
///   POST /api/moderation/regex    — instant regex scan (call on every debounced keystroke)
///   POST /api/moderation/ai       — full AI scan (call on submit or after typing stops)
///   POST /api/moderation/suggest  — returns an AI-rewritten version of flagged content
///
/// All return 200 with their respective response body.
/// </summary>
[ApiController]
[Route("api/moderation")]
public class ModerationController(IAiModerationService moderation) : ControllerBase
{
    [HttpPost("regex")]
    public ActionResult<ModerationResponseDto> CheckRegex([FromBody] ModerationRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto?.Text))
            return Ok(new ModerationResponseDto { IsAcceptable = true });

        var result = moderation.CheckWithRegex(dto.Text);
        return Ok(MapToDto(result));
    }

    [HttpPost("ai")]
    public async Task<ActionResult<ModerationResponseDto>> CheckAi(
        [FromBody] ModerationRequestDto dto,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto?.Text))
            return Ok(new ModerationResponseDto { IsAcceptable = true });

        var result = await moderation.CheckWithAiAsync(dto.Text, ct);
        return Ok(MapToDto(result));
    }

    /// <summary>
    /// Asks the AI to rewrite flagged content constructively.
    /// The front-end shows the suggestion in a popup — the user can accept
    /// (submitting the clean version) or decline (submitting the original as flagged).
    /// Returns 200 with Suggestion = null if no rewrite could be produced.
    /// </summary>
    [HttpPost("suggest")]
    public async Task<ActionResult<SuggestResponseDto>> Suggest(
        [FromBody] ModerationRequestDto dto,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto?.Text))
            return Ok(new SuggestResponseDto { Suggestion = null });

        var suggestion = await moderation.SuggestRewriteAsync(dto.Text, ct);
        return Ok(new SuggestResponseDto { Suggestion = suggestion });
    }

    private static ModerationResponseDto MapToDto(ModerationResult result) => new()
    {
        IsAcceptable = result.IsAcceptable,
        Reason       = result.Reason,
        FlaggedBy    = result.FlaggedBy,
    };
}

public sealed class ModerationRequestDto
{
    public string Text { get; set; }
}

public sealed class ModerationResponseDto
{
    public bool   IsAcceptable { get; set; }
    public string Reason       { get; set; }
    public string FlaggedBy    { get; set; }
}

public sealed class SuggestResponseDto
{
    /// <summary>
    /// The AI-rewritten version of the flagged text, or null if no
    /// meaningful rewrite could be produced (pure spam/abuse).
    /// </summary>
    public string Suggestion { get; set; }
}
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.BL.AiModeration;

namespace SynapseEnqueteManagement.API.Controllers;

/// <summary>
/// Exposes two moderation endpoints consumed by the front-end:
///
/// POST /api/moderation/regex   – instant regex scan (call on every debounced keystroke)
/// POST /api/moderation/ai      – full AI scan       (call on submit or after typing stops)
///
/// Both return 200 with a <see cref="ModerationResponseDto"/> body.
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
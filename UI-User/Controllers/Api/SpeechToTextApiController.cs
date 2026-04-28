using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SynapseEnqueteManagement.UI.User.Models.SpeechToText;
using SynapseEnqueteManagement.UI.User.Services.Session;
using SynapseEnqueteManagement.UI.User.Services.SpeechToText;

namespace SynapseEnqueteManagement.UI.User.Controllers.Api;

/// <summary>
/// Thin browser-facing API controller that proxies speech-to-text calls from
/// the TypeScript front-end to the backend API.
///
/// The browser cannot call the backend API directly (API key, CORS).
/// This controller acts as a same-origin relay, exactly as
/// <see cref="IdeaSupportApiController"/> does for idea support.
///
/// Route: /{projectId}/api/SpeechToTextApi/transcribe
/// </summary>
[ApiController]
[Route("{projectId:int}/api/[controller]")]
public class SpeechToTextApiController(
    SpeechToTextService speechToTextService,
    ISessionService sessionService) : ControllerBase
{
    [HttpPost("transcribe")]
    [EnableRateLimiting("ai-stt")]
    public async Task<ActionResult<SpeechToTextResponse>> Transcribe(
        int projectId,
        [FromBody] SpeechToTextRequest body)
    {
        var token = sessionService.GetToken(projectId);
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized("Participant session required.");

        SpeechToTextResponse result;
        try
        {
            result = await speechToTextService.TranscribeAsync(
                projectId,
                body.AudioData,
                body.MimeType);
        }
        catch (Exception)
        {
            return Ok(new SpeechToTextResponse
            {
                IsSuccess    = false,
                ErrorMessage = "Spraakherkenning is momenteel niet beschikbaar. Typ je bericht handmatig.",
            });
        }

        return Ok(result);
    }
}

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
        SpeechToTextRequest body)
    {
        var token = sessionService.GetToken(projectId);
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized("Participant session required.");

        var lang = string.IsNullOrWhiteSpace(body.Language)
            ? System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
            : body.Language;

        SpeechToTextResponse result;
        try
        {
            result = await speechToTextService.TranscribeAsync(
                projectId,
                body.AudioData,
                body.MimeType,
                lang);
        }
        catch (Exception)
        {
            return Ok(new SpeechToTextResponse
            {
                IsSuccess = false,
                ErrorCode = "service_unavailable",
            });
        }

        return Ok(result);
    }
}

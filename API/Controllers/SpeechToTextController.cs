using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.SpeechToText;
using SynapseEnqueteManagement.BL.AiSpeechToText;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/speechtotext")]
public class SpeechToTextController(
    IAiSpeechToTextService speechToTextService,
    AiSpeechToTextConfig speechToTextConfig,
    IParticipantManager participantManager, IProjectManager projectManager)
    : ParticipantControllerBase(participantManager, projectManager)
{
    [HttpPost("transcribe")]
    public async Task<ActionResult<SpeechToTextResponseDto>> Transcribe(
        int projectId,
        SpeechToTextRequestDto dto,
        CancellationToken ct)
    {
        var (_, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        byte[] audioBytes;
        try
        {
            audioBytes = Convert.FromBase64String(dto.AudioData);
        }
        catch (FormatException)
        {
            return BadRequest(new SpeechToTextResponseDto
            {
                IsSuccess    = false,
                ErrorCode    = SpeechToTextErrorCodes.InvalidAudioData,
                ErrorMessage = "Invalid audio data.",
            });
        }

        if (audioBytes.Length > speechToTextConfig.MaxAudioSizeBytes)
        {
            return BadRequest(new SpeechToTextResponseDto
            {
                IsSuccess    = false,
                ErrorCode    = SpeechToTextErrorCodes.TooLong,
                ErrorMessage = "Recording exceeds the configured maximum length.",
            });
        }

        var lang = string.IsNullOrWhiteSpace(dto.Language)
            ? System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
            : dto.Language;

        var request = new SpeechToTextRequest
        {
            AudioData = audioBytes,
            MimeType  = dto.MimeType,
            Language  = lang,
        };

        var result = await speechToTextService.TranscribeAsync(request, projectId, ct);

        return Ok(new SpeechToTextResponseDto
        {
            IsSuccess    = result.IsSuccess,
            Transcript   = result.Transcript,
            ErrorCode    = result.ErrorCode,
            ErrorMessage = result.ErrorMessage,
        });
    }
}

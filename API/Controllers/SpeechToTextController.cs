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
                ErrorMessage = "Ongeldige audiodata.",
            });
        }

        if (audioBytes.Length > speechToTextConfig.MaxAudioSizeBytes)
        {
            return BadRequest(new SpeechToTextResponseDto
            {
                IsSuccess    = false,
                ErrorMessage = "De opname is te lang. Houd het onder 30 seconden.",
            });
        }

        var request = new SpeechToTextRequest
        {
            AudioData = audioBytes,
            MimeType  = dto.MimeType,
        };

        var result = await speechToTextService.TranscribeAsync(request, ct);

        return Ok(new SpeechToTextResponseDto
        {
            IsSuccess    = result.IsSuccess,
            Transcript   = result.Transcript,
            ErrorMessage = result.ErrorMessage,
        });
    }
}

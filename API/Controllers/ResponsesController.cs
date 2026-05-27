using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Participants;
using SynapseEnqueteManagement.API.DTOs.Project;
using SynapseEnqueteManagement.API.DTOs.Survey;
using SynapseEnqueteManagement.API.Services;
using SynapseEnqueteManagement.BL.Interfaces;


namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}")]
public class ResponsesController(
    IParticipantManager participantManager,
    IResponseManager responseManager,
    ISubmissionManager submissionManager,
    IProjectManager projectManager,
    IParticipantLanguageCoordinator participantLanguageCoordinator)
    : ParticipantControllerBase(participantManager, projectManager)
{
    [HttpPatch("participants/me/language")]
    public async Task<ActionResult> SetLanguage(int projectId, SetParticipantLanguageDto dto, CancellationToken ct)
    {
        var (participant, error) = await GetParticipantAsync();
        if (error is not null) return error;

        try
        {
            await participantLanguageCoordinator.SwitchLanguageAsync(participant.ParticipantId, dto.LanguageCode, ct);
        }
        catch (KeyNotFoundException)
        {
            return BadRequest("Unknown or inactive language code.");
        }
        return NoContent();
    }

    [HttpPatch("participants/me/contact")]
    public async Task<ActionResult> SaveContactInfo(int projectId, SaveContactInfoDto dto, CancellationToken ct)
    {
        var (participant, error) = await GetParticipantAsync();
        if (error is not null) return error;

        await SaveContactInfoAsync(participant.ParticipantId, dto.Email, ct);

        return NoContent();
    }

    [HttpPost("participants")]
    public async Task<ActionResult<ParticipantDto>> StartSession(int projectId, [FromQuery] string languageCode, CancellationToken ct)
    {
        var participant = await AddParticipantAsync(projectId, languageCode ?? "nl", ct);
        return Created(string.Empty, new ParticipantDto { ParticipantToken = participant.SessionToken.ToString() });
    }

    [HttpPost("submit")]
    public async Task<ActionResult> Submit(int projectId, CancellationToken ct)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        await submissionManager.SubmitAsync(projectId, participant, ct);

        return Ok();
    }

    [HttpPost("responses")]
    public async Task<ActionResult<ResponseDto>> CreateResponse(int projectId, CreateResponseDto createDto,
        CancellationToken ct)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        var response =
            await responseManager.UpdateOrCreateResponseAsync(createDto.QuestionId, createDto.Value, participant, ct);
        var dto = ResponseDtoMapper.ToDto(response);
        return Created(string.Empty, dto);
    }

    [HttpDelete("responses")]
    public async Task<ActionResult> RemoveAllResponses(int projectId, CancellationToken ct)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        await responseManager.RemoveAllAsync(participant, ct);
        return NoContent();
    }

    [HttpGet("responses")]
    public async Task<ActionResult<IEnumerable<ResponseDto>>> GetResponses(int projectId)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        var responses = await responseManager.GetResponsesForParticipantReadOnlyAsync(participant);
        var dtos = responses.Select(ResponseDtoMapper.ToDto).ToList();
        return Ok(dtos);
    }

    [HttpGet("participants/me")]
    public async Task<ActionResult> GetParticipantStatus(int projectId)
    {
        if (!TryGetParticipantToken(out var token))
            return Ok(new ParticipantStatusDto { HasResponded = false });

        var hasSubmitted = await HasParticipantSubmittedAsync(token);
        return Ok(new ParticipantStatusDto { HasResponded = hasSubmitted });
    }

}
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Project;
using SynapseEnqueteManagement.API.DTOs.Survey;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;


namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}")]
public class ResponsesController(
    IParticipantManager participantManager,
    IResponseManager responseManager,
    ISubmissionManager submissionManager,
    IProjectManager projectManager)
    : ParticipantControllerBase(participantManager, projectManager)
{
    /// <summary>
    /// Saves optional contact info (email / phone) for the current participant.
    /// </summary>
    [HttpPatch("participants/me/contact")]
    public async Task<ActionResult> SaveContactInfo(int projectId, [FromBody] SaveContactInfoDto dto, CancellationToken ct)
    {
        var (participant, error) = await GetParticipantAsync();
        if (error is not null) return error;

        await SaveContactInfoAsync(participant.ParticipantId, dto.Email, ct);
        return NoContent();
    }

    /// <summary>
    /// Creates an anonymous participant and returns a session token.
    /// </summary>
    [HttpPost("participants")]
    public async Task<ActionResult<ParticipantDto>> StartSession(int projectId, CancellationToken ct)
    {
        var participant = await AddParticipantAsync(projectId, ct);
        return Created(string.Empty, new ParticipantDto { ParticipantToken = participant.SessionToken.ToString() });
    }

    /// <summary>
    /// Submits the survey for a participant who saved responses incrementally.
    /// Computes the visible path, validates mandatory answers, commits visible responses,
    /// and marks the participant as submitted.
    /// </summary>
    [HttpPost("submit")]
    public async Task<ActionResult> Submit(int projectId, CancellationToken ct)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        await submissionManager.SubmitAsync(projectId, participant, ct);
        return Ok();
    }

    /// <summary>
    /// Upserts a single response. Saves with IsCommitted = false (interim save).
    /// </summary>
    [HttpPost("responses")]
    public async Task<ActionResult<ResponseDto>> CreateResponse(int projectId, [FromBody] CreateResponseDto createDto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (participant, error) = await GetParticipantAsync();
        if (error is not null) return error;

        var response = await responseManager.UpdateOrCreateResponseAsync(createDto.QuestionId, createDto.Value, participant, ct);
        var dto = MapToDto(response);
        return Created(string.Empty, dto);
    }

    /// <summary>
    /// Returns all responses for the current participant (for resume support).
    /// </summary>
    [HttpGet("responses")]
    public async Task<ActionResult<IEnumerable<ResponseDto>>> GetResponses(int projectId)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        var responses = await responseManager.GetResponsesForParticipantReadOnlyAsync(participant.ParticipantId);
        var dtos = responses.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    /// <summary>
    /// Check if a participant (by token) has already submitted.
    /// </summary>
    [HttpGet("participants/me")]
    public async Task<ActionResult> GetParticipantStatus(int projectId)
    {
        if (!TryGetParticipantToken(out var token))
            return Ok(new ParticipantStatusDto { HasResponded = false });

        var hasSubmitted = await HasParticipantSubmittedAsync(token);
        return Ok(new ParticipantStatusDto { HasResponded = hasSubmitted });
    }

    private static ResponseDto MapToDto(Response response)
    {
        return new ResponseDto
        {
            ResponseId = response.ResponseId,
            Value = response.Value,
            QuestionId = response.Question?.QuestionId ?? 0,
            ParticipantId = response.Participant?.ParticipantId ?? 0
        };
    }
}

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs;
using SynapseEnqueteManagement.BL;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}")]
public class ResponsesController(
    IQuestionnaireManager questionnaireManager,
    IParticipantManager participantManager,
    IResponseManager responseManager)
    : ControllerBase
{
    private const string SessionHeader = "X-Participant-Token";

    /// <summary>
    /// Creates an anonymous participant and returns a session token.
    /// The client stores this token in localStorage and sends it as
    /// the X-Participant-Token header on subsequent requests.
    /// </summary>
    [HttpPost("participants")]
    public async Task<ActionResult<ParticipantDto>> StartSession(int projectId)
    {
        var questionnaire = await questionnaireManager.GetQuestionnaireByProjectIdAsync(projectId);
        if (questionnaire is null)
            return NotFound("Questionnaire not found.");

        var participant = await participantManager.AddParticipantAsync(questionnaire);

        return Created(string.Empty, new ParticipantDto { ParticipantToken = participant.SessionToken.ToString() });
    }

    [HttpGet("responses")]
    public async Task<ActionResult<IEnumerable<ResponseDto>>> GetAllResponses(int projectId)
    {
        var responses = await responseManager.GetResponsesByProjectIdAsync(projectId);
        var dtos = responses.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    /// <summary>
    /// Creates a participant and submits all answers in one request.
    /// Returns the participant token for the session cookie.
    /// </summary>
    [HttpPost("submit")]
    public async Task<ActionResult<ParticipantDto>> Submit(int projectId, [FromBody] SubmitSurveyDto submitDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var questionnaire = await questionnaireManager.GetQuestionnaireByProjectIdAsync(projectId);
        if (questionnaire is null)
            return NotFound("Questionnaire not found.");

        try
        {
            questionnaireManager.ValidateMandatoryAnswers(
                questionnaire,
                submitDto.Answers.Select(a => (a.QuestionId, a.Value)));
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Message);
        }

        var participant = await participantManager.AddParticipantAsync(questionnaire);

        foreach (var answer in submitDto.Answers)
        {
            if (string.IsNullOrWhiteSpace(answer.Value)) continue;

            var question = await questionnaireManager.GetQuestionByIdAsync(answer.QuestionId);
            if (question is null) continue;

            await responseManager.AddResponseAsync(answer.Value, question, participant);
        }

        return Created(string.Empty, new ParticipantDto { ParticipantToken = participant.SessionToken.ToString() });
    }

    /// <summary>
    /// Submit a response. The participant is identified by the
    /// X-Participant-Token header (obtained from POST /participants).
    /// </summary>
    [HttpPost("responses")]
    public async Task<ActionResult<ResponseDto>> CreateResponse(int projectId, [FromBody] CreateResponseDto createDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!TryGetParticipantToken(out var token))
            return Unauthorized("Missing or invalid X-Participant-Token header. Call POST /participants first.");

        var question = await questionnaireManager.GetQuestionByIdAsync(createDto.QuestionId);
        if (question is null)
            return NotFound($"Question with id {createDto.QuestionId} not found.");

        var participant = await participantManager.GetParticipantBySessionTokenAsync(token);
        if (participant is null)
            return NotFound("Participant session not found.");

        var response = await responseManager.AddResponseAsync(createDto.Value, question, participant);
        var dto = MapToDto(response);
        return CreatedAtAction(nameof(GetAllResponses), new { projectId, id = dto.ResponseId }, dto);
    }

    /// <summary>
    /// Check if a participant (by token) has already submitted responses.
    /// Returns { hasResponded: true/false }
    /// </summary>
    [HttpGet("participants/me")]
    public async Task<ActionResult> GetParticipantStatus(int projectId)
    {
        if (!TryGetParticipantToken(out var token))
            return Ok(new { HasResponded = false });

        var hasResponded = await participantManager.HasParticipantRespondedAsync(token);
        return Ok(new { HasResponded = hasResponded });
    }

    /// <summary>
    /// Extracts the participant GUID token from the X-Participant-Token header.
    /// </summary>
    private bool TryGetParticipantToken(out Guid token)
    {
        token = Guid.Empty;
        if (!Request.Headers.TryGetValue(SessionHeader, out var headerValue))
            return false;

        return Guid.TryParse(headerValue.ToString(), out token);
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

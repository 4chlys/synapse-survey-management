using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs;
using SynapseEnqueteManagement.BL;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}")]
public class ResponsesController : ControllerBase
{
    private const string SessionHeader = "X-Participant-Token";
    private readonly IManager _manager;

    public ResponsesController(IManager manager)
    {
        _manager = manager;
    }

    /// <summary>
    /// Creates an anonymous participant and returns a session token.
    /// The client stores this token in localStorage and sends it as
    /// the X-Participant-Token header on subsequent requests.
    /// </summary>
    [HttpPost("participants")]
    public ActionResult<ParticipantDto> StartSession(int projectId)
    {
        var questionnaire = _manager.GetQuestionnaireByProjectId(projectId);
        if (questionnaire is null)
            return NotFound("Questionnaire not found.");

        var participant = _manager.AddParticipant(questionnaire);

        return Created(string.Empty, new ParticipantDto { ParticipantToken = participant.SessionToken.ToString() });
    }

    [HttpGet("responses")]
    public ActionResult<IEnumerable<ResponseDto>> GetAllResponses(int projectId)
    {
        var responses = _manager.GetResponsesByProjectId(projectId);
        var dtos = responses.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    /// <summary>
    /// Submit a response. The participant is identified by the
    /// X-Participant-Token header (obtained from POST /participants).
    /// </summary>
    [HttpPost("responses")]
    public ActionResult<ResponseDto> CreateResponse(int projectId, [FromBody] CreateResponseDto createDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!TryGetParticipantToken(out var token))
            return Unauthorized("Missing or invalid X-Participant-Token header. Call POST /participants first.");

        var question = _manager.GetQuestionById(createDto.QuestionId);
        if (question is null)
            return NotFound($"Question with id {createDto.QuestionId} not found.");

        var participant = _manager.GetParticipantBySessionToken(token);
        if (participant is null)
            return NotFound("Participant session not found.");

        var response = _manager.AddResponse(createDto.Value, question, participant);
        var dto = MapToDto(response);
        return CreatedAtAction(nameof(GetAllResponses), new { projectId, id = dto.ResponseId }, dto);
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

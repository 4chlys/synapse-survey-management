using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Survey;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/questionnaire")]
public class QuestionnairesController(
    IQuestionnaireManager questionnaireManager,
    IParticipantManager participantManager)
    : ParticipantControllerBase(participantManager)
{
    [HttpGet]
    public async Task<ActionResult<QuestionnaireDto>> GetQuestionnaire(int projectId)
    {
        var questionnaire = await questionnaireManager.GetQuestionnaireByProjectIdAsync(projectId);
        if (questionnaire is null)
            return NotFound();

        var dto = MapToDto(questionnaire);
        return Ok(dto);
    }

    [HttpGet("questions")]
    public async Task<ActionResult<IEnumerable<QuestionDto>>> GetQuestions(int projectId)
    {
        var questionnaire = await questionnaireManager.GetQuestionnaireByProjectIdAsync(projectId);
        if (questionnaire is null)
            return NotFound();

        var dtos = questionnaire.Questions
            .OrderBy(q => q.Position)
            .Select(MapToDto)
            .ToList();
        return Ok(dtos);
    }

    [HttpPost("questions")]
    public async Task<ActionResult<QuestionDto>> AddQuestion(int projectId,
        [FromBody] CreateQuestionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!Enum.TryParse<QuestionFormat>(dto.QuestionFormat, out var format)) 
            return BadRequest("Invalid QuestionFormat.");

        var question = await questionnaireManager.AddQuestionAsync(projectId,
            dto.Text, format, dto.IsMandatory, dto.AnswerChoices);
        if (question is null)
            return NotFound();

        return Created(string.Empty, MapToDto(question));
    }
    
    [HttpPut("questions/{questionId}")]
    public async Task<IActionResult> UpdateQuestion(int questionId, [FromBody]
        UpdateQuestionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!Enum.TryParse<QuestionFormat>(dto.QuestionFormat, out var format)) 
            return BadRequest("Invalid QuestionFormat.");

        var success = await questionnaireManager.UpdateQuestionAsync(questionId,
            dto.Text, format, dto.IsMandatory, dto.AnswerChoices);
        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("questions/{questionId}")]
    public async Task<IActionResult> DeleteQuestion(int questionId)
    {
        var success = await questionnaireManager.DeleteQuestionAsync(questionId);
        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpGet("next-question")]
    public async Task<ActionResult> GetNextQuestion(int projectId, [FromQuery] int currentQuestionId)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        var nextQuestion = await questionnaireManager.GetNextQuestionAsync(currentQuestionId, participant.ParticipantId);
        return Ok(new { questionId = nextQuestion?.QuestionId });
    }

    [HttpGet("previous-question")]
    public async Task<ActionResult> GetPreviousQuestion(int projectId, [FromQuery] int currentQuestionId)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        var prevQuestion = await questionnaireManager.GetPreviousQuestionAsync(currentQuestionId, participant.ParticipantId);
        return Ok(new { questionId = prevQuestion?.QuestionId });
    }

    [HttpGet("resume-question")]
    public async Task<ActionResult> GetResumeQuestion(int projectId)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        var resumeQuestion = await questionnaireManager.GetResumeQuestionAsync(projectId, participant.ParticipantId);
        return Ok(new { questionId = resumeQuestion?.QuestionId });
    }

    [HttpGet("visible-path")]
    public async Task<ActionResult> GetVisiblePath(int projectId)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        var path = await questionnaireManager.ComputeVisiblePathAsync(projectId, participant.ParticipantId);
        return Ok(new { questionIds = path });
    }

    private static QuestionnaireDto MapToDto(Questionnaire questionnaire)
    {
        return new QuestionnaireDto
        {
            QuestionnaireId = questionnaire.QuestionnaireId,
            Questions = questionnaire.Questions
                .OrderBy(q => q.Position)
                .Select(MapToDto)
                .ToList()
        };
    }

    private static QuestionDto MapToDto(Question question)
    {
        return new QuestionDto
        {
            QuestionId = question.QuestionId,
            Text = question.Text,
            QuestionFormat = question.QuestionFormat.ToString(),
            AnswerChoices = question.AnswerChoices.ToList(),
            IsMandatory = question.IsMandatory,
            Position = question.Position,
            BranchRules = question.BranchRules.Select(br => new BranchRuleDto
            {
                TriggerValue = br.TriggerValue,
                TargetQuestionId = br.SkipToEnd ? null : br.TargetQuestion?.QuestionId,
                SkipToEnd = br.SkipToEnd
            }).ToList()
        };
    }
}
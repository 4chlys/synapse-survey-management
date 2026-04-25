using System.ComponentModel.DataAnnotations;
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
    ISurveyFlowManager surveyFlowManager,
    IParticipantManager participantManager,
    IProjectManager projectManager)
    : ParticipantControllerBase(participantManager, projectManager)
{
    [HttpGet]
    public async Task<ActionResult<QuestionnaireDto>> GetQuestionnaire(int projectId)
    {
        var questionnaire = await questionnaireManager.GetQuestionnaireByProjectIdWithQuestionsReadOnlyAsync(projectId);
        if (questionnaire is null)
            return NotFound();

        var dto = MapToDto(questionnaire);
        return Ok(dto);
    }

    [HttpGet("questions")]
    public async Task<ActionResult<IEnumerable<QuestionDto>>> GetQuestions(int projectId)
    {
        var questionnaire = await questionnaireManager.GetQuestionnaireByProjectIdReadOnlyAsync(projectId);
        if (questionnaire is null)
            return NotFound();

        var questions = await questionnaireManager.GetQuestionsForProjectAsync(projectId);
        var dtos = questions.Select(MapToDto).ToList();
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

        try
        {
            var question = await questionnaireManager.AddQuestionAsync(projectId,
                dto.Text, format, dto.IsMandatory, dto.AnswerChoices?.ToList(), dto.SectionId,
                dto.ParentQuestionId, dto.ParentTriggerValue, dto.EndingAnswers?.ToList());
            if (question is null)
                return NotFound();

            return Created(string.Empty, MapToDto(question));
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("questions/{questionId}")]
    public async Task<IActionResult> UpdateQuestion(int questionId, [FromBody] UpdateQuestionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!Enum.TryParse<QuestionFormat>(dto.QuestionFormat, out var format))
            return BadRequest("Invalid QuestionFormat.");

        var success = await questionnaireManager.UpdateQuestionAsync(questionId,
            dto.Text, format, dto.IsMandatory, dto.AnswerChoices?.ToList(), dto.SectionId,
            dto.EndingAnswers?.ToList());
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

    [HttpPut("questions/{questionId}/location")]
    public async Task<ActionResult<QuestionDto>> MoveQuestion(int questionId, [FromBody] MoveQuestionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var moved = await questionnaireManager.MoveQuestionAsync(
                questionId, dto.ParentQuestionId, dto.ParentTriggerValue, dto.Position);
            if (moved is null)
                return NotFound();
            return Ok(MapToDto(moved));
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("next-question")]
    public async Task<ActionResult> GetNextQuestion(int projectId, [FromQuery] int currentQuestionId)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        var nextQuestion = await surveyFlowManager.GetNextQuestionAsync(currentQuestionId, participant.ParticipantId);
        return Ok(new { questionId = nextQuestion?.QuestionId });
    }

    [HttpGet("previous-question")]
    public async Task<ActionResult> GetPreviousQuestion(int projectId, [FromQuery] int currentQuestionId)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        var prevQuestion = await surveyFlowManager.GetPreviousQuestionAsync(currentQuestionId, participant.ParticipantId);
        return Ok(new { questionId = prevQuestion?.QuestionId });
    }

    [HttpGet("resume-question")]
    public async Task<ActionResult> GetResumeQuestion(int projectId)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        var resumeQuestion = await surveyFlowManager.GetResumeQuestionAsync(projectId, participant.ParticipantId);
        return Ok(new { questionId = resumeQuestion?.QuestionId });
    }

    [HttpGet("visible-path")]
    public async Task<ActionResult> GetVisiblePath(int projectId)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        var path = await surveyFlowManager.ComputeVisiblePathAsync(projectId, participant.ParticipantId);
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
            ParentQuestionId = question.ParentQuestion?.QuestionId,
            ParentTriggerValue = question.ParentTriggerValue,
            EndingAnswers = question.EndingAnswers.Select(e => e.Value).ToList(),
            SectionName = question.Section?.Name,
            SectionId = question.Section?.SectionId
        };
    }
}

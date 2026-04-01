using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Survey;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/questionnaire")]
public class QuestionnairesController(IQuestionnaireManager questionnaireManager) : ControllerBase
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

        var dtos = questionnaire.Questions.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    [HttpPost("questions")]
    public async Task<ActionResult<QuestionDto>> AddQuestion(int projectId,
        [FromBody] CreateQuestionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!Enum.TryParse<SynapseEnqueteManagement.BL.Domain.Project.Enums.QuestionFormat>(dto.QuestionFormat, out var format)) 
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

        if (!Enum.TryParse<SynapseEnqueteManagement.BL.Domain.Project.Enums.QuestionFormat>(dto.QuestionFormat, out var format)) 
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

    private static QuestionnaireDto MapToDto(Questionnaire questionnaire)
    {
        return new QuestionnaireDto
        {
            QuestionnaireId = questionnaire.QuestionnaireId,
            Questions = questionnaire.Questions.Select(MapToDto).ToList()
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
        };
    }
}
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs;
using SynapseEnqueteManagement.BL;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/questionnaire")]
public class QuestionnairesController : ControllerBase
{
    private readonly IManager _manager;

    public QuestionnairesController(IManager manager)
    {
        _manager = manager;
    }

    [HttpGet]
    public ActionResult<QuestionnaireDto> GetQuestionnaire(int projectId)
    {
        var questionnaire = _manager.GetQuestionnaireByProjectId(projectId);
        if (questionnaire is null)
            return NotFound();

        var dto = MapToDto(questionnaire);
        return Ok(dto);
    }

    [HttpGet("questions")]
    public ActionResult<IEnumerable<QuestionDto>> GetQuestions(int projectId)
    {
        var questionnaire = _manager.GetQuestionnaireByProjectId(projectId);
        if (questionnaire is null)
            return NotFound();

        var dtos = questionnaire.Questions.Select(MapToDto).ToList();
        return Ok(dtos);
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
            AnswerChoices = question.AnswerChoices.ToList()
        };
    }
}
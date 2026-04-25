using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.Admin.Models.Question;
using SynapseEnqueteManagement.UI.Admin.Services.Question;

namespace SynapseEnqueteManagement.UI.Admin.Controllers.Api;

[ApiController]
[Route("api/admin/projects/{projectId:int}/questions")]
public class QuestionsApiController(QuestionAdminService questionService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<QuestionViewModel>>> GetQuestions(int projectId)
    {
        var questions = await questionService.GetQuestionsAsync(projectId);
        return Ok(questions);
    }

    [HttpPut("{questionId:int}/location")]
    public async Task<ActionResult> Move(int projectId, int questionId, [FromBody] MoveRequest request)
    {
        try
        {
            await questionService.MoveQuestionAsync(projectId, questionId,
                request.ParentQuestionId, request.ParentTriggerValue, request.Position);
            return NoContent();
        }
        catch (HttpRequestException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{questionId:int}/ending-answers")]
    public async Task<ActionResult> UpdateEndingAnswers(int projectId, int questionId, [FromBody] EndingAnswersRequest request)
    {
        var questions = await questionService.GetQuestionsAsync(projectId);
        var question = questions.FirstOrDefault(q => q.QuestionId == questionId);
        if (question is null) return NotFound();

        await questionService.UpdateQuestionAsync(projectId, questionId,
            question.Text, question.QuestionFormat, question.IsMandatory,
            question.AnswerChoices, question.SectionId, request.EndingAnswers);
        return NoContent();
    }

    public class MoveRequest
    {
        public int? ParentQuestionId { get; set; }
        public string ParentTriggerValue { get; set; }
        public int Position { get; set; }
    }

    public class EndingAnswersRequest
    {
        public ICollection<string> EndingAnswers { get; set; } = new List<string>();
    }
}

using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.Admin.Models.Question;
using SynapseEnqueteManagement.UI.Admin.Services.Question;

namespace SynapseEnqueteManagement.UI.Admin.Controllers;

public class QuestionController(QuestionAdminService questionService) : Controller
{
    public async Task<IActionResult> Index(int projectId)
    {
        var questions = await questionService.GetQuestionsAsync(projectId);
        ViewBag.ProjectId = projectId;
        return View(questions);
    }

    public IActionResult Create(int projectId)
    {
        ViewBag.ProjectId = projectId;
        return View(new CreateQuestionViewModel());
    }
    [HttpPost]
    public async Task<IActionResult> Create(int projectId,
        CreateQuestionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ProjectId = projectId;
            return View(model);
        }

        await questionService.CreateQuestionAsync(projectId, model.Text,
            model.QuestionFormat, model.IsMandatory, model.GetAnswerChoices());
        return RedirectToAction(nameof(Index), new { projectId });
    }

    public async Task<IActionResult> Edit(int projectId, int questionId)
    {
        var questions = await questionService.GetQuestionsAsync(projectId);
        var question = questions.FirstOrDefault(q => q.QuestionId == questionId);
        if (question is null)
            return NotFound();

        var model = new EditQuestionViewModel
        {
            QuestionId = questionId,
            Text = question.Text,
            QuestionFormat = question.QuestionFormat,
            IsMandatory = question.IsMandatory,
            AnswerChoicesRaw = string.Join('\n', question.AnswerChoices)
        };
        ViewBag.ProjectId = projectId;
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int projectId, int questionId,
        EditQuestionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ProjectId = projectId;
            return View(model);
        }

        await questionService.UpdateQuestionAsync(projectId, questionId,
            model.Text, model.QuestionFormat, model.IsMandatory, model.GetAnswerChoices());
        return RedirectToAction(nameof(Index), new { projectId });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int projectId, int questionId)
    {
        await questionService.DeleteQuestionAsync(projectId, questionId);
        return RedirectToAction(nameof(Index), new { projectId });
    }
}

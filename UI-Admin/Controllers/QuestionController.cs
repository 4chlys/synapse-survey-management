using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using SynapseEnqueteManagement.UI.Admin.Filters;
using SynapseEnqueteManagement.UI.Admin.Models.Enums;
using SynapseEnqueteManagement.UI.Admin.Models.Project;
using SynapseEnqueteManagement.UI.Admin.Models.Question;
using SynapseEnqueteManagement.UI.Admin.Services.Project;
using SynapseEnqueteManagement.UI.Admin.Services.Question;

namespace SynapseEnqueteManagement.UI.Admin.Controllers;

[Authorize(Roles = "ProjectPlatformAdmin")]
[RequireProjectPlatformOwnership]
public class QuestionController(
    QuestionAdminService questionService,
    ProjectAdminService projectService,
    IStringLocalizer<SharedResource> localizer) : Controller
{
    public async Task<IActionResult> Index(int projectId)
    {
        var questions = await questionService.GetQuestionsAsync(projectId);
        ViewData["ProjectId"] = projectId;
        return View(questions);
    }

    public IActionResult Create(int projectId)
    {
        ViewData["ProjectId"] = projectId;
        return View(new CreateQuestionViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(int projectId,
        CreateQuestionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ProjectId"] = projectId;
            return View(model);
        }

        await questionService.CreateQuestionAsync(projectId, model.Text,
            model.QuestionFormat.ToString(), model.IsMandatory, model.GetAnswerChoices());
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
            QuestionFormat = Enum.TryParse<QuestionFormat>(question.QuestionFormat, ignoreCase: true, out var qf) ? qf : QuestionFormat.Open,
            IsMandatory = question.IsMandatory,
            AnswerChoicesRaw = string.Join('\n', question.AnswerChoices),
            SectionId = question.SectionId
        };
        ViewData["ProjectId"] = projectId;
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int projectId, int questionId,
        EditQuestionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ProjectId"] = projectId;
            return View(model);
        }

        await questionService.UpdateQuestionAsync(projectId, questionId,
            model.Text, model.QuestionFormat.ToString(), model.IsMandatory, model.GetAnswerChoices(), model.SectionId);
        return RedirectToAction(nameof(Index), new { projectId });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int projectId, int questionId, AiAssistanceType aiAssistance)
    {
        var project = await projectService.GetProjectByIdAsync(projectId);
        if (project?.ProjectState is "Active" or "Archived")
        {
            TempData["Error"] = localizer["Project_LockedNotEditable"].Value;
            return RedirectToAction("Questions", "Project", new { projectId, aiAssistance });
        }
        await questionService.DeleteQuestionAsync(projectId, questionId);
        return RedirectToAction("Questions", "Project", new { projectId, aiAssistance });
    }
}

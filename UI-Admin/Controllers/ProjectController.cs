using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.Admin.Models.Project;
using SynapseEnqueteManagement.UI.Admin.Models.Question;
using SynapseEnqueteManagement.UI.Admin.Services.Project;
using SynapseEnqueteManagement.UI.Admin.Services.Question;

namespace SynapseEnqueteManagement.UI.Admin.Controllers;

public class ProjectController(ProjectAdminService projectService, QuestionAdminService questionService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var projects = await projectService.GetAllProjectsAsync();
        return View(projects);
    }

    public IActionResult Create()
    {
        return View(new CreateProjectViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProjectViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var project = await projectService.CreateProjectAsync(model.Name, model.ImageUrl);
        return RedirectToAction(nameof(Questions), new { projectId = project.ProjectId, aiAssistance = model.AiAssistance });
    }

    public async Task<IActionResult> Questions(int projectId, string aiAssistance = "ProActive")
    {
        var questions = await questionService.GetQuestionsAsync(projectId);
        ViewBag.ProjectId = projectId;
        ViewBag.AiAssistance = aiAssistance;
        return View(questions);
    }

    [HttpPost]
    public async Task<IActionResult> CreateQuestion(int projectId, string aiAssistance, CreateQuestionViewModel model)
    {
        if (ModelState.IsValid)
            await questionService.CreateQuestionAsync(projectId, model.Text, model.QuestionFormat, model.IsMandatory, model.GetAnswerChoices());
        return RedirectToAction(nameof(Questions), new { projectId, aiAssistance });
    }

    public async Task<IActionResult> Dashboard(int projectId)
    {
        var project = await projectService.GetProjectByIdAsync(projectId);
        if (project is null)
            return NotFound();

        return View(project);
    }
}

using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.Admin.Models.Category;
using SynapseEnqueteManagement.UI.Admin.Models.Enums;
using SynapseEnqueteManagement.UI.Admin.Models.Project;
using SynapseEnqueteManagement.UI.Admin.Models.Question;
using SynapseEnqueteManagement.UI.Admin.Services.Category;
using SynapseEnqueteManagement.UI.Admin.Services.Project;
using SynapseEnqueteManagement.UI.Admin.Services.Question;
using SynapseEnqueteManagement.UI.Admin.Services.Section;

namespace SynapseEnqueteManagement.UI.Admin.Controllers;

public class ProjectController(
    ProjectAdminService projectService,
    QuestionAdminService questionService,
    CategoryAdminService categoryService,
    SectionAdminService sectionService,
    IWebHostEnvironment env) : Controller
{
    public async Task<IActionResult> Index()
    {
        var projects = await projectService.GetAllProjectsAsync();
        return View(projects);
    }

    public async Task<IActionResult> Create(int? projectId)
    {
        if (!projectId.HasValue)
            return View(new CreateProjectViewModel());

        var project = await projectService.GetProjectByIdAsync(projectId.Value);
        if (project is null)
            return View(new CreateProjectViewModel());

        var sections = await sectionService.GetSectionsAsync(projectId.Value);
        var model = new CreateProjectViewModel
        {
            ProjectId = projectId,
            Name = project.Name,
            Topic = project.Topic,
            Introduction = project.Introduction,
            ImageUrl = project.ProjectImage,
            InteractionMode = project.FormType,
            AiAssistance = project.AiAssistance,
            MinAge = project.MinAge,
            MaxAge = project.MaxAge,
            ContactOptIn = project.RequestContactInfo,
            Sections = sections.Select(s => s.Name).ToList()
        };
        ViewData["Project"] = project;
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProjectViewModel model, string submitAction)
    {
        // For "next", Topic and Introduction are also required
        if (submitAction == "next")
        {
            if (string.IsNullOrWhiteSpace(model.Topic))
                ModelState.AddModelError(nameof(model.Topic), "Onderwerp is verplicht.");
            if (string.IsNullOrWhiteSpace(model.Introduction))
                ModelState.AddModelError(nameof(model.Introduction), "Introductie is verplicht.");
        }

        if (!ModelState.IsValid)
            return View(model);

        ProjectAdminViewModel project;
        if (model.ProjectId.HasValue)
        {
            project = await projectService.UpdateProjectAsync(
                model.ProjectId.Value, model.Name, model.Topic, model.Introduction,
                model.ImageUrl, model.InteractionMode, model.AiAssistance, model.MinAge, model.MaxAge, model.ContactOptIn);
        }
        else
        {
            project = await projectService.CreateProjectAsync(
                model.Name, model.Topic, model.Introduction,
                model.ImageUrl, model.InteractionMode, model.AiAssistance, model.MinAge, model.MaxAge, model.ContactOptIn);
        }

        // Sync sections: keep existing by name, delete removed, add new
        var existingSections = await sectionService.GetSectionsAsync(project.ProjectId);
        var newNames = (model.Sections ?? new List<string>())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToList();

        foreach (var s in existingSections)
            if (!newNames.Contains(s.Name))
                await sectionService.DeleteSectionAsync(project.ProjectId, s.SectionId);

        var existingNames = existingSections.Select(s => s.Name).ToList();
        foreach (var name in newNames)
            if (!existingNames.Contains(name))
                await sectionService.CreateSectionAsync(project.ProjectId, name, string.Empty);

        if (submitAction == "next")
            return RedirectToAction(nameof(Questions), new { projectId = project.ProjectId, aiAssistance = model.AiAssistance });

        return RedirectToAction(nameof(Create), new { projectId = project.ProjectId });
    }

    public async Task<IActionResult> Questions(int projectId, AiAssistanceType aiAssistance = AiAssistanceType.ProActive)
    {
        var questions = await questionService.GetQuestionsAsync(projectId);
        var project = await projectService.GetProjectByIdAsync(projectId);
        var sections = await sectionService.GetSectionsAsync(projectId);
        ViewData["ProjectId"] = projectId;
        ViewData["AiAssistance"] = aiAssistance;
        ViewData["Project"] = project;
        ViewData["Sections"] = sections;
        return View(questions);
    }

    [HttpPost]
    public async Task<IActionResult> CreateQuestion(int projectId, AiAssistanceType aiAssistance, CreateQuestionViewModel model)
    {
        if (ModelState.IsValid)
            await questionService.CreateQuestionAsync(projectId, model.Text, model.QuestionFormat.ToString(), model.IsMandatory, model.GetAnswerChoices(), model.SectionId);
        return RedirectToAction(nameof(Questions), new { projectId, aiAssistance });
    }

    public async Task<IActionResult> Ideation(int projectId)
    {
        var project = await projectService.GetProjectByIdAsync(projectId);
        var categories = await categoryService.GetCategoriesAsync(projectId);
        ViewData["ProjectId"] = projectId;
        ViewData["Project"] = project;
        return View(categories);
    }

    [HttpPost]
    public async Task<IActionResult> AddCategory(int projectId, CategoryFormViewModel model)
    {
        if (ModelState.IsValid)
            await categoryService.CreateCategoryAsync(projectId, model.Name, model.Description ?? string.Empty);
        return RedirectToAction(nameof(Ideation), new { projectId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int projectId, int categoryId)
    {
        await categoryService.DeleteCategoryAsync(projectId, categoryId);
        return RedirectToAction(nameof(Ideation), new { projectId });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Geen bestand ontvangen." });

        var uploadsDir = Path.Combine(env.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using var stream = System.IO.File.Create(filePath);
        await file.CopyToAsync(stream);

        return Ok(new { url = $"/uploads/{fileName}" });
    }

    public async Task<IActionResult> Dashboard(int projectId)
    {
        var project = await projectService.GetProjectByIdAsync(projectId);
        if (project is null)
            return NotFound();

        return View(project);
    }
}

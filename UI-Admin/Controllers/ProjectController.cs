using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using SynapseEnqueteManagement.UI.Admin.Data;
using SynapseEnqueteManagement.UI.Admin.Models.Category;
using SynapseEnqueteManagement.UI.Admin.Models.Enums;
using SynapseEnqueteManagement.UI.Admin.Models.Project;
using SynapseEnqueteManagement.UI.Admin.Models.Question;
using SynapseEnqueteManagement.UI.Admin.Services.Category;
using SynapseEnqueteManagement.UI.Admin.Services.Dashboard;
using SynapseEnqueteManagement.UI.Admin.Services.Project;
using SynapseEnqueteManagement.UI.Admin.Services.Question;
using SynapseEnqueteManagement.UI.Admin.Models.Dashboard;
using SynapseEnqueteManagement.UI.Admin.Services.Platform;
using SynapseEnqueteManagement.UI.Admin.Services.Section;
using SynapseEnqueteManagement.UI.Admin.Services.Upload;

namespace SynapseEnqueteManagement.UI.Admin.Controllers;

[Authorize(Roles = "ProjectPlatformAdmin")]
public class ProjectController(
    ProjectAdminService projectService,
    QuestionAdminService questionService,
    CategoryAdminService categoryService,
    SectionAdminService sectionService,
    DashboardAdminService dashboardService,
    UploadAdminService uploadService,
    PlatformAdminService platformService,
    UserManager<ApplicationUser> userManager,
    WidgetPreferenceService widgetPreferenceService,
    IdeaAdminService ideaService,
    ParticipantAdminService participantService,
    IOptions<AdminSettings> adminSettings) : Controller
{
    public async Task<IActionResult> Index()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Auth");
        var projects = await projectService.GetAllProjectsAsync(user.ProjectPlatformId);
        ViewData["ActiveSection"] = "Projecten";
        return View(projects);
    }

    public async Task<IActionResult> Create(int? projectId, bool isNew = false)
    {
        if (!projectId.HasValue)
        {
            var user = await userManager.GetUserAsync(User);
            if (user?.ProjectPlatformId is not null)
            {
                var platform = await platformService.GetPlatformByIdAsync(user.ProjectPlatformId.Value);
                ViewData["Project"] = new ProjectAdminViewModel { OrganisationLogoUrl = platform?.LogoUrl };
            }
            ViewData["IsNew"]       = true;
            ViewData["HideSidebar"] = true;
            return View(new CreateProjectViewModel());
        }

        var project = await projectService.GetProjectByIdAsync(projectId.Value);
        if (project is null)
            return View(new CreateProjectViewModel());

        var sections = await sectionService.GetSectionsAsync(projectId.Value);
        var model = new CreateProjectViewModel
        {
            ProjectId = projectId,
            Name = project.Name,
            ShortForm = project.ShortForm,
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
        ViewData["ProjectId"]     = projectId.Value;
        ViewData["ProjectName"]   = project.Name;
        ViewData["PlatformName"]  = project.OrganisationName ?? "";
        ViewData["ProjectState"]  = project.ProjectState ?? "";
        ViewData["ActiveSection"] = "Instellingen";
        ViewData["IsNew"]         = isNew;
        ViewData["HideSidebar"]   = isNew;
        return View(model);
    }

    [HttpPost]
    [EnableRateLimiting("ai-image-generation")]
    public async Task<IActionResult> Create(CreateProjectViewModel model, string submitAction, bool isNew = false)
    {
        // For "next", Topic and Introduction are also required
        if (submitAction == "next")
        {
            if (string.IsNullOrWhiteSpace(model.Topic))
                ModelState.AddModelError(nameof(model.Topic), "Onderwerp is verplicht.");
            if (string.IsNullOrWhiteSpace(model.Introduction))
                ModelState.AddModelError(nameof(model.Introduction), "Introductie is verplicht.");
        }

        if (model.MinAge.HasValue && model.MaxAge.HasValue && model.MinAge > model.MaxAge)
            ModelState.AddModelError(nameof(model.MinAge), "MinLeeftijd mag niet groter zijn dan MaxLeeftijd.");

        var wizardActive = isNew || !model.ProjectId.HasValue;

        if (!ModelState.IsValid)
        {
            if (model.ProjectId.HasValue)
            {
                var existingProject = await projectService.GetProjectByIdAsync(model.ProjectId.Value);
                ViewData["ProjectId"]     = model.ProjectId.Value;
                ViewData["ProjectName"]   = existingProject?.Name ?? model.Name ?? "";
                ViewData["PlatformName"]  = existingProject?.OrganisationName ?? "";
                ViewData["ProjectState"]  = existingProject?.ProjectState ?? "";
            }
            ViewData["ActiveSection"] = "Instellingen";
            ViewData["IsNew"]         = wizardActive;
            ViewData["HideSidebar"]   = wizardActive;
            return View(model);
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Auth");
        ProjectAdminViewModel project;
        if (model.ProjectId.HasValue)
        {
            var lockCheck = await projectService.GetProjectByIdAsync(model.ProjectId.Value);
            if (IsProjectLocked(lockCheck))
            {
                TempData["Error"] = "Dit project is actief of gearchiveerd en kan niet meer worden bewerkt.";
                return RedirectToAction(nameof(Create), new { projectId = model.ProjectId.Value });
            }
            project = await projectService.UpdateProjectAsync(
                model.ProjectId.Value, model.Name, model.Topic, model.Introduction,
                model.ImageUrl, model.InteractionMode, model.AiAssistance, model.MinAge, model.MaxAge,
                model.ContactOptIn, model.ShortForm,
                lockCheck.IdeaPageSize, lockCheck.ExtraIdeasReplaceExisting, lockCheck.MaxExtraIdeaRequests);
        }
        else
        {
            if (user.ProjectPlatformId is null)
            {
                ModelState.AddModelError(string.Empty, "Uw account is niet gekoppeld aan een platform.");
                return View(model);
            }
            project = await projectService.CreateProjectAsync(
                model.Name, model.Topic, model.Introduction,
                model.ImageUrl, model.InteractionMode, model.AiAssistance,
                model.MinAge, model.MaxAge, model.ContactOptIn,
                platformId: user.ProjectPlatformId.Value,
                shortForm: model.ShortForm);
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
            return RedirectToAction(nameof(Questions),
                new { projectId = project.ProjectId, aiAssistance = model.AiAssistance, isNew = true });

        return RedirectToAction(nameof(Create), new { projectId = project.ProjectId, isNew = wizardActive, saved = 1 });
    }

    public async Task<IActionResult> Questions(int projectId,
        AiAssistanceType aiAssistance = AiAssistanceType.ProActive, bool isNew = false)
    {
        var questions = await questionService.GetQuestionsAsync(projectId);
        var project = await projectService.GetProjectByIdAsync(projectId);
        var sections = await sectionService.GetSectionsAsync(projectId);
        ViewData["ProjectId"]     = projectId;
        ViewData["ProjectName"]   = project?.Name ?? "";
        ViewData["PlatformName"]  = project?.OrganisationName ?? "";
        ViewData["ProjectState"]  = project?.ProjectState ?? "";
        ViewData["ActiveSection"] = "Vragen";
        ViewData["AiAssistance"]  = aiAssistance;
        ViewData["Project"]       = project;
        ViewData["Sections"]      = sections;
        ViewData["IsNew"]         = isNew;
        ViewData["HideSidebar"]   = isNew;
        return View(questions);
    }

    [HttpPost]
    public async Task<IActionResult> CreateQuestion(int projectId, AiAssistanceType aiAssistance,
        bool isNew, CreateQuestionViewModel model)
    {
        var projectForCreate = await projectService.GetProjectByIdAsync(projectId);
        if (IsProjectLocked(projectForCreate))
        {
            TempData["Error"] = "Dit project is actief of gearchiveerd en kan niet meer worden bewerkt.";
            return RedirectToAction(nameof(Questions), new { projectId, aiAssistance, isNew });
        }
        if (ModelState.IsValid)
            await questionService.CreateQuestionAsync(projectId, model.Text, model.QuestionFormat.ToString(),
                model.IsMandatory, model.GetAnswerChoices(), model.SectionId,
                mediaUrl: model.MediaUrl, mediaType: model.MediaType,
                mediaIsEmbed: model.MediaIsEmbed, mediaText: model.MediaText);
        return RedirectToAction(nameof(Questions), new { projectId, aiAssistance, isNew, saved = 1 });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateQuestion(int projectId, AiAssistanceType aiAssistance,
        bool isNew, EditQuestionViewModel model)
    {
        var projectForUpdate = await projectService.GetProjectByIdAsync(projectId);
        if (IsProjectLocked(projectForUpdate))
        {
            TempData["Error"] = "Dit project is actief of gearchiveerd en kan niet meer worden bewerkt.";
            return RedirectToAction(nameof(Questions), new { projectId, aiAssistance, isNew });
        }
        if (ModelState.IsValid)
            await questionService.UpdateQuestionAsync(projectId, model.QuestionId, model.Text, model.QuestionFormat.ToString(),
                model.IsMandatory, model.GetAnswerChoices(), model.SectionId,
                mediaUrl: model.MediaUrl, mediaType: model.MediaType,
                mediaIsEmbed: model.MediaIsEmbed, mediaText: model.MediaText);
        return RedirectToAction(nameof(Questions), new { projectId, aiAssistance, isNew, saved = 1 });
    }

    public async Task<IActionResult> Ideation(int projectId, bool isNew = false)
    {
        var project = await projectService.GetProjectByIdAsync(projectId);
        var categories = await categoryService.GetCategoriesAsync(projectId);
        ViewData["ProjectId"]     = projectId;
        ViewData["ProjectName"]   = project?.Name ?? "";
        ViewData["PlatformName"]  = project?.OrganisationName ?? "";
        ViewData["ProjectState"]  = project?.ProjectState ?? "";
        ViewData["ActiveSection"] = "Categorieën";
        ViewData["Project"]       = project;
        ViewData["IsNew"]         = isNew;
        ViewData["HideSidebar"]   = isNew;
        return View(categories);
    }

    [HttpPost]
    public async Task<IActionResult> SaveIdeationSettings(int projectId, bool isNew, int ideaPageSize, bool extraIdeasReplaceExisting, int maxExtraIdeaRequests)
    {
        var project = await projectService.GetProjectByIdAsync(projectId);
        if (IsProjectLocked(project))
        {
            TempData["Error"] = "Dit project is actief of gearchiveerd en kan niet meer worden bewerkt.";
            return RedirectToAction(nameof(Ideation), new { projectId, isNew });
        }

        await projectService.UpdateProjectAsync(
            projectId, project.Name, project.Topic, project.Introduction,
            project.ProjectImage, project.FormType, project.AiAssistance,
            project.MinAge, project.MaxAge, project.RequestContactInfo,
            project.ShortForm, ideaPageSize, extraIdeasReplaceExisting, maxExtraIdeaRequests);

        return RedirectToAction(nameof(Ideation), new { projectId, isNew, saved = 1 });
    }

    [HttpPost]
    public async Task<IActionResult> AddCategory(int projectId, bool isNew, CategoryFormViewModel model)
    {
        var projectForAdd = await projectService.GetProjectByIdAsync(projectId);
        if (IsProjectLocked(projectForAdd))
        {
            TempData["Error"] = "Dit project is actief of gearchiveerd en kan niet meer worden bewerkt.";
            return RedirectToAction(nameof(Ideation), new { projectId, isNew });
        }
        if (ModelState.IsValid)
            await categoryService.CreateCategoryAsync(projectId, model.Name, model.Description ?? string.Empty);
        ViewData["ProjectId"]     = projectId;
        ViewData["ProjectName"]   = projectForAdd?.Name ?? "";
        ViewData["PlatformName"]  = projectForAdd?.OrganisationName ?? "";
        ViewData["ProjectState"]  = projectForAdd?.ProjectState ?? "";
        ViewData["ActiveSection"] = "Categorieën";
        return RedirectToAction(nameof(Ideation), new { projectId, isNew });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int projectId, int categoryId, bool isNew)
    {
        var projectForDelete = await projectService.GetProjectByIdAsync(projectId);
        if (IsProjectLocked(projectForDelete))
        {
            TempData["Error"] = "Dit project is actief of gearchiveerd en kan niet meer worden bewerkt.";
            return RedirectToAction(nameof(Ideation), new { projectId, isNew });
        }
        await categoryService.DeleteCategoryAsync(projectId, categoryId);
        ViewData["ProjectId"]     = projectId;
        ViewData["ProjectName"]   = projectForDelete?.Name ?? "";
        ViewData["PlatformName"]  = projectForDelete?.OrganisationName ?? "";
        ViewData["ProjectState"]  = projectForDelete?.ProjectState ?? "";
        ViewData["ActiveSection"] = "Categorieën";
        return RedirectToAction(nameof(Ideation), new { projectId, isNew });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Geen bestand ontvangen." });

        try
        {
            var url = await uploadService.UploadImageAsync(file);
            return Ok(new { url });
        }
        catch (HttpRequestException)
        {
            return StatusCode(502, new { error = "Uploaden mislukt." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> CheckShortForm(string shortForm, int? projectId)
    {
        if (string.IsNullOrWhiteSpace(shortForm))
            return Json(new { available = true });

        var available = await projectService.IsShortFormAvailableAsync(shortForm, projectId);
        return Json(new { available });
    }

    [HttpPost]
    public async Task<IActionResult> CopyProject(int projectId, string newName)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.ProjectPlatformId is not int platformId)
            return Forbid();

        var source = await projectService.GetProjectByIdAsync(projectId);
        if (source is null || source.PlatformId != platformId)
        {
            TempData["Error"] = "Project niet gevonden of geen toegang.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(newName))
            newName = $"{source.Name} (kopie)";
        if (newName.Length > 100)
            newName = newName[..100];

        try
        {
            var copy = await projectService.CopyProjectAsync(projectId, newName, platformId);
            TempData["Success"] = "Project gekopieerd als sjabloon!";
            return RedirectToAction(nameof(Create), new { projectId = copy.ProjectId, isNew = true });
        }
        catch (HttpRequestException ex)
        {
            TempData["Error"] = $"Kopiëren mislukt: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteProject(int projectId)
    {
        await projectService.DeleteProjectAsync(projectId);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Activate(int projectId)
    {
        var project = await projectService.GetProjectByIdAsync(projectId);
        if (project is null)
            return NotFound();

        ViewData["ProjectId"]     = projectId;
        ViewData["ProjectName"]   = project.Name;
        ViewData["PlatformName"]  = project.OrganisationName ?? "";
        ViewData["ProjectState"]  = project.ProjectState ?? "";
        ViewData["ActiveSection"] = "Instellingen";
        return View(project);
    }

    [HttpPost]
    public async Task<IActionResult> ActivateProject(int projectId)
    {
        await projectService.UpdateProjectStateAsync(projectId, "Active");
        var projectForActivate = await projectService.GetProjectByIdAsync(projectId);
        ViewData["ProjectId"]     = projectId;
        ViewData["ProjectName"]   = projectForActivate?.Name ?? "";
        ViewData["PlatformName"]  = projectForActivate?.OrganisationName ?? "";
        ViewData["ProjectState"]  = projectForActivate?.ProjectState ?? "";
        ViewData["ActiveSection"] = "Instellingen";
        return RedirectToAction(nameof(Dashboard), new { projectId });
    }

    public async Task<IActionResult> Archive(int projectId)
    {
        var project = await projectService.GetProjectByIdAsync(projectId);
        if (project is null)
            return NotFound();
        if (project.ProjectState != "Active")
            return RedirectToAction(nameof(Dashboard), new { projectId });

        ViewData["ProjectId"]     = projectId;
        ViewData["ProjectName"]   = project.Name;
        ViewData["PlatformName"]  = project.OrganisationName ?? "";
        ViewData["ProjectState"]  = project.ProjectState ?? "";
        ViewData["ActiveSection"] = "Dashboard";
        return View(project);
    }

    [HttpPost]
    public async Task<IActionResult> ArchiveProject(int projectId)
    {
        var project = await projectService.GetProjectByIdAsync(projectId);
        if (project is null)
            return NotFound();
        if (project.ProjectState != "Active")
            return RedirectToAction(nameof(Dashboard), new { projectId });

        await projectService.UpdateProjectStateAsync(projectId, "Archived");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Dashboard(int projectId)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Auth");

        var project = await projectService.GetProjectByIdAsync(projectId);
        if (project is null) return NotFound();

        var dashboard = await dashboardService.GetDashboardAsync(projectId, project.Name);
        var layout = await widgetPreferenceService.GetLayoutAsync(user.Id, projectId);

        dashboard.WidgetLayout = layout;
        dashboard.ApiBaseUrl = $"{Request.Scheme}://{Request.Host}";
        var participantDomain = adminSettings.Value.ParticipantDomain;
        var scheme = participantDomain.StartsWith("localhost") ? "http" : "https";
        dashboard.SurveyUrl = $"{scheme}://{project.ShortForm}.{participantDomain}";

        ViewData["ProjectId"]     = projectId;
        ViewData["ProjectName"]   = project.Name;
        ViewData["PlatformName"]  = project.OrganisationName ?? "";
        ViewData["ProjectState"]  = project.ProjectState ?? "";
        ViewData["ActiveSection"] = "Dashboard";
        return View(dashboard);
    }

    public async Task<IActionResult> Ideas(
        int projectId,
        string search = null,
        int? categoryId = null,
        string status = null,
        int page = 1)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Auth");

        var project = await projectService.GetProjectByIdAsync(projectId);
        if (project is null) return NotFound();

        ViewData["ProjectId"]     = projectId;
        ViewData["ProjectName"]   = project.Name;
        ViewData["PlatformName"]  = project.OrganisationName;
        ViewData["ProjectState"]  = project.ProjectState;
        ViewData["ActiveSection"] = "Ideeën";
        ViewData["Title"]         = "Ideeën";

        var (items, total, approved, pending, flagged) =
            await ideaService.GetIdeasAsync(projectId, search, categoryId, status, page);

        var categories = await categoryService.GetCategoriesAsync(projectId);

        var vm = new IdeasViewModel
        {
            ProjectId     = projectId,
            ProjectName   = project.Name,
            Search        = search,
            CategoryId    = categoryId,
            Status        = status,
            Page          = page,
            TotalCount    = total,
            ApprovedCount = approved,
            PendingCount  = pending,
            FlaggedCount  = flagged,
            Ideas         = items,
            Categories    = categories.Select(c => new CategoryOptionViewModel
            {
                CategoryId = c.CategoryId,
                Name       = c.Name
            }).ToList()
        };

        return View(vm);
    }

    public async Task<IActionResult> ExportParticipantEmails(int projectId, string format = "csv")
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Auth");
        if (await projectService.GetProjectByIdAsync(projectId) is null) return NotFound();

        var normalized = (format ?? "csv").Trim().ToLowerInvariant();
        if (normalized is not ("csv" or "txt" or "xlsx"))
            return BadRequest("Ongeldig formaat.");

        var (bytes, contentType, fileName) = await participantService.ExportEmailsAsync(projectId, normalized);
        return File(bytes, contentType, fileName);
    }

    public async Task<IActionResult> ExportRawData(int projectId, string format = "xlsx")
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Auth");

        var project = await projectService.GetProjectByIdAsync(projectId);
        if (project is null) return NotFound();
        if (project.ProjectState is not ("Active" or "Archived"))
            return BadRequest("Export is enkel beschikbaar voor actieve of gearchiveerde projecten.");

        var normalized = (format ?? "xlsx").Trim().ToLowerInvariant();
        if (normalized is not ("csv" or "xlsx"))
            return BadRequest("Ongeldig formaat.");

        var (bytes, contentType, fileName) = await participantService.ExportRawDataAsync(projectId, normalized);
        return File(bytes, contentType, fileName);
    }

    [HttpPost]
    public async Task<IActionResult> ApproveIdea(int projectId, int ideaId, string returnUrl)
    {
        await ideaService.SetStatusAsync(projectId, ideaId, "Approved");
        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action(nameof(Ideas), new { projectId })!);
    }

    [HttpPost]
    public async Task<IActionResult> DeclineIdea(int projectId, int ideaId, string returnUrl)
    {
        await ideaService.SetStatusAsync(projectId, ideaId, "Declined");
        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action(nameof(Ideas), new { projectId })!);
    }

    [HttpPost]
    public async Task<IActionResult> FlagIdea(int projectId, int ideaId, string returnUrl)
    {
        await ideaService.SetStatusAsync(projectId, ideaId, "Flagged");
        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action(nameof(Ideas), new { projectId })!);
    }

    [HttpPost]
    public async Task<IActionResult> RemoveIdeaImage(int projectId, int ideaId, string returnUrl)
    {
        await ideaService.RemoveIdeaImageAsync(projectId, ideaId);
        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action(nameof(Ideas), new { projectId })!);
    }

    public async Task<IActionResult> ConfigureDashboard(int projectId)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Auth");

        var project = await projectService.GetProjectByIdAsync(projectId);
        if (project is null) return NotFound();

        var layout = await widgetPreferenceService.GetLayoutAsync(user.Id, projectId);
        ViewData["ProjectId"]     = projectId;
        ViewData["ProjectName"]   = project.Name;
        ViewData["PlatformName"]  = project.OrganisationName ?? "";
        ViewData["ProjectState"]  = project.ProjectState ?? "";
        ViewData["ActiveSection"] = "Dashboard";
        return View(new ConfigureDashboardViewModel
        {
            ProjectId = projectId,
            ProjectName = project.Name,
            Layout = layout,
            AllWidgetTypes = WidgetPreferenceService.AllWidgetTypes
        });
    }

    [HttpPost]
    public async Task<IActionResult> ConfigureDashboard(int projectId, ConfigureDashboardViewModel model)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Auth");

        var source = model.Layout ?? WidgetPreferenceService.DefaultLayout;
        var sanitized = Enumerable.Range(0, 9)
            .Select(i => i < source.Length && WidgetPreferenceService.AllWidgetTypes.Contains(source[i])
                ? source[i]
                : WidgetPreferenceService.DefaultLayout[i])
            .ToArray();

        await widgetPreferenceService.SaveLayoutAsync(user.Id, projectId, sanitized);
        var projectForConfigure = await projectService.GetProjectByIdAsync(projectId);
        ViewData["ProjectId"]     = projectId;
        ViewData["ProjectName"]   = projectForConfigure?.Name ?? "";
        ViewData["PlatformName"]  = projectForConfigure?.OrganisationName ?? "";
        ViewData["ProjectState"]  = projectForConfigure?.ProjectState ?? "";
        ViewData["ActiveSection"] = "Dashboard";
        return RedirectToAction(nameof(Dashboard), new { projectId });
    }

    private static bool IsProjectLocked(ProjectAdminViewModel project) =>
        project?.ProjectState is "Active" or "Archived";
}

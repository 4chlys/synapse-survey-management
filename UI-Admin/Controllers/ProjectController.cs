using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SynapseEnqueteManagement.UI.Admin.Data;
using SynapseEnqueteManagement.UI.Admin.Filters;
using SynapseEnqueteManagement.UI.Admin.Models.Category;
using SynapseEnqueteManagement.UI.Admin.Models.Enums;
using SynapseEnqueteManagement.UI.Admin.Models.Project;
using SynapseEnqueteManagement.UI.Admin.Models.Question;
using SynapseEnqueteManagement.UI.Admin.Services;
using SynapseEnqueteManagement.UI.Admin.Services.Category;
using SynapseEnqueteManagement.UI.Admin.Services.Dashboard;
using SynapseEnqueteManagement.UI.Admin.Services.Project;
using SynapseEnqueteManagement.UI.Admin.Services.Question;
using SynapseEnqueteManagement.UI.Admin.Models.Dashboard;
using SynapseEnqueteManagement.UI.Admin.Services.Platform;
using SynapseEnqueteManagement.UI.Admin.Services.Section;
using SynapseEnqueteManagement.UI.Admin.Services.Upload;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.UI.Admin.Controllers;

[Authorize(Roles = "ProjectPlatformAdmin")]
[RequireProjectPlatformOwnership]
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
    IOptions<AdminSettings> adminSettings,
    IStringLocalizer<SharedResource> localizer) : Controller
{
    public async Task<IActionResult> Index()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Auth");
        var projects = await projectService.GetAllProjectsAsync(user.ProjectPlatformId);
        ViewData["ActiveSection"] = "Projects";
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
            MinAge = project.MinAge,
            MaxAge = project.MaxAge,
            ContactOptIn = project.RequestContactInfo,
            AllowCrossLanguageIdeas = project.AllowCrossLanguageIdeas,
            DefaultLanguageCode = project.DefaultLanguageCode ?? "nl",
            SupportedLanguageCodes = project.SupportedLanguageCodes,
            Theme = project.Theme,
            PrimaryColor = project.PrimaryColor,
            CustomCss = project.CustomCss,
            Sections = sections.Select(s => s.Name).ToList()
        };
        ViewData["Project"] = project;
        ViewData["ProjectId"]     = projectId.Value;
        ViewData["ProjectName"]   = project.Name;
        ViewData["PlatformName"]  = project.OrganisationName ?? "";
        ViewData["ProjectState"]  = project.ProjectState ?? "";
        ViewData["ActiveSection"] = "Settings";
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
                ModelState.AddModelError(nameof(model.Topic), localizer["Project_TopicRequired"]);
            if (string.IsNullOrWhiteSpace(model.Introduction))
                ModelState.AddModelError(nameof(model.Introduction), localizer["Project_IntroductionRequired"]);
        }

        if (model.MinAge.HasValue && model.MaxAge.HasValue && model.MinAge > model.MaxAge)
            ModelState.AddModelError(nameof(model.MinAge), localizer["Project_AgeRange"]);

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
            ViewData["ActiveSection"] = "Settings";
            ViewData["IsNew"]         = wizardActive;
            ViewData["HideSidebar"]   = wizardActive;
            return View(model);
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Auth");
        ProjectAdminViewModel project;
        try
        {
            if (model.ProjectId.HasValue)
            {
                var lockCheck = await projectService.GetProjectByIdAsync(model.ProjectId.Value);
                if (IsProjectLocked(lockCheck))
                {
                    TempData["Error"] = localizer["Project_LockedNotEditable"].Value;
                    return RedirectToAction(nameof(Create), new { projectId = model.ProjectId.Value });
                }
                project = await projectService.UpdateProjectAsync(
                    model.ProjectId.Value, model.Name, model.Topic, model.Introduction,
                    model.ImageUrl, model.InteractionMode, model.MinAge, model.MaxAge,
                    model.ContactOptIn, model.ShortForm,
                    lockCheck.IdeaPageSize, lockCheck.ExtraIdeasReplaceExisting, lockCheck.MaxExtraIdeaRequests,
                    model.AllowCrossLanguageIdeas,
                    model.Theme, model.PrimaryColor, model.CustomCss,
                    model.DefaultLanguageCode);
            }
            else
            {
                if (user.ProjectPlatformId is null)
                {
                    ModelState.AddModelError(string.Empty, localizer["Project_NoPlatform"]);
                    return View(model);
                }
                project = await projectService.CreateProjectAsync(
                    model.Name, model.Topic, model.Introduction,
                    model.ImageUrl, model.InteractionMode,
                    model.MinAge, model.MaxAge, model.ContactOptIn,
                    platformId: user.ProjectPlatformId.Value,
                    shortForm: model.ShortForm,
                    allowCrossLanguageIdeas: model.AllowCrossLanguageIdeas,
                    theme: model.Theme,
                    primaryColor: model.PrimaryColor,
                    customCss: model.CustomCss,
                    languageCode: model.DefaultLanguageCode);
            }
        }
        catch (ApiValidationException ex)
        {
            // API rejected the payload (e.g. low-contrast primary color, disallowed
            // CustomCss). Message is already localised by the API middleware —
            // show it as an inline error on the likely culprit field AND as a toast.
            ModelState.AddModelError(nameof(model.PrimaryColor), ex.Message);
            TempData["Error"] = ex.Message;
            if (model.ProjectId.HasValue)
            {
                var existingProject = await projectService.GetProjectByIdAsync(model.ProjectId.Value);
                ViewData["ProjectId"]     = model.ProjectId.Value;
                ViewData["ProjectName"]   = existingProject?.Name ?? model.Name ?? "";
                ViewData["PlatformName"]  = existingProject?.OrganisationName ?? "";
                ViewData["ProjectState"]  = existingProject?.ProjectState ?? "";
            }
            ViewData["ActiveSection"] = "Settings";
            ViewData["IsNew"]         = wizardActive;
            ViewData["HideSidebar"]   = wizardActive;
            return View(model);
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
                await sectionService.CreateSectionAsync(project.ProjectId, name, string.Empty,
                    model.DefaultLanguageCode ?? "nl");

        if (submitAction == "next")
            return RedirectToAction(nameof(Questions),
                new { projectId = project.ProjectId, isNew = true });

        return RedirectToAction(nameof(Create), new { projectId = project.ProjectId, isNew = wizardActive, saved = 1 });
    }

    public async Task<IActionResult> Questions(int projectId, bool isNew = false)
    {
        var questions = await questionService.GetQuestionsAsync(projectId);
        var project = await projectService.GetProjectByIdAsync(projectId);
        var sections = await sectionService.GetSectionsAsync(projectId);

        var user = await userManager.GetUserAsync(User);
        var platformSettings = user?.ProjectPlatformId is not null
            ? await platformService.GetPlatformSettingsAsync(user.ProjectPlatformId.Value)
            : null;
        var aiAssistance = Enum.TryParse<AiAssistanceType>(platformSettings?.AiAssistanceMode, out var assistanceMode)
            ? assistanceMode
            : AiAssistanceType.ProActive;

        if (isNew && project?.ProjectState == "Draft")
            await projectService.UpdateWizardStepAsync(projectId, 2);

        ViewData["ProjectId"]     = projectId;
        ViewData["ProjectName"]   = project?.Name ?? "";
        ViewData["PlatformName"]  = project?.OrganisationName ?? "";
        ViewData["ProjectState"]  = project?.ProjectState ?? "";
        ViewData["ActiveSection"] = "Questions";
        ViewData["AiAssistance"]  = aiAssistance.ToString();
        ViewData["Project"]       = project;
        ViewData["Sections"]      = sections;
        ViewData["IsNew"]         = isNew;
        ViewData["HideSidebar"]   = isNew;
        return View(questions);
    }

    [HttpPost]
    public async Task<IActionResult> SaveProgress(int projectId, bool isNew, int step, string returnAction)
    {
        if (returnAction != nameof(Questions) && returnAction != nameof(Ideation))
            return BadRequest();

        await projectService.UpdateWizardStepAsync(projectId, step);
        TempData["Success"] = localizer["Common_ProgressSaved"].Value;
        return RedirectToAction(returnAction, new { projectId, isNew });
    }

    [HttpPost]
    public async Task<IActionResult> CreateQuestion(int projectId,
        bool isNew, CreateQuestionViewModel model)
    {
        var projectForCreate = await projectService.GetProjectByIdAsync(projectId);
        if (IsProjectLocked(projectForCreate))
        {
            TempData["Error"] = localizer["Project_LockedNotEditable"].Value;
            return RedirectToAction(nameof(Questions), new { projectId, isNew });
        }
        if (ModelState.IsValid)
            await questionService.CreateQuestionAsync(projectId, model.Text, model.QuestionFormat.ToString(),
                model.IsMandatory, model.GetAnswerChoices(), model.SectionId,
                parentQuestionId: model.ParentQuestionId, choiceIndex: model.ChoiceIndex,
                mediaUrl: model.MediaUrl, mediaType: model.MediaType,
                mediaIsEmbed: model.MediaIsEmbed, mediaText: model.MediaText);
        return RedirectToAction(nameof(Questions), new { projectId, isNew, saved = 1 });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateQuestion(int projectId,
        bool isNew, EditQuestionViewModel model)
    {
        var projectForUpdate = await projectService.GetProjectByIdAsync(projectId);
        if (IsProjectLocked(projectForUpdate))
        {
            TempData["Error"] = localizer["Project_LockedNotEditable"].Value;
            return RedirectToAction(nameof(Questions), new { projectId, isNew });
        }
        if (ModelState.IsValid)
            await questionService.UpdateQuestionAsync(projectId, model.QuestionId, model.Text, model.QuestionFormat.ToString(),
                model.IsMandatory, model.GetAnswerChoices(), model.SectionId,
                mediaUrl: model.MediaUrl, mediaType: model.MediaType,
                mediaIsEmbed: model.MediaIsEmbed, mediaText: model.MediaText);
        return RedirectToAction(nameof(Questions), new { projectId, isNew, saved = 1 });
    }

    public async Task<IActionResult> Ideation(int projectId, bool isNew = false)
    {
        var project = await projectService.GetProjectByIdAsync(projectId);
        var categories = await categoryService.GetCategoriesAsync(projectId);

        if (isNew && project?.ProjectState == "Draft")
            await projectService.UpdateWizardStepAsync(projectId, 3);

        ViewData["ProjectId"]     = projectId;
        ViewData["ProjectName"]   = project?.Name ?? "";
        ViewData["PlatformName"]  = project?.OrganisationName ?? "";
        ViewData["ProjectState"]  = project?.ProjectState ?? "";
        ViewData["ActiveSection"] = "Categories";
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
            TempData["Error"] = localizer["Project_LockedNotEditable"].Value;
            return RedirectToAction(nameof(Ideation), new { projectId, isNew });
        }

        await projectService.UpdateProjectAsync(
            projectId, project.Name, project.Topic, project.Introduction,
            project.ProjectImage, project.FormType,
            project.MinAge, project.MaxAge, project.RequestContactInfo,
            project.ShortForm, ideaPageSize, extraIdeasReplaceExisting, maxExtraIdeaRequests,
            project.AllowCrossLanguageIdeas,
            project.Theme, project.PrimaryColor, project.CustomCss,
            project.DefaultLanguageCode ?? "nl");

        return RedirectToAction(nameof(Ideation), new { projectId, isNew, saved = 1 });
    }

    [HttpPost]
    public async Task<IActionResult> AddCategory(int projectId, bool isNew, CategoryFormViewModel model)
    {
        var projectForAdd = await projectService.GetProjectByIdAsync(projectId);
        if (IsProjectLocked(projectForAdd))
        {
            TempData["Error"] = localizer["Project_LockedNotEditable"].Value;
            return RedirectToAction(nameof(Ideation), new { projectId, isNew });
        }
        if (ModelState.IsValid)
            await categoryService.CreateCategoryAsync(projectId, model.Name, model.Description ?? string.Empty);
        ViewData["ProjectId"]     = projectId;
        ViewData["ProjectName"]   = projectForAdd?.Name ?? "";
        ViewData["PlatformName"]  = projectForAdd?.OrganisationName ?? "";
        ViewData["ProjectState"]  = projectForAdd?.ProjectState ?? "";
        ViewData["ActiveSection"] = "Categories";
        return RedirectToAction(nameof(Ideation), new { projectId, isNew });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int projectId, int categoryId, bool isNew)
    {
        var projectForDelete = await projectService.GetProjectByIdAsync(projectId);
        if (IsProjectLocked(projectForDelete))
        {
            TempData["Error"] = localizer["Project_LockedNotEditable"].Value;
            return RedirectToAction(nameof(Ideation), new { projectId, isNew });
        }
        await categoryService.DeleteCategoryAsync(projectId, categoryId);
        ViewData["ProjectId"]     = projectId;
        ViewData["ProjectName"]   = projectForDelete?.Name ?? "";
        ViewData["PlatformName"]  = projectForDelete?.OrganisationName ?? "";
        ViewData["ProjectState"]  = projectForDelete?.ProjectState ?? "";
        ViewData["ActiveSection"] = "Categories";
        return RedirectToAction(nameof(Ideation), new { projectId, isNew });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    [EnableRateLimiting("ai-image-generation")]
    public async Task<IActionResult> GenerateImage([FromBody] GenerateImageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Name))
            return BadRequest(new { error = localizer["Project_NameRequired"].Value });

        try
        {
            var url = await uploadService.GenerateImageAsync(request.Name, request.Topic, request.Intro);
            return Ok(new { url });
        }
        catch (HttpRequestException)
        {
            return StatusCode(502, new { error = localizer["Project_ImageGenerationFailed"].Value });
        }
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = localizer["Project_NoFileReceived"].Value });

        try
        {
            var url = await uploadService.UploadImageAsync(file);
            return Ok(new { url });
        }
        catch (HttpRequestException)
        {
            return StatusCode(502, new { error = localizer["Project_UploadFailed"].Value });
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
        if (source is null)
        {
            TempData["Error"] = localizer["Project_NotFoundOrNoAccess"].Value;
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(newName))
            newName = $"{source.Name} (kopie)";
        if (newName.Length > 100)
            newName = newName[..100];

        try
        {
            var copy = await projectService.CopyProjectAsync(projectId, newName, platformId, languageCode: source.DefaultLanguageCode ?? "nl");
            TempData["Success"] = localizer["Project_CopySuccess"].Value;
            return RedirectToAction(nameof(Create), new { projectId = copy.ProjectId, isNew = true });
        }
        catch (HttpRequestException ex)
        {
            TempData["Error"] = string.Format(localizer["Project_CopyFailed"].Value, ex.Message);
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteProject(int projectId)
    {
        var project = await projectService.GetProjectByIdAsync(projectId);
        if (project?.ProjectState == "Active")
        {
            TempData["Error"] = localizer["Project_DeleteActiveBlocked"].Value;
            return RedirectToAction(nameof(Index));
        }
        await projectService.DeleteProjectAsync(projectId);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Activate(int projectId)
    {
        var project = await projectService.GetProjectByIdAsync(projectId);
        if (project is null)
            return NotFound();

        var questions  = await questionService.GetQuestionsAsync(projectId);
        var categories = await categoryService.GetCategoriesAsync(projectId);

        ViewData["ProjectId"]     = projectId;
        ViewData["ProjectName"]   = project.Name;
        ViewData["PlatformName"]  = project.OrganisationName ?? "";
        ViewData["ProjectState"]  = project.ProjectState ?? "";
        ViewData["ActiveSection"] = "Settings";
        ViewData["QuestionCount"] = questions.Count;
        ViewData["CategoryCount"] = categories.Count;
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
        ViewData["ActiveSection"] = "Settings";
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
        var slots = await widgetPreferenceService.GetSlotsAsync(user.Id, projectId);

        var participantDomain = adminSettings.Value.ParticipantDomain;
        var scheme = participantDomain.StartsWith("localhost") ? "http" : "https";
        dashboard = dashboard with
        {
            WidgetSlots = slots,
            WidgetLayout = Array.ConvertAll(slots, s => s.WidgetType),
            ApiBaseUrl = $"{Request.Scheme}://{Request.Host}",
            SurveyUrl = $"{scheme}://{project.ShortForm}.{participantDomain}"
        };

        ViewData["ProjectId"]     = projectId;
        ViewData["ProjectName"]   = project.Name;
        ViewData["PlatformName"]  = project.OrganisationName ?? "";
        ViewData["ProjectState"]  = project.ProjectState ?? "";
        ViewData["ActiveSection"] = "Dashboard";
        return View(dashboard);
    }

    public async Task<IActionResult> IdeasCategories(int projectId)
    {
        var project = await projectService.GetProjectByIdAsync(projectId);
        if (project is null) return NotFound();

        ViewData["ProjectId"]     = projectId;
        ViewData["ProjectName"]   = project.Name;
        ViewData["PlatformName"]  = project.OrganisationName;
        ViewData["ProjectState"]  = project.ProjectState;
        ViewData["ActiveSection"] = "Ideas";

        var categories = await categoryService.GetCategoriesAsync(projectId);

        var (_, total, approved, pending, flagged) =
            await ideaService.GetIdeasAsync(projectId, null, null, null, 1, 1);

        var vm = new IdeasCategoriesViewModel
        {
            ProjectId      = projectId,
            ProjectName    = project.Name,
            TotalIdeaCount = total,
            TotalApproved  = approved,
            TotalPending   = pending,
            TotalFlagged   = flagged,
            TotalDeclined  = Math.Max(0, total - approved - pending - flagged),
            Categories     = categories
        };

        return View(vm);
    }

    public async Task<IActionResult> Ideas(
        int projectId,
        int categoryId,
        string search = null,
        string status = null,
        int page = 1)
    {
        var project = await projectService.GetProjectByIdAsync(projectId);
        if (project is null) return NotFound();

        ViewData["ProjectId"]     = projectId;
        ViewData["ProjectName"]   = project.Name;
        ViewData["PlatformName"]  = project.OrganisationName;
        ViewData["ProjectState"]  = project.ProjectState;
        ViewData["ActiveSection"] = "Ideas";

        var (items, total, approved, pending, flagged) =
            await ideaService.GetIdeasAsync(projectId, search, categoryId, status, page);

        var categories = await categoryService.GetCategoriesAsync(projectId);
        var currentCategory = categories.FirstOrDefault(c => c.CategoryId == categoryId);

        var vm = new IdeasViewModel
        {
            ProjectId     = projectId,
            ProjectName   = project.Name,
            Search        = search,
            CategoryId    = categoryId,
            CategoryName  = currentCategory?.Name ?? string.Empty,
            Status        = status,
            Page          = page,
            TotalCount    = total,
            ApprovedCount = approved,
            PendingCount  = pending,
            FlaggedCount  = flagged,
            DeclinedCount = Math.Max(0, total - approved - pending - flagged),
            Ideas         = items,
            Categories    = categories.Select(c => new CategoryOptionViewModel
            {
                CategoryId = c.CategoryId,
                Name       = c.Name
            }).ToList()
        };

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return PartialView("_IdeasTable", vm);

        return View(vm);
    }

    public async Task<IActionResult> IdeaDetail(
        int projectId, int ideaId,
        int categoryId = 0, string categoryName = null,
        string returnUrl = null)
    {
        var project = await projectService.GetProjectByIdAsync(projectId);
        if (project is null) return NotFound();

        ViewData["ProjectId"]     = projectId;
        ViewData["ProjectName"]   = project.Name;
        ViewData["PlatformName"]  = project.OrganisationName;
        ViewData["ProjectState"]  = project.ProjectState;
        ViewData["ActiveSection"] = "Ideas";

        var vm = await ideaService.GetIdeaDetailAsync(projectId, ideaId);
        if (vm is null) return NotFound();

        return View(vm with
        {
            CategoryId   = categoryId,
            CategoryName = categoryName ?? string.Empty,
            BackUrl      = returnUrl ?? Url.Action(nameof(IdeasCategories), new { projectId })!
        });
    }

    public async Task<IActionResult> ExportParticipantEmails(int projectId, string format = "csv")
    {
        if (await projectService.GetProjectByIdAsync(projectId) is null) return NotFound();

        var normalized = (format ?? "csv").Trim().ToLowerInvariant();
        if (normalized is not ("csv" or "txt" or "xlsx"))
            return BadRequest(localizer["Project_InvalidFormat"].Value);

        var (bytes, contentType, fileName) = await participantService.ExportEmailsAsync(projectId, normalized);
        return File(bytes, contentType, fileName);
    }

    public async Task<IActionResult> ExportRawData(int projectId, string format = "xlsx", string tables = "")
    {
        var project = await projectService.GetProjectByIdAsync(projectId);
        if (project is null) return NotFound();
        if (project.ProjectState is not ("Active" or "Archived"))
            return BadRequest(localizer["Project_ExportOnlyActive"].Value);

        var normalized = (format ?? "xlsx").Trim().ToLowerInvariant();
        if (normalized is not ("csv" or "xlsx"))
            return BadRequest(localizer["Project_InvalidFormat"].Value);

        var (bytes, contentType, fileName) = await participantService.ExportRawDataAsync(projectId, normalized, tables);
        return File(bytes, contentType, fileName);
    }

    [HttpPost]
    public async Task<IActionResult> ApproveIdea(int projectId, int ideaId, string returnUrl)
    {
        await ideaService.SetStatusAsync(projectId, ideaId, "Approved");
        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action(nameof(IdeasCategories), new { projectId })!);
    }

    [HttpPost]
    public async Task<IActionResult> DeclineIdea(int projectId, int ideaId, string returnUrl)
    {
        await ideaService.SetStatusAsync(projectId, ideaId, "Declined");
        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action(nameof(IdeasCategories), new { projectId })!);
    }

    [HttpPost]
    public async Task<IActionResult> FlagIdea(int projectId, int ideaId, string returnUrl)
    {
        await ideaService.SetStatusAsync(projectId, ideaId, "Flagged");
        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action(nameof(IdeasCategories), new { projectId })!);
    }

    [HttpPost]
    public async Task<IActionResult> RemoveIdeaImage(int projectId, int ideaId, string returnUrl)
    {
        await ideaService.RemoveIdeaImageAsync(projectId, ideaId);
        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action(nameof(IdeasCategories), new { projectId })!);
    }

    [HttpPost]
    public async Task<IActionResult> ApproveComment(int projectId, int ideaId, int commentId, string returnUrl)
    {
        await ideaService.SetCommentStatusAsync(projectId, ideaId, commentId, "Approved");
        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action(nameof(IdeaDetail), new { projectId, ideaId })!);
    }

    [HttpPost]
    public async Task<IActionResult> DeclineComment(int projectId, int ideaId, int commentId, string returnUrl)
    {
        await ideaService.SetCommentStatusAsync(projectId, ideaId, commentId, "Declined");
        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action(nameof(IdeaDetail), new { projectId, ideaId })!);
    }

    [HttpPost]
    public async Task<IActionResult> FlagComment(int projectId, int ideaId, int commentId, string returnUrl)
    {
        await ideaService.SetCommentStatusAsync(projectId, ideaId, commentId, "Flagged");
        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action(nameof(IdeaDetail), new { projectId, ideaId })!);
    }

    public async Task<IActionResult> ConfigureDashboard(int projectId)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Auth");

        var project = await projectService.GetProjectByIdAsync(projectId);
        if (project is null) return NotFound();

        var slots = await widgetPreferenceService.GetSlotsAsync(user.Id, projectId);
        ViewData["ProjectId"]     = projectId;
        ViewData["ProjectName"]   = project.Name;
        ViewData["PlatformName"]  = project.OrganisationName ?? "";
        ViewData["ProjectState"]  = project.ProjectState ?? "";
        ViewData["ActiveSection"] = "Dashboard";
        return View(new ConfigureDashboardViewModel
        {
            ProjectId = projectId,
            ProjectName = project.Name,
            Layout = Array.ConvertAll(slots, s => s.WidgetType),
            Slots = Array.ConvertAll(slots, s => new WidgetSlotInput { WidgetType = s.WidgetType, QuestionId = s.QuestionId }),
            AllWidgetTypes = WidgetPreferenceService.AllWidgetTypes
        });
    }

    [HttpPost]
    public async Task<IActionResult> ConfigureDashboard(int projectId, ConfigureDashboardViewModel model)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Auth");

        var source = model.Slots ?? [];
        var sanitized = Enumerable.Range(0, 9)
            .Select(i =>
            {
                var input = i < source.Length ? source[i] : null;
                var type = input is not null && WidgetPreferenceService.AllWidgetTypes.Contains(input.WidgetType)
                    ? input.WidgetType
                    : WidgetPreferenceService.DefaultLayout[i];
                var isQuestionBound = type is "QuestionSinglePie" or "QuestionMultiBar" or "QuestionLikertBar";
                return new WidgetSlot(type, isQuestionBound ? input?.QuestionId : null);
            })
            .ToArray();

        await widgetPreferenceService.SaveSlotsAsync(user.Id, projectId, sanitized);
        var projectForConfigure = await projectService.GetProjectByIdAsync(projectId);
        ViewData["ProjectId"]     = projectId;
        ViewData["ProjectName"]   = projectForConfigure?.Name ?? "";
        ViewData["PlatformName"]  = projectForConfigure?.OrganisationName ?? "";
        ViewData["ProjectState"]  = projectForConfigure?.ProjectState ?? "";
        ViewData["ActiveSection"] = "Dashboard";
        return RedirectToAction(nameof(Dashboard), new { projectId });
    }

    [HttpPost]
    public async Task<IActionResult> SetWidgetQuestion(int projectId, int position, int? questionId)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        await widgetPreferenceService.UpdateSlotQuestionAsync(user.Id, projectId, position, questionId);
        return Ok();
    }

    private static bool IsProjectLocked(ProjectAdminViewModel project) =>
        project?.ProjectState is "Active" or "Archived";
}

// Request body for ProjectController.GenerateImage.
// [FromBody] avoids query-string length limits: the intro field can hold
// up to 5000 chars of Quill HTML which, after URI-encoding, exceeds the ~8 KB
// limit enforced by many proxies and servers.
public sealed record GenerateImageRequest(string Name, string Topic, string Intro);

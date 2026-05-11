using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.Admin.Models.Platform;
using SynapseEnqueteManagement.UI.Admin.Services.Platform;
using SynapseEnqueteManagement.UI.Admin.Services.Project;

namespace SynapseEnqueteManagement.UI.Admin.Controllers;

[Authorize(Roles = "SystemAdmin")]
public class PlatformController(
    PlatformAdminService platformService,
    ProjectAdminService projectService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var platforms = await platformService.GetAllPlatformsAsync();
        return View(platforms.Select(p => new PlatformViewModel
        {
            ProjectPlatformId = p.ProjectPlatformId,
            Name = p.Name,
            LogoUrl = p.LogoUrl,
            WebsiteUrl = p.WebsiteUrl
        }).ToList());
    }

    public IActionResult Create() => View(new PlatformFormViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(PlatformFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var platform = await platformService.CreatePlatformAsync(
            model.Name,
            model.LogoUrl ?? string.Empty,
            model.WebsiteUrl ?? string.Empty);

        return RedirectToAction(nameof(Dashboard), new { id = platform.ProjectPlatformId });
    }

    public async Task<IActionResult> SystemDashboard()
    {
        var platforms = await platformService.GetAllPlatformsAsync();
        var allAdmins = await platformService.GetAllAdminsAsync();

        var summaries = new List<PlatformSummaryViewModel>();
        foreach (var platform in platforms)
        {
            var projects = await projectService.GetAllProjectsAsync(platform.ProjectPlatformId);
            var settings = await platformService.GetPlatformSettingsAsync(platform.ProjectPlatformId);
            var adminCount = allAdmins.Count(a => a.ProjectPlatformId == platform.ProjectPlatformId);

            summaries.Add(new PlatformSummaryViewModel(
                platform.ProjectPlatformId,
                platform.Name,
                platform.LogoUrl,
                adminCount,
                projects.Count(p => p.ProjectState == "Active"),
                projects.Count(p => p.ProjectState == "Draft"),
                settings?.AiModelName ?? string.Empty,
                settings?.CostDashboardUrl ?? string.Empty
            ));
        }

        ViewData["ActiveSection"] = "Dashboard";
        return View(new SystemDashboardViewModel(summaries));
    }

    public async Task<IActionResult> Dashboard(int id)
    {
        var platform = await platformService.GetPlatformByIdAsync(id);
        if (platform is null) return NotFound();

        var admins = await platformService.GetAdminsForPlatformAsync(id);
        var projects = await projectService.GetAllProjectsAsync(id);
        var settings = await platformService.GetPlatformSettingsAsync(id);

        ViewData["PlatformId"] = id;
        ViewData["PlatformName"] = platform.Name;
        ViewData["ActiveSection"] = "Dashboard";

        return View(new PlatformDashboardViewModel(
            platform.ProjectPlatformId,
            platform.Name,
            platform.LogoUrl,
            platform.WebsiteUrl,
            platform.CreatedAt,
            admins.Count,
            projects.Count(p => p.ProjectState == "Active"),
            projects.Count(p => p.ProjectState == "Draft"),
            settings?.AiModelName ?? string.Empty,
            settings?.CostDashboardUrl ?? string.Empty
        ));
    }

    public async Task<IActionResult> Admins(int id)
    {
        var platform = await platformService.GetPlatformByIdAsync(id);
        if (platform is null) return NotFound();

        ViewData["PlatformId"] = id;
        ViewData["PlatformName"] = platform.Name;
        ViewData["ActiveSection"] = "Beheerders";

        return View(await BuildAdminsViewModel(id, platform.Name));
    }

    [HttpPost]
    public async Task<IActionResult> AddAdmin(int id, AddAdminViewModel model)
    {
        var platform = await platformService.GetPlatformByIdAsync(id);
        if (platform is null) return NotFound();

        ViewData["PlatformId"] = id;
        ViewData["PlatformName"] = platform.Name;
        ViewData["ActiveSection"] = "Beheerders";

        if (!ModelState.IsValid)
            return View("Admins", await BuildAdminsViewModel(id, platform.Name));

        var result = await platformService.AddAdminAsync(id, model.Email, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View("Admins", await BuildAdminsViewModel(id, platform.Name));
        }

        TempData["Success"] = "Beheerder toegevoegd.";
        return RedirectToAction(nameof(Admins), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveAdmin(int id, string userId)
    {
        await platformService.RemoveAdminAsync(id, userId);
        TempData["Success"] = "Beheerder verwijderd.";
        return RedirectToAction(nameof(Admins), new { id });
    }

    public async Task<IActionResult> Settings(int id)
    {
        var platform = await platformService.GetPlatformByIdAsync(id);
        if (platform is null) return NotFound();

        ViewData["PlatformId"] = id;
        ViewData["PlatformName"] = platform.Name;
        ViewData["ActiveSection"] = "Instellingen";

        return View(await BuildSettingsViewModel(id, platform));
    }

    [HttpPost]
    public async Task<IActionResult> SaveBasicInfo(int id, PlatformFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var platform = await platformService.GetPlatformByIdAsync(id);
            ViewData["PlatformId"] = id;
            ViewData["PlatformName"] = platform?.Name;
            ViewData["ActiveSection"] = "Instellingen";
            return View("Settings", await BuildSettingsViewModel(id, platform!));
        }

        await platformService.UpdatePlatformAsync(
            id,
            model.Name,
            model.LogoUrl ?? string.Empty,
            model.WebsiteUrl ?? string.Empty);

        TempData["Success"] = "Basisinformatie opgeslagen.";
        return RedirectToAction(nameof(Settings), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> SaveConfiguration(int id, PlatformConfigurationViewModel model)
    {
        model.ProjectPlatformId = id;
        await platformService.SavePlatformSettingsAsync(id, model);
        TempData["Success"] = "Instellingen opgeslagen.";
        return RedirectToAction(nameof(Settings), new { id });
    }

    private async Task<PlatformAdminsViewModel> BuildAdminsViewModel(int id, string platformName)
    {
        var admins = await platformService.GetAdminsForPlatformAsync(id);
        return new PlatformAdminsViewModel(
            id,
            platformName,
            admins.Select(a => new AdminSummaryViewModel
            {
                UserId = a.Id,
                UserName = a.UserName ?? a.Email ?? string.Empty,
                Email = a.Email ?? string.Empty
            }).ToList()
        );
    }

    private async Task<PlatformSettingsViewModel> BuildSettingsViewModel(
        int id, SynapseEnqueteManagement.BL.Domain.Platform.Entities.ProjectPlatform platform)
    {
        var settings = await platformService.GetPlatformSettingsAsync(id);
        var basicInfo = new PlatformFormViewModel
        {
            ProjectPlatformId = platform.ProjectPlatformId,
            Name = platform.Name,
            LogoUrl = platform.LogoUrl,
            WebsiteUrl = platform.WebsiteUrl
        };
        var config = settings is not null
            ? new PlatformConfigurationViewModel
            {
                ProjectPlatformId = id,
                ContactName = settings.ContactName,
                ContactEmail = settings.ContactEmail,
                BillingReference = settings.BillingReference,
                AiAssistanceEnabled = settings.AiAssistanceEnabled,
                IdeationEnabled = settings.IdeationEnabled,
                MaxProjects = settings.MaxProjects,
                MaxAdmins = settings.MaxAdmins,
                AiModelName = settings.AiModelName,
                CostDashboardUrl = settings.CostDashboardUrl
            }
            : new PlatformConfigurationViewModel
            {
                ProjectPlatformId = id,
                AiAssistanceEnabled = true,
                IdeationEnabled = true
            };
        return new PlatformSettingsViewModel(id, platform.Name, basicInfo, config);
    }
}
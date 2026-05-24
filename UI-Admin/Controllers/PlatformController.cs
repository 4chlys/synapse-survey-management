using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using SynapseEnqueteManagement.BL.Domain.Ai;
using SynapseEnqueteManagement.UI.Admin.Data;
using SynapseEnqueteManagement.UI.Admin.Models.Platform;
using SynapseEnqueteManagement.UI.Admin.Services.Ai;
using SynapseEnqueteManagement.UI.Admin.Services.Platform;
using SynapseEnqueteManagement.UI.Admin.Services.Project;

namespace SynapseEnqueteManagement.UI.Admin.Controllers;

[Authorize(Roles = "SystemAdmin")]
public class PlatformController(
    PlatformAdminService platformService,
    ProjectAdminService projectService,
    AiAdminService aiAdminService,
    SystemStatsAdminService systemStatsAdminService,
    UserManager<ApplicationUser> userManager,
    IStringLocalizer<SharedResource> localizer) : Controller
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
        var stats = await systemStatsAdminService.GetSystemStatsAsync(null);
        var platforms = await platformService.GetAllPlatformsAsync();
        var allAdmins = await platformService.GetAllAdminsAsync();

        var summaries = new List<PlatformSummaryViewModel>();
        foreach (var platform in platforms)
        {
            var platformStats = stats.Platforms.FirstOrDefault(p => p.PlatformId == platform.ProjectPlatformId);
            var settings = await platformService.GetPlatformSettingsAsync(platform.ProjectPlatformId);
            var adminCount = allAdmins.Count(a => a.ProjectPlatformId == platform.ProjectPlatformId);

            summaries.Add(new PlatformSummaryViewModel(
                platform.ProjectPlatformId,
                platform.Name,
                platform.LogoUrl,
                adminCount,
                platformStats?.ActiveCount ?? 0,
                platformStats?.DraftCount ?? 0,
                platformStats?.ArchivedCount ?? 0,
                platformStats?.ParticipantCount ?? 0,
                settings?.AiModelName ?? string.Empty,
                settings?.CostDashboardUrl ?? string.Empty
            ));
        }

        ViewData["ActiveSection"] = "Dashboard";
        return View(new SystemDashboardViewModel(summaries)
        {
            AvgCompletionSeconds = stats.AvgCompletionSeconds,
            MedianCompletionSeconds = stats.MedianCompletionSeconds,
            Projects = stats.Projects.Select(p => new ProjectRowViewModel
            {
                ProjectId = p.ProjectId,
                ProjectName = p.ProjectName,
                PlatformName = p.PlatformName,
                State = p.State,
                ParticipantCount = p.ParticipantCount,
                AvgCompletionSeconds = p.AvgCompletionSeconds,
                MedianCompletionSeconds = p.MedianCompletionSeconds
            }).ToList(),
            AiUsageBySystem = stats.AiUsageBySystem.Select(a => new ServiceUsageRowViewModel
            {
                Service = a.Service,
                Model = a.Model,
                InputTokens = a.InputTokens,
                OutputTokens = a.OutputTokens,
                RequestCount = a.RequestCount,
                CostEur = a.CostEur
            }).ToList(),
            AiUsageByPlatform = stats.AiUsageByPlatform.Select(a => new PlatformServiceUsageRowViewModel
            {
                PlatformId = a.ProjectPlatformId,
                PlatformName = a.ProjectPlatformName,
                Service = a.Service,
                Model = a.Model,
                InputTokens = a.InputTokens,
                OutputTokens = a.OutputTokens,
                RequestCount = a.RequestCount,
                CostEur = a.CostEur
            }).ToList()
        });
    }

    public async Task<IActionResult> SystemSettings()
    {
        var settings     = await platformService.GetSystemAiSettingsAsync();
        var allowedIds   = settings is not null
            ? JsonSerializer.Deserialize<List<string>>(settings.AllowedModelIds) ?? []
            : [];
        var availableIds = await aiAdminService.GetAvailableModelIdsAsync();

        ViewData["ActiveSection"] = "SystemSettings";
        return View(new SystemAiSettingsViewModel(
            AllModels: AiModelCatalog.All,
            AllowedModelIds: allowedIds,
            AvailableModelIds: availableIds
        ));
    }

    [HttpPost]
    public async Task<IActionResult> SaveSystemAiSettings(List<string> AllowedModelIds)
    {
        if (AllowedModelIds is null || AllowedModelIds.Count == 0)
        {
            TempData["Error"] = localizer["Platform_AiModelRequired"].Value;
            return RedirectToAction(nameof(SystemSettings));
        }

        var available = await aiAdminService.GetAvailableModelIdsAsync();
        AllowedModelIds = AllowedModelIds.Where(available.Contains).ToList();

        if (AllowedModelIds.Count == 0)
        {
            TempData["Error"] = localizer["Platform_AiModelConfiguredRequired"].Value;
            return RedirectToAction(nameof(SystemSettings));
        }

        await platformService.SaveSystemAiSettingsAsync(AllowedModelIds);
        TempData["Success"] = localizer["Platform_AiSettingsSaved"].Value;
        return RedirectToAction(nameof(SystemSettings));
    }

    [Authorize(Roles = "SystemAdmin,ProjectPlatformAdmin")]
    public async Task<IActionResult> Dashboard(int id)
    {
        if (!User.IsInRole("SystemAdmin"))
        {
            var user = await userManager.GetUserAsync(User);
            if (user?.ProjectPlatformId != id)
                return Forbid();
        }

        var platform = await platformService.GetPlatformByIdAsync(id);
        if (platform is null) return NotFound();

        var admins = await platformService.GetAdminsForPlatformAsync(id);
        var stats = await projectService.GetPlatformStatsAsync(id);
        var settings = await platformService.GetPlatformSettingsAsync(id);
        var systemStats = await systemStatsAdminService.GetSystemStatsAsync(id);

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
            stats.ActiveCount,
            stats.DraftCount,
            stats.ArchivedCount,
            stats.ParticipantCount,
            settings?.AiModelName ?? string.Empty,
            settings?.CostDashboardUrl ?? string.Empty
        )
        {
            Projects = systemStats.Projects.Select(p => new ProjectRowViewModel
            {
                ProjectId = p.ProjectId,
                ProjectName = p.ProjectName,
                PlatformName = p.PlatformName,
                State = p.State,
                ParticipantCount = p.ParticipantCount,
                AvgCompletionSeconds = p.AvgCompletionSeconds,
                MedianCompletionSeconds = p.MedianCompletionSeconds
            }).ToList()
        });
    }

    public async Task<IActionResult> Settings(int id)
    {
        var platform = await platformService.GetPlatformByIdAsync(id);
        if (platform is null) return NotFound();

        ViewData["PlatformId"] = id;
        ViewData["PlatformName"] = platform.Name;
        ViewData["ActiveSection"] = "Settings";

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
            ViewData["ActiveSection"] = "Settings";
            return View("Settings", await BuildSettingsViewModel(id, platform!));
        }

        await platformService.UpdatePlatformAsync(
            id,
            model.Name,
            model.LogoUrl ?? string.Empty,
            model.WebsiteUrl ?? string.Empty);

        TempData["Success"] = localizer["Platform_BasicInfoSaved"].Value;
        return RedirectToAction(nameof(Settings), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> SaveConfiguration(int id, PlatformConfigurationViewModel model)
    {
        model = model with { ProjectPlatformId = id };
        await platformService.SavePlatformSettingsAsync(id, model);
        TempData["Success"] = localizer["Platform_SettingsSaved"].Value;
        return RedirectToAction(nameof(Settings), new { id });
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
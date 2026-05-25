using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using SynapseEnqueteManagement.BL.Ai;
using SynapseEnqueteManagement.BL.Domain.Ai;
using SynapseEnqueteManagement.UI.Admin.Data;
using SynapseEnqueteManagement.UI.Admin.Models.Settings;
using SynapseEnqueteManagement.UI.Admin.Services.Ai;
using SynapseEnqueteManagement.UI.Admin.Services.Platform;
using AiAssistanceType = SynapseEnqueteManagement.UI.Admin.Models.Enums.AiAssistanceType;

namespace SynapseEnqueteManagement.UI.Admin.Controllers;

[Authorize(Roles = "ProjectPlatformAdmin")]
public class SettingsController(
    PlatformAdminService platformService,
    UserManager<ApplicationUser> userManager,
    AiConfig aiConfig,
    AiAdminService aiAdminService,
    IStringLocalizer<SharedResource> localizer) : Controller
{
    public async Task<IActionResult> Index()
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.ProjectPlatformId is null) return Forbid();

        var platformId = user.ProjectPlatformId.Value;
        var platform   = await platformService.GetPlatformByIdAsync(platformId);
        if (platform is null) return NotFound();

        ViewData["ActiveSection"] = "Settings";
        return View(await BuildViewModel(platformId, platform.Name));
    }

    [HttpPost]
    public async Task<IActionResult> SaveBasicInfo(PlatformAdminBasicInfoViewModel model)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.ProjectPlatformId is null) return Forbid();

        var platformId = user.ProjectPlatformId.Value;
        var settings   = await platformService.GetPlatformSettingsAsync(platformId);

        await platformService.SavePlatformSettingsAsync(platformId, new Models.Platform.PlatformConfigurationViewModel
        {
            ProjectPlatformId   = platformId,
            ContactName         = model.ContactName,
            ContactEmail        = model.ContactEmail,
            BillingReference    = model.BillingReference,
            AiAssistanceEnabled = settings?.AiAssistanceEnabled ?? true,
            IdeationEnabled     = settings?.IdeationEnabled ?? true,
            MaxProjects         = settings?.MaxProjects,
            MaxAdmins           = settings?.MaxAdmins,
            AiModelName         = settings?.AiModelName,
            CostDashboardUrl    = settings?.CostDashboardUrl,
        });

        TempData["Success"] = localizer["Platform_BasicInfoSaved"].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SaveAiAssistanceMode(string aiAssistanceMode)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.ProjectPlatformId is null) return Forbid();

        if (!Enum.TryParse<AiAssistanceType>(aiAssistanceMode, out var parsed))
        {
            TempData["Error"] = localizer["Settings_InvalidAiMode"].Value;
            return RedirectToAction(nameof(Index));
        }

        await platformService.SaveAiAssistanceModeAsync(user.ProjectPlatformId.Value, parsed.ToString());
        TempData["Success"] = localizer["Settings_AiModeSaved"].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SaveAiModel(string selectedModelId)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.ProjectPlatformId is null) return Forbid();

        var allowedIds = await GetAllowedModelIdsAsync();
        if (!allowedIds.Contains(selectedModelId))
        {
            TempData["Error"] = localizer["Settings_InvalidModel"].Value;
            return RedirectToAction(nameof(Index));
        }

        await platformService.SaveSelectedAiModelAsync(user.ProjectPlatformId.Value, selectedModelId);
        TempData["Success"] = localizer["Settings_AiModelSaved"].Value;
        return RedirectToAction(nameof(Index));
    }

    private const int MaxPromptLength = 4000;

    [HttpPost]
    public async Task<IActionResult> SavePromptOverride(string promptType, string value)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.ProjectPlatformId is null) return Forbid();

        var validTypes = new HashSet<string>
            { "Moderation", "CommentModeration", "Rewrite", "ImageModeration", "IdeaSupport", "CommentSupport", "QuestionSupport" };

        if (!validTypes.Contains(promptType))
        {
            TempData["Error"] = localizer["Settings_InvalidPromptType"].Value;
            return RedirectToAction(nameof(Index));
        }

        if (!string.IsNullOrEmpty(value) && value.Length > MaxPromptLength)
        {
            TempData["Error"] = string.Format(localizer["Settings_PromptTooLong"].Value, MaxPromptLength);
            return RedirectToAction(nameof(Index));
        }

        await platformService.SavePromptOverrideAsync(user.ProjectPlatformId.Value, promptType, value);
        TempData["Success"] = localizer["Settings_PromptSaved"].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ResetPrompt(string promptType)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.ProjectPlatformId is null) return Forbid();

        await platformService.SavePromptOverrideAsync(user.ProjectPlatformId.Value, promptType, null);
        TempData["Success"] = localizer["Settings_PromptReset"].Value;
        return RedirectToAction(nameof(Index));
    }

    private async Task<HashSet<string>> GetAllowedModelIdsAsync()
    {
        var sysSettings = await platformService.GetSystemAiSettingsAsync();
        if (sysSettings is null)
            return AiModelCatalog.All.Where(m => m.Provider == AiProvider.VertexAi).Select(m => m.Id).ToHashSet();
        return (JsonSerializer.Deserialize<List<string>>(sysSettings.AllowedModelIds) ?? []).ToHashSet();
    }

    private async Task<PlatformAdminSettingsViewModel> BuildViewModel(int platformId, string platformName)
    {
        var settings       = await platformService.GetPlatformSettingsAsync(platformId);
        var allowedIds       = await GetAllowedModelIdsAsync();
        var modelAvailability = await aiAdminService.GetModelAvailabilityAsync();
        var globalPrompts    = await aiAdminService.GetGlobalPromptsAsync();
        var allowedModels    = AiModelCatalog.All
            .Where(m => allowedIds.Contains(m.Id) && modelAvailability.GetValueOrDefault(m.Id, false))
            .ToList();

        // API-fetched prompts are preferred; fall back to locally loaded AiConfig if the API is unreachable.
        string Fallback(string apiPrompt, string localPrompt) =>
            !string.IsNullOrEmpty(apiPrompt) ? apiPrompt : localPrompt;

        var basicInfo = new PlatformAdminBasicInfoViewModel(
            settings?.ContactName      ?? string.Empty,
            settings?.ContactEmail     ?? string.Empty,
            settings?.BillingReference ?? string.Empty
        );

        var aiSettings = new PlatformAdminAiViewModel(
            allowedModels,
            settings?.SelectedAiModel ?? allowedModels.FirstOrDefault()?.Id ?? string.Empty,
            Enum.TryParse<AiAssistanceType>(settings?.AiAssistanceMode, out var aiMode) ? aiMode : AiAssistanceType.ProActive,
            modelAvailability.Where(kv => !kv.Value).Select(kv => kv.Key).ToHashSet()
        );

        var prompts = new PlatformAdminPromptsViewModel([
            new("Moderation",        localizer["Prompt_Moderation"].Value,        settings?.PromptModeration        ?? Fallback(globalPrompts.Moderation,        aiConfig.Moderation.ModerationPrompt),        settings?.PromptModeration        is not null),
            new("CommentModeration", localizer["Prompt_CommentModeration"].Value, settings?.PromptCommentModeration ?? Fallback(globalPrompts.CommentModeration,  aiConfig.Moderation.CommentModerationPrompt),  settings?.PromptCommentModeration is not null),
            new("Rewrite",           localizer["Prompt_Rewrite"].Value,           settings?.PromptRewrite           ?? Fallback(globalPrompts.Rewrite,            aiConfig.Moderation.RewritePrompt),            settings?.PromptRewrite           is not null),
            new("ImageModeration",   localizer["Prompt_ImageModeration"].Value,   settings?.PromptImageModeration   ?? Fallback(globalPrompts.ImageModeration,    aiConfig.Moderation.ImageModerationPrompt),    settings?.PromptImageModeration   is not null),
            new("IdeaSupport",       localizer["Prompt_IdeaSupport"].Value,       settings?.PromptIdeaSupport       ?? Fallback(globalPrompts.IdeaSupport,        aiConfig.IdeaSupport.SystemPrompt),            settings?.PromptIdeaSupport       is not null),
            new("CommentSupport",    localizer["Prompt_CommentSupport"].Value,    settings?.PromptCommentSupport    ?? Fallback(globalPrompts.CommentSupport,     aiConfig.CommentSupport.SystemPrompt),         settings?.PromptCommentSupport    is not null),
            new("QuestionSupport",   localizer["Prompt_QuestionSupport"].Value,   settings?.PromptQuestionSupport   ?? Fallback(globalPrompts.QuestionSupport,    aiConfig.QuestionSupport.SystemPrompt),        settings?.PromptQuestionSupport   is not null),
        ]);

        return new PlatformAdminSettingsViewModel(platformId, platformName, basicInfo, aiSettings, prompts);
    }
}
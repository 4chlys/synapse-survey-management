using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.UI.Admin.Data;
using SynapseEnqueteManagement.UI.Admin.Models.Platform;

namespace SynapseEnqueteManagement.UI.Admin.Services.Platform;

// Platform-level data + admin-user queries used by UI-Admin.
// Platform CRUD (ProjectPlatform rows) is delegated to the API via PlatformApiService —
// UI-Admin no longer knows about the BL/Domain ProjectPlatform entity.
// PlatformSettings + SystemAiSettings tables are UI-Admin-owned and still queried locally.
public class PlatformAdminService(
    AdminDbContext db,
    UserManager<ApplicationUser> userManager,
    PlatformApiService platformApi)
{
    // ---- Platform CRUD (delegated to API) ----

    public Task<List<PlatformDto>> GetAllPlatformsAsync()
        => platformApi.GetAllAsync();

    public Task<PlatformDto> GetPlatformByIdAsync(int id)
        => platformApi.GetByIdAsync(id);

    // Basic platform info as a view model, so controllers don't have to touch the
    // platform DTO directly. Backed by the API (no EF access to ProjectPlatforms).
    public async Task<PlatformFormViewModel> GetPlatformFormAsync(int id)
    {
        var platform = await platformApi.GetByIdAsync(id);
        return platform is null
            ? null
            : new PlatformFormViewModel
            {
                ProjectPlatformId = platform.ProjectPlatformId,
                Name = platform.Name,
                LogoUrl = platform.LogoUrl,
                WebsiteUrl = platform.WebsiteUrl
            };
    }

    public Task<PlatformDto> CreatePlatformAsync(string name, string logoUrl, string websiteUrl)
        => platformApi.CreateAsync(name, logoUrl, websiteUrl);

    public Task UpdatePlatformAsync(int id, string name, string logoUrl, string websiteUrl)
        => platformApi.UpdateAsync(id, name, logoUrl, websiteUrl);

    // ---- Admin users (local Identity store) ----

    public async Task<List<ApplicationUser>> GetAdminsForPlatformAsync(int platformId)
    {
        return await userManager.Users
            .Where(u => u.ProjectPlatformId == platformId)
            .ToListAsync();
    }

    public async Task<IdentityResult> RemoveAdminAsync(int platformId, string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || user.ProjectPlatformId != platformId)
            return IdentityResult.Failed(new IdentityError { Description = "User not found." });
        return await userManager.DeleteAsync(user);
    }

    // Returns admins with non-null platform id. Sorted by platform name (resolved via the
    // API instead of an EF join), then by email. Page is small — in-memory sort is fine.
    public async Task<List<ApplicationUser>> GetAllAdminsAsync()
    {
        var admins = await userManager.Users
            .Where(u => u.ProjectPlatformId != null)
            .ToListAsync();

        if (admins.Count == 0) return admins;

        var platforms = await platformApi.GetAllAsync();
        var nameById = platforms.ToDictionary(p => p.ProjectPlatformId, p => p.Name ?? string.Empty);

        return admins
            .OrderBy(u => nameById.TryGetValue(u.ProjectPlatformId!.Value, out var n) ? n : string.Empty)
            .ThenBy(u => u.Email)
            .ToList();
    }

    // ---- PlatformSettings (UI-Admin-owned table) ----

    public async Task<PlatformSettings> GetPlatformSettingsAsync(int platformId)
    {
        return await db.PlatformSettings.FirstOrDefaultAsync(s => s.ProjectPlatformId == platformId);
    }

    public async Task SavePlatformSettingsAsync(int platformId, PlatformConfigurationViewModel model)
    {
        var settings = await GetOrCreatePlatformSettingsAsync(platformId);

        settings.ContactName = model.ContactName;
        settings.ContactEmail = model.ContactEmail;
        settings.BillingReference = model.BillingReference;
        settings.AiAssistanceEnabled = model.AiAssistanceEnabled;
        settings.IdeationEnabled = model.IdeationEnabled;
        settings.MaxProjects = model.MaxProjects;
        settings.MaxAdmins = model.MaxAdmins;
        settings.AiModelName = model.AiModelName;
        settings.CostDashboardUrl = model.CostDashboardUrl;

        await SavePlatformSettingsAsync(settings);
    }

    // A PlatformSettings row may not exist yet for a platform; load it or stage a new one.
    private async Task<PlatformSettings> GetOrCreatePlatformSettingsAsync(int platformId)
        => await db.PlatformSettings.FirstOrDefaultAsync(s => s.ProjectPlatformId == platformId)
            ?? new PlatformSettings { ProjectPlatformId = platformId };

    // Attach the row on first save (Id == 0 means it was newly created above), then persist.
    private async Task SavePlatformSettingsAsync(PlatformSettings settings)
    {
        if (settings.Id == 0)
            db.PlatformSettings.Add(settings);
        await db.SaveChangesAsync();
    }

    public async Task<SystemAiSettings> GetSystemAiSettingsAsync()
    {
        return await db.SystemAiSettings.FirstOrDefaultAsync();
    }

    public async Task SaveSystemAiSettingsAsync(IReadOnlyList<string> allowedModelIds)
    {
        var settings = await db.SystemAiSettings.FirstOrDefaultAsync()
            ?? new SystemAiSettings { Id = 1 };

        settings.AllowedModelIds = System.Text.Json.JsonSerializer.Serialize(allowedModelIds);

        if (!await db.SystemAiSettings.AnyAsync(s => s.Id == 1))
            db.SystemAiSettings.Add(settings);

        await db.SaveChangesAsync();
    }

    public async Task SaveAiAssistanceModeAsync(int platformId, string mode)
    {
        var settings = await GetOrCreatePlatformSettingsAsync(platformId);
        settings.AiAssistanceMode = mode;
        await SavePlatformSettingsAsync(settings);
    }

    public async Task SaveSelectedAiModelAsync(int platformId, string selectedModelId)
    {
        var settings = await GetOrCreatePlatformSettingsAsync(platformId);
        settings.SelectedAiModel = selectedModelId;
        await SavePlatformSettingsAsync(settings);
    }

    public async Task SavePromptOverrideAsync(int platformId, string promptType, string value)
    {
        var settings = await GetOrCreatePlatformSettingsAsync(platformId);

        switch (promptType)
        {
            case "Moderation":        settings.PromptModeration        = value; break;
            case "CommentModeration": settings.PromptCommentModeration = value; break;
            case "Rewrite":           settings.PromptRewrite           = value; break;
            case "ImageModeration":   settings.PromptImageModeration   = value; break;
            case "IdeaSupport":       settings.PromptIdeaSupport       = value; break;
            case "CommentSupport":    settings.PromptCommentSupport    = value; break;
            case "QuestionSupport":   settings.PromptQuestionSupport   = value; break;
            case "IdeaSummary":       settings.PromptIdeaSummary       = value; break;
            default: return;
        }

        await SavePlatformSettingsAsync(settings);
    }

    public async Task SaveIdeaSummaryMaxIdeasAsync(int platformId, int maxIdeas)
    {
        var settings = await GetOrCreatePlatformSettingsAsync(platformId);
        settings.IdeaSummaryMaxIdeas = Math.Clamp(maxIdeas, 10, 2000);
        await SavePlatformSettingsAsync(settings);
    }
}

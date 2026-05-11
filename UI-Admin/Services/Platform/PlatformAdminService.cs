using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Platform.Entities;
using SynapseEnqueteManagement.UI.Admin.Data;
using SynapseEnqueteManagement.UI.Admin.Models.Platform;

namespace SynapseEnqueteManagement.UI.Admin.Services.Platform;

public class PlatformAdminService(AdminDbContext db, UserManager<ApplicationUser> userManager)
{
    public async Task<List<ProjectPlatform>> GetAllPlatformsAsync()
    {
        return await db.ProjectPlatforms.OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<ProjectPlatform> GetPlatformByIdAsync(int id)
    {
        return await db.ProjectPlatforms.FindAsync(id);
    }

    public async Task<ProjectPlatform> CreatePlatformAsync(string name, string logoUrl, string websiteUrl)
    {
        var platform = new ProjectPlatform
        {
            Name = name,
            LogoUrl = logoUrl,
            WebsiteUrl = websiteUrl,
            CreatedAt = DateTime.UtcNow
        };
        db.ProjectPlatforms.Add(platform);
        await db.SaveChangesAsync();
        return platform;
    }

    public async Task UpdatePlatformAsync(int id, string name, string logoUrl, string websiteUrl)
    {
        var platform = await db.ProjectPlatforms.FindAsync(id);
        if (platform is null) return;
        platform.Name = name;
        platform.LogoUrl = logoUrl;
        platform.WebsiteUrl = websiteUrl;
        await db.SaveChangesAsync();
    }

    public async Task<List<ApplicationUser>> GetAdminsForPlatformAsync(int platformId)
    {
        return await userManager.Users
            .Where(u => u.ProjectPlatformId == platformId)
            .ToListAsync();
    }

    public async Task<IdentityResult> AddAdminAsync(int platformId, string email, string password)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            ProjectPlatformId = platformId
        };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded) return result;
        await userManager.AddToRoleAsync(user, "ProjectPlatformAdmin");
        return result;
    }

    public async Task<IdentityResult> RemoveAdminAsync(int platformId, string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || user.ProjectPlatformId != platformId)
            return IdentityResult.Failed(new IdentityError { Description = "Gebruiker niet gevonden." });
        return await userManager.DeleteAsync(user);
    }

    public async Task<List<ApplicationUser>> GetAllAdminsAsync()
    {
        return await userManager.Users
            .Include(u => u.ProjectPlatform)
            .Where(u => u.ProjectPlatformId != null)
            .OrderBy(u => u.ProjectPlatform.Name)
            .ThenBy(u => u.Email)
            .ToListAsync();
    }

    public async Task<PlatformSettings> GetPlatformSettingsAsync(int platformId)
    {
        return await db.PlatformSettings.FirstOrDefaultAsync(s => s.ProjectPlatformId == platformId);
    }

    public async Task SavePlatformSettingsAsync(int platformId, PlatformConfigurationViewModel model)
    {
        var settings = await db.PlatformSettings.FirstOrDefaultAsync(s => s.ProjectPlatformId == platformId)
            ?? new PlatformSettings { ProjectPlatformId = platformId };

        settings.ContactName = model.ContactName;
        settings.ContactEmail = model.ContactEmail;
        settings.BillingReference = model.BillingReference;
        settings.AiAssistanceEnabled = model.AiAssistanceEnabled;
        settings.IdeationEnabled = model.IdeationEnabled;
        settings.MaxProjects = model.MaxProjects;
        settings.MaxAdmins = model.MaxAdmins;

        if (settings.Id == 0)
            db.PlatformSettings.Add(settings);

        await db.SaveChangesAsync();
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Platform.Entities;
using SynapseEnqueteManagement.UI.Admin.Data;

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
}

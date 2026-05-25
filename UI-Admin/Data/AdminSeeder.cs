using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.BL.Domain.Ai;
using SynapseEnqueteManagement.BL.Domain.Platform.Entities;

namespace SynapseEnqueteManagement.UI.Admin.Data;

public class AdminSeeder(
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    AdminDbContext db,
    ILogger<AdminSeeder> logger)
{
    public async Task SeedAsync()
    {
        await EnsureRoleAsync("SystemAdmin");
        await EnsureRoleAsync("ProjectPlatformAdmin");
        await EnsureSystemAdminAsync();
        await EnsurePlatformAdminsAsync();
        await EnsureSystemAiSettingsAsync();
    }

    private async Task EnsureRoleAsync(string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
            await roleManager.CreateAsync(new IdentityRole(roleName));
    }

    private async Task EnsureSystemAdminAsync()
    {
        var email = configuration["SystemAdmin:Email"]
                    ?? throw new InvalidOperationException("SystemAdmin:Email is not configured.");
        var password = configuration["SystemAdmin:Password"]
                       ?? throw new InvalidOperationException("SystemAdmin:Password is not configured.");

        if (await userManager.FindByEmailAsync(email) is not null) return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            ProjectPlatformId = null
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Failed to seed SystemAdmin: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        await userManager.AddToRoleAsync(user, "SystemAdmin");
    }

    private async Task EnsurePlatformAdminsAsync()
    {
        List<ProjectPlatform> platforms;
        try
        {
            platforms = await db.ProjectPlatforms.ToListAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load ProjectPlatforms during admin seeding");
            return;
        }

        logger.LogInformation("Seeding platform admins for {Count} platforms", platforms.Count);

        foreach (var platform in platforms)
        {
            var slug = platform.Name.ToLower().Replace(" ", "").Replace(".", "");
            var email = $"admin@{slug}.com";

            if (await userManager.FindByEmailAsync(email) is not null)
            {
                logger.LogInformation("Platform admin {Email} already exists — skipping", email);
                continue;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                ProjectPlatformId = platform.ProjectPlatformId
            };

            var result = await userManager.CreateAsync(user, "Admin1234!");
            if (!result.Succeeded)
            {
                logger.LogError("Failed to seed ProjectPlatformAdmin for '{Platform}': {Errors}",
                    platform.Name, string.Join(", ", result.Errors.Select(e => e.Description)));
                continue;
            }

            var roleResult = await userManager.AddToRoleAsync(user, "ProjectPlatformAdmin");
            if (!roleResult.Succeeded)
            {
                logger.LogError("Failed to add ProjectPlatformAdmin role to '{Email}': {Errors}",
                    email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                continue;
            }

            logger.LogInformation("Seeded platform admin {Email} for platform {Platform}", email, platform.Name);
        }
    }

    private async Task EnsureSystemAiSettingsAsync()
    {
        if (await db.SystemAiSettings.AnyAsync()) return;

        var vertexModelIds = AiModelCatalog.All
            .Where(m => m.Provider == AiProvider.VertexAi)
            .Select(m => m.Id)
            .ToList();

        db.SystemAiSettings.Add(new SystemAiSettings
        {
            Id = 1,
            AllowedModelIds = System.Text.Json.JsonSerializer.Serialize(vertexModelIds),
        });
        await db.SaveChangesAsync();
        logger.LogInformation("[Seeder] SystemAiSettings seeded with {Count} Vertex AI models.", vertexModelIds.Count);
    }
}
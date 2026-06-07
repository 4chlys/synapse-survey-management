using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.UI.Admin.Models.Ai;
using SynapseEnqueteManagement.UI.Admin.Services.Platform;

namespace SynapseEnqueteManagement.UI.Admin.Data;

// Seeds Identity roles, the SystemAdmin account, the per-platform admin accounts
// (one per known platform), and the row in the UI-Admin-owned SystemAiSettings table.
// Platforms themselves are fetched over HTTP from the API — UI-Admin has no
// EF mapping of the ProjectPlatforms table.
public class AdminSeeder(
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    AdminDbContext db,
    PlatformApiService platformApi,
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

    // Cold-start retry: the admin MIG and the API MIG boot independently, so on a fresh
    // stack the API (reached over the public LB) is often not yet serving when we get here.
    // Retry a few times with backoff before giving up; the next restart is the final backstop.
    private const int PlatformFetchMaxAttempts = 5;
    private static readonly TimeSpan PlatformFetchRetryDelay = TimeSpan.FromSeconds(10);

    // Reads platforms over HTTP from the API. If the API stays unreachable across all
    // retries (cold-start race), we skip — the next restart will seed the per-platform admins.
    private async Task EnsurePlatformAdminsAsync()
    {
        List<Models.Platform.PlatformDto> platforms;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                platforms = await platformApi.GetAllAsync();
                break;
            }
            catch (Exception ex) when (attempt < PlatformFetchMaxAttempts)
            {
                logger.LogWarning(ex,
                    "API not reachable for platform-admin seeding (attempt {Attempt}/{Max}); retrying in {Delay}s",
                    attempt, PlatformFetchMaxAttempts, PlatformFetchRetryDelay.TotalSeconds);
                await Task.Delay(PlatformFetchRetryDelay);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Skipping platform-admin seeding: API still not reachable after {Max} attempts", PlatformFetchMaxAttempts);
                return;
            }
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

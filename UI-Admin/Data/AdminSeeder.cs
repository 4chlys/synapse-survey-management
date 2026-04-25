// UI-Admin/Data/AdminSeeder.cs
using Microsoft.AspNetCore.Identity;

namespace SynapseEnqueteManagement.UI.Admin.Data;

public class AdminSeeder(
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration)
{
    public async Task SeedAsync()
    {
        await EnsureRoleAsync("SystemAdmin");
        await EnsureRoleAsync("ProjectPlatformAdmin");
        await EnsureSystemAdminAsync();
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
}

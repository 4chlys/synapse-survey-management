using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using SynapseEnqueteManagement.UI.Admin.Data;
using SynapseEnqueteManagement.UI.Admin.Services.Email;

namespace SynapseEnqueteManagement.UI.Admin.Services.Platform;

// Single entry point for adding a platform administrator: creates a passwordless
// account and emails a set-password link. The account has no usable password until
// the invitee follows the link. Shared by every admin-creation flow so the invite
// behaviour stays consistent. The caller supplies buildResetUrl because URL building
// belongs to the controller (it owns the routing context).
public class AdminInviteService(
    UserManager<ApplicationUser> userManager,
    EmailApiService emailApiService,
    IStringLocalizer<SharedResource> localizer,
    ILogger<AdminInviteService> logger)
{
    public async Task<IdentityResult> InviteAdminAsync(
        int platformId, string platformName, string email, Func<string, string> buildResetUrl)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            ProjectPlatformId = platformId
        };
        var result = await userManager.CreateAsync(user);
        if (!result.Succeeded) return result;
        await userManager.AddToRoleAsync(user, "ProjectPlatformAdmin");

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetUrl = buildResetUrl(token);

        var subject = localizer["Email_Welcome_Subject", platformName].Value;
        var body = localizer["Email_Welcome_Body", platformName, email, resetUrl].Value;
        try { await emailApiService.SendAsync(email, subject, body); }
        catch (Exception ex) { logger.LogWarning(ex, "Failed to send admin invite email to {Email}", email); }

        return result;
    }
}

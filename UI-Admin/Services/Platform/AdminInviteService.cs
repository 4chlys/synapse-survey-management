using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using SynapseEnqueteManagement.UI.Admin.Data;
using SynapseEnqueteManagement.UI.Admin.Services.Email;

namespace SynapseEnqueteManagement.UI.Admin.Services.Platform;

// Result of an invite. EmailAttempted=false means the caller asked us to skip the email
// (they'll deliver the link out-of-band). EmailAttempted=true but EmailSent=false means
// we tried and the email API rejected/failed — the caller should surface a warning.
public record AdminInviteResult(IdentityResult Identity, bool EmailAttempted, bool EmailSent);

// Single entry point for adding a platform administrator: creates a passwordless
// account and (optionally) emails a set-password link. The account has no usable
// password until the invitee follows the link. Shared by every admin-creation flow
// so the invite behaviour stays consistent. The caller supplies buildResetUrl
// because URL building belongs to the controller (it owns the routing context).
public class AdminInviteService(
    UserManager<ApplicationUser> userManager,
    EmailApiService emailApiService,
    IStringLocalizer<SharedResource> localizer,
    ILogger<AdminInviteService> logger)
{
    public async Task<AdminInviteResult> InviteAdminAsync(
        int platformId, string platformName, string email, Func<string, string> buildResetUrl,
        bool sendEmail, CancellationToken ct = default)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            ProjectPlatformId = platformId
        };
        var identity = await userManager.CreateAsync(user);
        if (!identity.Succeeded) return new AdminInviteResult(identity, false, false);

        var roleResult = await userManager.AddToRoleAsync(user, "ProjectPlatformAdmin");
        if (!roleResult.Succeeded)
        {
            // Compensating delete: the user was created but has no role — undo it so InviteAdminAsync stays all-or-nothing.
            await userManager.DeleteAsync(user);
            return new AdminInviteResult(roleResult, false, false);
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetUrl = buildResetUrl(token);

        if (!sendEmail) return new AdminInviteResult(identity, false, false);

        var subject = localizer["Email_Welcome_Subject", platformName].Value;
        var body = localizer["Email_Welcome_Body", platformName, email, resetUrl].Value;
        try
        {
            await emailApiService.SendAsync(email, subject, body, ct);
            return new AdminInviteResult(identity, true, true);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Admin invite email send cancelled for {Email}", email);
            return new AdminInviteResult(identity, true, false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send admin invite email to {Email}", email);
            return new AdminInviteResult(identity, true, false);
        }
    }
}

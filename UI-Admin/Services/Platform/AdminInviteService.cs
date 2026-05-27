using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using SynapseEnqueteManagement.UI.Admin.Data;
using SynapseEnqueteManagement.UI.Admin.Services.Email;

namespace SynapseEnqueteManagement.UI.Admin.Services.Platform;

// Result of an invite. Just the identity result + the invite link (so the
// controller can show it as a fallback if SendEmail=false). The earlier
// EmailAttempted/EmailSent flags are gone: once the row lands in the
// EmailJob queue we no longer know synchronously whether it sent, so there
// is no honest value to surface here. The operator queue page is the place
// to inspect delivery.
public record AdminInviteResult(IdentityResult Identity, string InviteLink);

// Creates a passwordless platform-admin account and (optionally) enqueues a
// set-password welcome email. The account has no usable password until the
// invitee follows the link.
public class AdminInviteService(
    UserManager<ApplicationUser> userManager,
    EmailApiService emailApiService,
    IStringLocalizer<SharedResource> localizer)
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
        if (!identity.Succeeded) return new AdminInviteResult(identity, null);

        var roleResult = await userManager.AddToRoleAsync(user, "ProjectPlatformAdmin");
        if (!roleResult.Succeeded)
        {
            // Compensating delete: keep InviteAdminAsync all-or-nothing.
            await userManager.DeleteAsync(user);
            return new AdminInviteResult(roleResult, null);
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetUrl = buildResetUrl(token);

        if (!sendEmail) return new AdminInviteResult(identity, resetUrl);

        var subject = localizer["Email_Welcome_Subject", platformName].Value;
        var body    = localizer["Email_Welcome_Body", platformName, email, resetUrl].Value;

        // Persists to the EmailJob queue and returns immediately — the
        // EmailJobWorker handles transport + retries.
        await emailApiService.EnqueueAsync(new EnqueueEmailRequest(
            To:                email,
            Bcc:               Array.Empty<string>(),
            ReplyTo:           null,
            Subject:           subject,
            Body:              body,
            Category:          "AdminInvite",
            LanguageCode:      null,
            ProjectPlatformId: platformId,
            ProjectId:         null,
            IdeaId:            null,
            CreatedByUserId:   user.Id), ct);

        return new AdminInviteResult(identity, resetUrl);
    }
}

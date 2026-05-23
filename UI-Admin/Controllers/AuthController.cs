using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using SynapseEnqueteManagement.UI.Admin.Data;
using SynapseEnqueteManagement.UI.Admin.Models.Auth;
using SynapseEnqueteManagement.UI.Admin.Services.Email;

namespace SynapseEnqueteManagement.UI.Admin.Controllers;

public class AuthController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    EmailApiService emailApiService,
    ILogger<AuthController> logger,
    IStringLocalizer<SharedResource> localizer) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            // Guard against stale cookies (e.g. after DB reset/reseed): if the cookie
            // references a user ID that no longer exists, sign out and show the login form.
            // Without this check, GetUserAsync in downstream controllers returns null and
            // redirects back here, creating an infinite redirect loop.
            var user = await userManager.GetUserAsync(User);
            if (user is null)
            {
                await signInManager.SignOutAsync();
                return View(new LoginViewModel { ReturnUrl = returnUrl });
            }
            return await RedirectAfterLogin(User.Identity!.Name!);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await signInManager.PasswordSignInAsync(
            model.Email, model.Password, isPersistent: false, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, localizer["Auth_AccountLocked"]);
            return View(model);
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, localizer["Auth_InvalidCredentials"]);
            return View(model);
        }

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return await RedirectAfterLogin(model.Email);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction(nameof(Login));

        var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await signInManager.RefreshSignInAsync(user);

        TempData["SuccessMessage"] = localizer["Auth_PasswordChanged"].Value;
        return RedirectToAction(nameof(ChangePassword));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel(string.Empty));

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is not null)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var resetUrl = Url.Action("ResetPassword", "Auth",
                new { email = model.Email, token },
                protocol: Request.Scheme);

            var body = localizer["Email_ForgotPassword_Body", resetUrl].Value;
            var subject = localizer["Email_ForgotPassword_Subject"].Value;
            try { await emailApiService.SendAsync(model.Email, subject, body, ct); }
            catch (OperationCanceledException) { logger.LogInformation("Password reset email send cancelled for {Email}", model.Email); }
            catch (Exception ex) { logger.LogWarning(ex, "Failed to send password reset email to {Email}", model.Email); }
        }

        // Always show the same message whether or not the email address exists (security)
        TempData["ResetEmailSent"] = true;
        return RedirectToAction(nameof(ForgotPassword));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string email, string token)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            return RedirectToAction(nameof(Login));

        return View(new ResetPasswordViewModel
        {
            Email = email,
            Token = token
        });
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            TempData["ResetSuccess"] = true;
            return RedirectToAction(nameof(Login));
        }

        var result = await userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        TempData["ResetSuccess"] = true;
        return RedirectToAction(nameof(Login));
    }

    private async Task<IActionResult> RedirectAfterLogin(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return RedirectToAction(nameof(Login));

        var roles = await userManager.GetRolesAsync(user);
        if (roles.Contains("SystemAdmin"))
            return RedirectToAction("Index", "Platform");
        if (roles.Contains("ProjectPlatformAdmin"))
            return RedirectToAction("Index", "Project");

        // User exists but has no recognized role — sign out to prevent an infinite
        // loop via AccessDeniedPath → Login → RedirectAfterLogin → Forbid → repeat.
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.Admin.Data;
using SynapseEnqueteManagement.UI.Admin.Models.Auth;

namespace SynapseEnqueteManagement.UI.Admin.Controllers;

public class AuthController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) : Controller
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
            ModelState.AddModelError(string.Empty, "Account is vergrendeld. Probeer het later opnieuw.");
            return View(model);
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Ongeldig e-mailadres of wachtwoord.");
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

        TempData["SuccessMessage"] = "Wachtwoord succesvol gewijzigd.";
        return RedirectToAction(nameof(ChangePassword));
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

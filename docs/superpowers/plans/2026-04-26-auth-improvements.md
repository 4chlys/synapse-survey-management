# Auth Improvements Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix the PPA post-login redirect bug, add a Change Password flow, and surface a "Wachtwoord wijzigen" link in the navbar.

**Architecture:** All changes live in `AuthController` (existing MVC controller) and its view folder. The `ChangePassword` flow follows the exact same pattern as `Login`: ViewModel → Controller action (GET + POST) → Cshtml view. No scaffolded Identity UI is introduced — the custom controller approach already used is the right fit for this Tailwind-based project.

**Tech Stack:** .NET 9, ASP.NET Core Identity, ASP.NET Core MVC, Tailwind CSS 4.

---

## File Map

- Modify: `UI-Admin/Controllers/AuthController.cs` — fix redirect, add `ChangePassword` GET/POST
- Create: `UI-Admin/Models/Auth/ChangePasswordViewModel.cs`
- Create: `UI-Admin/Views/Auth/ChangePassword.cshtml`
- Modify: `UI-Admin/Views/Shared/_Layout.cshtml` — add "Wachtwoord wijzigen" link

---

## Task 1: Fix PPA post-login redirect

**Files:**
- Modify: `UI-Admin/Controllers/AuthController.cs`

**Context:** `RedirectAfterLogin` currently sends ProjectPlatformAdmins to `RedirectToAction("Index", "Dashboard")`. There is no `DashboardController` in UI-Admin — this 404s. PPAs should land on `Project/Index` (their project list, already filtered by platform).

- [ ] **Step 1: Open `UI-Admin/Controllers/AuthController.cs` and locate `RedirectAfterLogin`**

The method currently reads:
```csharp
if (roles.Contains("ProjectPlatformAdmin"))
    return RedirectToAction("Index", "Dashboard");
```

- [ ] **Step 2: Change the redirect**

Replace that line with:
```csharp
if (roles.Contains("ProjectPlatformAdmin"))
    return RedirectToAction("Index", "Project");
```

- [ ] **Step 3: Build**

```bash
dotnet build --no-incremental 2>&1 | tail -5
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add UI-Admin/Controllers/AuthController.cs
git commit -m "fix(auth): redirect PPAs to Project/Index after login, not missing Dashboard/Index"
```

---

## Task 2: ChangePasswordViewModel and AuthController actions

**Files:**
- Create: `UI-Admin/Models/Auth/ChangePasswordViewModel.cs`
- Modify: `UI-Admin/Controllers/AuthController.cs`

- [ ] **Step 1: Create `UI-Admin/Models/Auth/ChangePasswordViewModel.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.UI.Admin.Models.Auth;

public class ChangePasswordViewModel
{
    [Required]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Wachtwoorden komen niet overeen.")]
    public string ConfirmNewPassword { get; set; }
}
```

- [ ] **Step 2: Add `ChangePassword` GET action to `AuthController`**

The controller already has `[AllowAnonymous]` on `Login` but no class-level `[Authorize]`. Add the following two actions after `Logout`. The actions must be `[Authorize]` (any authenticated user can change their own password):

```csharp
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

    // Re-sign the user in so their cookie reflects the updated security stamp
    await signInManager.RefreshSignInAsync(user);

    TempData["SuccessMessage"] = "Wachtwoord succesvol gewijzigd.";
    return RedirectToAction(nameof(ChangePassword));
}
```

Note: `RefreshSignInAsync` re-issues the auth cookie after a password change, preventing the session from being invalidated by the security stamp validator.

- [ ] **Step 3: Build**

```bash
dotnet build --no-incremental 2>&1 | tail -5
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add UI-Admin/Models/Auth/ChangePasswordViewModel.cs UI-Admin/Controllers/AuthController.cs
git commit -m "feat(auth): add ChangePassword GET/POST actions with security stamp refresh"
```

---

## Task 3: ChangePassword view

**Files:**
- Create: `UI-Admin/Views/Auth/ChangePassword.cshtml`

- [ ] **Step 1: Create `UI-Admin/Views/Auth/ChangePassword.cshtml`**

Style consistent with `Login.cshtml` (dark Tailwind, centered card, same input classes). The `TempData["SuccessMessage"]` set by the POST action is shown as a green banner above the form.

```cshtml
@model SynapseEnqueteManagement.UI.Admin.Models.Auth.ChangePasswordViewModel
@{
    ViewData["Title"] = "Wachtwoord wijzigen";
    Layout = "_Layout";
}

<div class="min-h-screen bg-gray-900 flex items-center justify-center">
    <div class="w-full max-w-sm px-6">
        <p class="text-white text-sm font-bold text-center mb-6">Wachtwoord wijzigen</p>

        @if (TempData["SuccessMessage"] is string msg)
        {
            <div class="bg-green-900 border border-green-600 text-green-300 text-xs px-3 py-2 rounded mb-4">
                @msg
            </div>
        }

        <form asp-action="ChangePassword" method="post" class="space-y-4">

            <div>
                <label asp-for="CurrentPassword" class="block text-xs text-gray-300 mb-1">Huidig wachtwoord</label>
                <input asp-for="CurrentPassword" type="password" autocomplete="current-password"
                       class="w-full bg-gray-800 border border-gray-600 text-white text-xs px-3 py-2 focus:outline-none focus:border-white" />
                <span asp-validation-for="CurrentPassword" class="text-red-400 text-xs"></span>
            </div>

            <div>
                <label asp-for="NewPassword" class="block text-xs text-gray-300 mb-1">Nieuw wachtwoord</label>
                <input asp-for="NewPassword" type="password" autocomplete="new-password"
                       class="w-full bg-gray-800 border border-gray-600 text-white text-xs px-3 py-2 focus:outline-none focus:border-white" />
                <span asp-validation-for="NewPassword" class="text-red-400 text-xs"></span>
            </div>

            <div>
                <label asp-for="ConfirmNewPassword" class="block text-xs text-gray-300 mb-1">Bevestig nieuw wachtwoord</label>
                <input asp-for="ConfirmNewPassword" type="password" autocomplete="new-password"
                       class="w-full bg-gray-800 border border-gray-600 text-white text-xs px-3 py-2 focus:outline-none focus:border-white" />
                <span asp-validation-for="ConfirmNewPassword" class="text-red-400 text-xs"></span>
            </div>

            <div asp-validation-summary="ModelOnly" class="text-red-400 text-xs"></div>

            <button type="submit"
                    class="w-full py-2 text-xs border border-gray-500 bg-gray-800 text-white hover:bg-gray-700 transition">
                Wijzigen
            </button>

        </form>

        <div class="mt-4 text-center">
            <a asp-controller="Project" asp-action="Index"
               class="text-xs text-gray-400 hover:text-white">← Terug naar projecten</a>
        </div>
    </div>
</div>

@section Scripts {
    @await Html.PartialAsync("_ValidationScriptsPartial")
}
```

- [ ] **Step 2: Build**

```bash
dotnet build --no-incremental 2>&1 | tail -5
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add UI-Admin/Views/Auth/ChangePassword.cshtml
git commit -m "feat(auth): add ChangePassword view with success banner"
```

---

## Task 4: Add "Wachtwoord wijzigen" link to navbar

**Files:**
- Modify: `UI-Admin/Views/Shared/_Layout.cshtml`

- [ ] **Step 1: Open `_Layout.cshtml` and find the authenticated nav block**

The current nav renders inside `@if (User.Identity?.IsAuthenticated == true)`:
```html
<div class="flex items-center gap-4">
    <span class="text-gray-300 text-xs">@User.Identity.Name</span>
    <form asp-controller="Auth" asp-action="Logout" method="post" class="inline">
        ...
    </form>
</div>
```

- [ ] **Step 2: Add the change-password link**

Insert the link between the username span and the logout form:
```html
<div class="flex items-center gap-4">
    <span class="text-gray-300 text-xs">@User.Identity.Name</span>
    <a asp-controller="Auth" asp-action="ChangePassword"
       class="text-xs text-gray-400 hover:text-white transition">
        Wachtwoord wijzigen
    </a>
    <form asp-controller="Auth" asp-action="Logout" method="post" class="inline">
        <button type="submit"
                class="text-xs text-gray-400 hover:text-white border border-gray-600 px-3 py-1 hover:border-gray-400 transition">
            Uitloggen
        </button>
    </form>
</div>
```

- [ ] **Step 3: Build**

```bash
dotnet build --no-incremental 2>&1 | tail -5
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add UI-Admin/Views/Shared/_Layout.cshtml
git commit -m "feat(auth): add Wachtwoord wijzigen link to authenticated navbar"
```

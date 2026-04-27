using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.Admin.Models.Platform;
using SynapseEnqueteManagement.UI.Admin.Services.Platform;

namespace SynapseEnqueteManagement.UI.Admin.Controllers;

[Authorize(Roles = "SystemAdmin")]
public class PlatformController(PlatformAdminService platformService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var platforms = await platformService.GetAllPlatformsAsync();
        var viewModels = platforms.Select(p => new PlatformViewModel
        {
            ProjectPlatformId = p.ProjectPlatformId,
            Name = p.Name,
            LogoUrl = p.LogoUrl,
            WebsiteUrl = p.WebsiteUrl
        }).ToList();
        return View(viewModels);
    }

    public IActionResult Create()
    {
        return View(new PlatformFormViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(PlatformFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        await platformService.CreatePlatformAsync(
            model.Name,
            model.LogoUrl ?? string.Empty,
            model.WebsiteUrl ?? string.Empty);

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var platform = await platformService.GetPlatformByIdAsync(id);
        if (platform is null)
            return NotFound();

        var model = new PlatformFormViewModel
        {
            ProjectPlatformId = platform.ProjectPlatformId,
            Name = platform.Name,
            LogoUrl = platform.LogoUrl,
            WebsiteUrl = platform.WebsiteUrl
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, PlatformFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        await platformService.UpdatePlatformAsync(
            id,
            model.Name,
            model.LogoUrl ?? string.Empty,
            model.WebsiteUrl ?? string.Empty);

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Detail(int id)
    {
        var platform = await platformService.GetPlatformByIdAsync(id);
        if (platform is null) return NotFound();
        var admins = await platformService.GetAdminsForPlatformAsync(id);
        return View(new PlatformDetailViewModel
        {
            ProjectPlatformId = platform.ProjectPlatformId,
            Name = platform.Name,
            LogoUrl = platform.LogoUrl,
            WebsiteUrl = platform.WebsiteUrl,
            Admins = admins.Select(a => new AdminSummaryViewModel
            {
                UserName = a.UserName,
                Email = a.Email
            }).ToList()
        });
    }

    public async Task<IActionResult> AddAdmin(int id)
    {
        var platform = await platformService.GetPlatformByIdAsync(id);
        if (platform is null) return NotFound();
        return View(new AddAdminViewModel
        {
            ProjectPlatformId = id,
            PlatformName = platform.Name
        });
    }

    [HttpPost]
    public async Task<IActionResult> AddAdmin(int id, AddAdminViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await platformService.AddAdminAsync(id, model.Email, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        return RedirectToAction(nameof(Detail), new { id });
    }
}

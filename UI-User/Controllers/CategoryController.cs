using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.User.Models;
using SynapseEnqueteManagement.UI.User.Services;

namespace SynapseEnqueteManagement.UI.User.Controllers;

public class CategoryController(CategoryService categoryService, IProjectApiService projectApiService)
    : Controller
{
    public async Task<IActionResult> Index(int projectId)
    {
        ViewData["ProjectId"] = projectId;
        var categories = await categoryService.GetCategoriesAsync(projectId);
        return View(categories);
    }

    public async Task<IActionResult> FinalThankYou(int projectId)
    {
        var project = await projectApiService.GetProjectAsync(projectId);
        var vm = new ThankYouViewModel
        {
            ProjectId = projectId,
            OrganisationName = project?.OrganisationName ?? string.Empty,
            OrganisationLogoUrl = project?.OrganisationLogoUrl ?? string.Empty,
            OrganisationWebsiteUrl = project?.OrganisationWebsiteUrl ?? string.Empty,
        };
        return View(vm);
    }
}

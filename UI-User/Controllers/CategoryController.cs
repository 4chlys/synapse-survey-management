using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.User.Models;
using SynapseEnqueteManagement.UI.User.Services;

namespace SynapseEnqueteManagement.UI.User.Controllers;

public class CategoryController : Controller
{
    private readonly CategoryService _categoryService;
    private readonly IProjectApiService _projectApiService;

    public CategoryController(CategoryService categoryService, IProjectApiService projectApiService)
    {
        _categoryService = categoryService;
        _projectApiService = projectApiService;
    }

    public async Task<IActionResult> Index(int projectId)
    {
        ViewData["ProjectId"] = projectId;
        var project = await _projectApiService.GetProjectAsync(projectId);
        ViewData["OrganisationLogoUrl"] = project?.OrganisationLogoUrl;
        ViewData["OrganisationWebsiteUrl"] = project?.OrganisationWebsiteUrl;
        var categories = await _categoryService.GetCategoriesAsync(projectId);
        return View(categories);
    }

    public async Task<IActionResult> FinalThankYou(int projectId)
    {
        var project = await _projectApiService.GetProjectAsync(projectId);
        var vm = new ThankYouViewModel
        {
            ProjectId = projectId,
            OrganisationName = project?.OrganisationName ?? string.Empty,
            OrganisationLogoUrl = project?.OrganisationLogoUrl ?? string.Empty,
            OrganisationWebsiteUrl = project?.OrganisationWebsiteUrl ?? string.Empty,
        };
        ViewData["OrganisationLogoUrl"] = vm.OrganisationLogoUrl;
        ViewData["OrganisationWebsiteUrl"] = vm.OrganisationWebsiteUrl;
        return View(vm);
    }
}

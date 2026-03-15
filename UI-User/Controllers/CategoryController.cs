using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.User.Filters;
using SynapseEnqueteManagement.UI.User.Models;
using SynapseEnqueteManagement.UI.User.Models.Survey;
using SynapseEnqueteManagement.UI.User.Services.Ideation;
using SynapseEnqueteManagement.UI.User.Services.Project;

namespace SynapseEnqueteManagement.UI.User.Controllers;

[ServiceFilter(typeof(RequiresSurveyCompletionFilter))]
public class CategoryController(CategoryService categoryService, IProjectApiService projectApiService)
    : Controller
{
    public async Task<IActionResult> Index(int projectId)
    {
        ViewData["ProjectId"] = projectId;
        var project = await projectApiService.GetProjectAsync(projectId);
        ViewData["OrganisationLogoUrl"] = project?.OrganisationLogoUrl;
        ViewData["OrganisationWebsiteUrl"] = project?.OrganisationWebsiteUrl;
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
        ViewData["OrganisationLogoUrl"] = vm.OrganisationLogoUrl;
        ViewData["OrganisationWebsiteUrl"] = vm.OrganisationWebsiteUrl;
        return View(vm);
    }
}
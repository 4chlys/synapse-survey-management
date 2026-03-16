using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.User.Extensions;
using SynapseEnqueteManagement.UI.User.Filters;
using SynapseEnqueteManagement.UI.User.Models.Ideation;
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
        var projectTask = projectApiService.GetProjectAsync(projectId);
        var categoriesTask = categoryService.GetCategoriesAsync(projectId);

        await Task.WhenAll(projectTask, categoriesTask);

        this.SetLayoutData(await projectTask, projectId);

        return View(new CategoryIndexViewModel
        {
            ProjectId = projectId,
            Categories = await categoriesTask
        });
    }

    public async Task<IActionResult> FinalThankYou(int projectId)
    {
        var project = await projectApiService.GetProjectAsync(projectId);

        this.SetLayoutData(project, projectId);

        return View(new ThankYouViewModel
        {
            ProjectId = projectId,
            OrganisationName = project?.OrganisationName ?? string.Empty,
            OrganisationLogoUrl = project?.OrganisationLogoUrl ?? string.Empty,
            OrganisationWebsiteUrl = project?.OrganisationWebsiteUrl ?? string.Empty,
        });
    }
}
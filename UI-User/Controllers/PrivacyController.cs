using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.User.Extensions;
using SynapseEnqueteManagement.UI.User.Services.Project;

namespace SynapseEnqueteManagement.UI.User.Controllers;

public class PrivacyController(IProjectApiService projectApiService) : Controller
{
    public async Task<IActionResult> Index(int projectId)
    {
        var project = await projectApiService.GetProjectAsync(projectId);
        if (project is null) return View("NotFound");

        this.SetLayoutData(project, projectId);
        ViewData["OrganisationName"] = project.OrganisationName;
        return View();
    }
}

using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.Admin.Models.Project;
using SynapseEnqueteManagement.UI.Admin.Services.Project;
namespace SynapseEnqueteManagement.UI.Admin.Controllers;

public class ProjectController(ProjectAdminService projectService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var projects = await projectService.GetAllProjectsAsync();
        return View(projects);
    }

    public IActionResult Create()
    {
        return View(new CreateProjectViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProjectViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        await projectService.CreateProjectAsync(model.Name, model.ImageUrl);
        return RedirectToAction(nameof(Index));
    }
}
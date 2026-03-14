using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.User.Services;

namespace SynapseEnqueteManagement.UI.User.Controllers;

public class ProjectLandingPageController : Controller
{
    private readonly IProjectApiService _projectApiService;
    private readonly ISessionService _sessionService;
    private readonly IQuestionnaireApiService _questionnaireApiService;

    public ProjectLandingPageController(
        IProjectApiService projectApiService, 
        ISessionService sessionService,
        IQuestionnaireApiService questionnaireApiService)
    {
        _projectApiService = projectApiService;
        _sessionService = sessionService;
        _questionnaireApiService = questionnaireApiService;
    }
    
    public async Task<IActionResult> Index(int projectId)
    {
        var project  = await _projectApiService.GetProjectAsync(projectId);
        if (project is null) return View("NotFound");

        if (project.ProjectState != "Active") return View("Inactive");

        var token = _sessionService.GetToken(projectId);
        if (!string.IsNullOrWhiteSpace(token)
            && await _questionnaireApiService.HasParticipantRespondedAsync(projectId, token))
        {
            return RedirectToAction("Index", "Category", new { projectId });
        }
        
        return View(project);
    }
}
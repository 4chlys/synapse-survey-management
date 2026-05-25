using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.User.Extensions;
using SynapseEnqueteManagement.UI.User.Services.Project;
using SynapseEnqueteManagement.UI.User.Services.Session;
using SynapseEnqueteManagement.UI.User.Services.Survey;

namespace SynapseEnqueteManagement.UI.User.Controllers;

public class LandingPageController : Controller
{
    private readonly IProjectApiService _projectApiService;
    private readonly ISessionService _sessionService;
    private readonly IQuestionnaireApiService _questionnaireApiService;

    public LandingPageController(
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
            return RedirectToAction("CategoryList", "Category", new { projectId });
        }
        
        await _projectApiService.TrackViewAsync(projectId);
        this.SetLayoutData(project, projectId);
        return View(project);
    }
}
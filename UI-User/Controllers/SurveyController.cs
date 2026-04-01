using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.User.Extensions;
using SynapseEnqueteManagement.UI.User.FormStrategies;
using SynapseEnqueteManagement.UI.User.Models.Survey;
using SynapseEnqueteManagement.UI.User.Services.Project;
using SynapseEnqueteManagement.UI.User.Services.Session;
using SynapseEnqueteManagement.UI.User.Services.Survey;

namespace SynapseEnqueteManagement.UI.User.Controllers;

public class SurveyController(
    IQuestionnaireApiService questionnaireService,
    ISessionService sessionService,
    IProjectApiService projectApiService,
    IFormStrategyFactory formStrategyFactory)
    : Controller
{
    public async Task<IActionResult> Index(int projectId)
    {
        var project = await projectApiService.GetProjectAsync(projectId);
        if (project is null)
            return View("NotFound");

        if (project.ProjectState != "Active")
            return View("Inactive");

        var token = sessionService.GetToken(projectId);
        if (!string.IsNullOrWhiteSpace(token)
            && await questionnaireService.HasParticipantRespondedAsync(projectId, token))
        {
            return RedirectToAction("Index", "Category", new { projectId });
        }

        var model = await questionnaireService.GetSurveyAsync(projectId);
        if (model is null)
            return View("NotFound");

        // Load existing responses for resume support
        if (!string.IsNullOrWhiteSpace(token))
        {
            Console.WriteLine("Loading responses");
            var existingResponses = await questionnaireService.GetResponsesAsync(projectId, token);

            if (existingResponses is null)
            {
                // null = participant not found in DB, stale cookie
                sessionService.ClearToken(projectId);
                Console.WriteLine("Session cleared");
            }
            else
            {
                // empty dict = participant exists, just no answers yet — still a valid session
                model.ExistingResponses = existingResponses;
                model.HasSession = true;
            }
        }

        this.SetLayoutData(project, projectId);
        var strategy = formStrategyFactory.Create(project.FormType);
        ViewData["FormLayout"] = strategy.GetLayoutData();
        return View(strategy.GetViewName(), model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int projectId)
    {
        var token = sessionService.GetToken(projectId);
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest("No session found.");

        await questionnaireService.SubmitAsync(projectId, token);

        return RedirectToAction("ThankYou", new { projectId });
    }

    public async Task<IActionResult> ThankYou(int projectId)
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

using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.User.FormStrategies;
using SynapseEnqueteManagement.UI.User.Models;
using SynapseEnqueteManagement.UI.User.Services;

namespace SynapseEnqueteManagement.UI.User.Controllers;

public class SurveyController : Controller
{
    private readonly IQuestionnaireApiService _questionnaireApiService;
    private readonly ISessionService _sessionService;
    private readonly IProjectApiService _projectApiService;
    private readonly IFormStrategyFactory _formStrategyFactory;

    public SurveyController(
        IQuestionnaireApiService questionnaireApiService,
        ISessionService sessionService,
        IProjectApiService projectApiService,
        IFormStrategyFactory formStrategyFactory)
    {
        _questionnaireApiService = questionnaireApiService;
        _sessionService = sessionService;
        _projectApiService = projectApiService;
        _formStrategyFactory = formStrategyFactory;
    }

    public async Task<IActionResult> Index(int projectId)
    {
        var project = await _projectApiService.GetProjectAsync(projectId);
        if (project is null)
            return View("NotFound");

        if (project.ProjectState != "Active")
            return View("Inactive");

        // var token = _sessionService.GetToken(projectId);
        // if (!string.IsNullOrWhiteSpace(token)
        //     && await _questionnaireApiService.HasParticipantRespondedAsync(projectId, token))
        // {
        //     return RedirectToAction("Index", "Category", new { projectId });
        // }

        var model = await _questionnaireApiService.GetSurveyAsync(projectId);
        if (model is null)
            return View("NotFound");

        var strategy = _formStrategyFactory.Create(project.FormType);
        ViewData["FormLayout"] = strategy.GetLayoutData();
        return View(strategy.GetViewName(), model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int projectId, IFormCollection form)
    {
        var answers = new Dictionary<int, string>();

        foreach (var key in form.Keys)
        {
            if (!key.StartsWith("q_")) continue;
            if (!int.TryParse(key[2..], out var questionId)) continue;

            var value = string.Join(", ", form[key].Where(v => !string.IsNullOrWhiteSpace(v)));
            if (!string.IsNullOrWhiteSpace(value))
                answers[questionId] = value;
        }

        var participantToken = await _questionnaireApiService.SubmitSurveyAsync(projectId, answers);

        _sessionService.SetToken(projectId, participantToken);

        return RedirectToAction("ThankYou", new { projectId });
    }

    public async Task<IActionResult> ThankYou(int projectId)
    {
        var project = await _projectApiService.GetProjectAsync(projectId);
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

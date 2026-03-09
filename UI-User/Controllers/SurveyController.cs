using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.User.Services;

namespace SynapseEnqueteManagement.UI.User.Controllers;

public class SurveyController : Controller
{
    private readonly IQuestionnaireApiService _questionnaireService;
    private readonly ISessionService _sessionService;
    private readonly ProjectApiService _projectApiService;

    public SurveyController(
        IQuestionnaireApiService questionnaireService,
        ISessionService sessionService,
        ProjectApiService projectApiService)
    {
        _questionnaireService = questionnaireService;
        _sessionService = sessionService;
        _projectApiService = projectApiService;
    }

    public async Task<IActionResult> Index(int projectId)
    {
        var project = await _projectApiService.GetProjectAsync(projectId);
        if (project is null)
            return View("NotFound");

        if (project.ProjectState != "Active")
            return View("Inactive");

        var token = _sessionService.GetToken(projectId);
        if (!string.IsNullOrWhiteSpace(token)
            && await _questionnaireService.HasParticipantRespondedAsync(projectId, token))
        {
            return RedirectToAction("Index", "Category", new { projectId });
        }

        var model = await _questionnaireService.GetSurveyAsync(projectId);
        if (model is null)
            return View("NotFound");

        return View(model);
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

        var participantToken = await _questionnaireService.SubmitSurveyAsync(projectId, answers);

        _sessionService.SetToken(projectId, participantToken);

        return RedirectToAction("Index", "Category", new { projectId });
    }

    public IActionResult ThankYou()
    {
        return View();
    }
}

using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.User.Services;

namespace SynapseEnqueteManagement.UI.User.Controllers;

public class SurveyController : Controller
{
    // TODO: Replace with real projectID by routing/env
    private const int ProjectId = 1;
    private readonly IQuestionnaireApiService _questionnaireService;
    private readonly ISessionService _sessionService;

    public SurveyController(IQuestionnaireApiService questionnaireService, ISessionService sessionService)
    {
        _questionnaireService = questionnaireService;
        _sessionService = sessionService;
    }

    public async Task<IActionResult> Index()
    {
        var token = _sessionService.GetToken(ProjectId);
        if (!string.IsNullOrWhiteSpace(token)
            && await _questionnaireService.HasParticipantRespondedAsync(token))
        {
            return RedirectToAction("Index", "Ideation");
        }

        var model = await _questionnaireService.GetSurveyAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(IFormCollection form)
    {
        var answers = new Dictionary<int, string>();

        foreach (var key in form.Keys)
        {
            if (!key.StartsWith("q_")) continue;
            if (!int.TryParse(key[2..], out var questionId)) continue;

            // Multiple checkbox values get joined with commas
            var value = string.Join(", ", form[key].Where(v => !string.IsNullOrWhiteSpace(v)));
            if (!string.IsNullOrWhiteSpace(value))
                answers[questionId] = value;
        }

        var participantToken = await _questionnaireService.SubmitSurveyAsync(answers);

        _sessionService.SetToken(ProjectId, participantToken);

        return RedirectToAction("Index", "Ideation");
    }

    public IActionResult ThankYou()
    {
        return View();
    }
}

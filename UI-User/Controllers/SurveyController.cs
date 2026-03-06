using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.User.Services;

namespace SynapseEnqueteManagement.UI.User.Controllers;

public class SurveyController : Controller
{
    // TODO: Replace with real projectID by routing/env
    private const int ProjectId = 1;
    private readonly IQuestionnaireService _questionnaireService;
    private readonly ISessionService _sessionService;

    public SurveyController(IQuestionnaireService questionnaireService, ISessionService sessionService)
    {
        _questionnaireService = questionnaireService;
        _sessionService = sessionService;
    }

    public IActionResult Index()
    {
        var token = _sessionService.GetToken(ProjectId);
        if (!string.IsNullOrWhiteSpace(token)
            && _questionnaireService.HasParticipantResponded(token))
        {
            return RedirectToAction("Index", "Ideation");
        }

        var model = _questionnaireService.GetSurvey();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Submit(IFormCollection form)
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

        var participantToken = _questionnaireService.SubmitSurvey(answers);

        _sessionService.SetToken(ProjectId, participantToken);

        return RedirectToAction("Index", "Ideation");
    }

    public IActionResult ThankYou()
    {
        return View();
    }
}

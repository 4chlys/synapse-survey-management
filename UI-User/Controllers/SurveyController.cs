using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.User.Services;

namespace SynapseEnqueteManagement.UI.User.Controllers;

public class SurveyController : Controller
{
    private const string TokenCookieName = "ParticipantToken";
    private readonly IQuestionnaireService _questionnaireService;

    public SurveyController(IQuestionnaireService questionnaireService)
    {
        _questionnaireService = questionnaireService;
    }

    public IActionResult Index()
    {
        // Cookie is only set after a successful submit, so its presence = already responded
        if (Request.Cookies.ContainsKey(TokenCookieName))
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

        // Store the token in a cookie so returning visitors skip the survey
        Response.Cookies.Append(TokenCookieName, participantToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(365)
        });

        return RedirectToAction("Index", "Ideation");
    }

    public IActionResult ThankYou()
    {
        return View();
    }
}

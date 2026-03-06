using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.User.Services;

namespace SynapseEnqueteManagement.UI.User.Controllers;

public class SurveyController : Controller
{
    private readonly IQuestionnaireService _questionnaireService;

    public SurveyController(IQuestionnaireService questionnaireService)
    {
        _questionnaireService = questionnaireService;
    }

    public IActionResult Index()
    {
        var model = _questionnaireService.GetSurvey();
        return View(model);
    }

    [HttpPost]
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

        _questionnaireService.SubmitSurvey(answers);
        return RedirectToAction("ThankYou");
    }

    public IActionResult ThankYou()
    {
        return View();
    }
}

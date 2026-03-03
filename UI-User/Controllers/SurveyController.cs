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
    public IActionResult Submit()
    {
        // Phase 2: forward form values to API
        return RedirectToAction("ThankYou");
    }

    public IActionResult ThankYou()
    {
        return View();
    }
}

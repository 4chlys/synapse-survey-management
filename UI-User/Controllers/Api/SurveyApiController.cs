using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SynapseEnqueteManagement.UI.User.Services.Session;
using SynapseEnqueteManagement.UI.User.Services.Survey;

namespace SynapseEnqueteManagement.UI.User.Controllers.Api;

[ApiController]
[Route("{projectId:int}/api/[controller]")]
public class SurveyApiController(
    IQuestionnaireApiService questionnaireService,
    ISessionService sessionService) : ControllerBase
{
    [HttpPost("participants")]
    [EnableRateLimiting("participant-creation")]
    public async Task<ActionResult> StartSession(int projectId)
    {
        var supportedCultures = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "nl", "en" };
        var culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var languageCode = supportedCultures.Contains(culture) ? culture : "nl";

        var token = await questionnaireService.StartSessionAsync(projectId, languageCode);
        sessionService.SetToken(projectId, token);
        return Ok(new { participantToken = token });
    }

    [HttpPost("responses")]
    public async Task<ActionResult> SaveResponse(int projectId, SaveResponseRequest request)
    {
        var token = sessionService.GetToken(projectId);
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();

        await questionnaireService.SaveResponseAsync(projectId, request.QuestionId, request.Value, token);
        return Ok();
    }

    [HttpGet("next-question")]
    public async Task<ActionResult> GetNextQuestion(int projectId, [FromQuery] int currentQuestionId)
    {
        var token = sessionService.GetToken(projectId);
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();

        var questionId = await questionnaireService.GetNextQuestionAsync(projectId, currentQuestionId, token);
        return Ok(new { questionId });
    }

    [HttpGet("previous-question")]
    public async Task<ActionResult> GetPreviousQuestion(int projectId, [FromQuery] int currentQuestionId)
    {
        var token = sessionService.GetToken(projectId);
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();

        var questionId = await questionnaireService.GetPreviousQuestionAsync(projectId, currentQuestionId, token);
        return Ok(new { questionId });
    }

    [HttpGet("resume-question")]
    public async Task<ActionResult> GetResumeQuestion(int projectId)
    {
        var token = sessionService.GetToken(projectId);
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();

        var questionId = await questionnaireService.GetResumeQuestionAsync(projectId, token);
        return Ok(new { questionId });
    }

    [HttpGet("visible-path")]
    public async Task<ActionResult> GetVisiblePath(int projectId)
    {
        var token = sessionService.GetToken(projectId);
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();

        var questionIds = await questionnaireService.GetVisiblePathAsync(projectId, token);
        return Ok(new { questionIds });
    }

    [HttpGet("responses")]
    public async Task<ActionResult> GetResponses(int projectId)
    {
        var token = sessionService.GetToken(projectId);
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();

        var responses = await questionnaireService.GetResponsesAsync(projectId, token);
        return Ok(responses);
    }

    [HttpPost("submit")]
    public async Task<ActionResult> Submit(int projectId)
    {
        var token = sessionService.GetToken(projectId);
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();

        await questionnaireService.SubmitAsync(projectId, token);
        return Ok();
    }

    [HttpDelete("responses")]
    public async Task<ActionResult> RemoveAllResponses(int projectId)
    {
        var token = sessionService.GetToken(projectId);
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();

        await questionnaireService.RemoveAllResponsesAsync(projectId, token);
        return NoContent();
    }
}

public record SaveResponseRequest(int QuestionId, string Value);
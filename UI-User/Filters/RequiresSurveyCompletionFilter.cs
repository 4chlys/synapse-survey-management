using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SynapseEnqueteManagement.UI.User.Services.Session;
using SynapseEnqueteManagement.UI.User.Services.Survey;

namespace SynapseEnqueteManagement.UI.User.Filters;

public class RequiresSurveyCompletionFilter(
    ISessionService sessionService,
    IQuestionnaireApiService questionnaireService) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ActionArguments.TryGetValue("projectId", out var projectIdObj)
            || projectIdObj is not int projectId)
        {
            await next();
            return;
        }

        var token = sessionService.GetToken(projectId);
        var hasResponded = !string.IsNullOrWhiteSpace(token)
                           && await questionnaireService.HasParticipantRespondedAsync(projectId, token);

        if (!hasResponded)
        {
            context.Result = new RedirectToActionResult("Index", "ProjectLandingPage", new { projectId });
            return;
        }

        await next();
    }
}

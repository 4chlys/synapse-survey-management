using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SynapseEnqueteManagement.UI.User.Services.Session;

namespace SynapseEnqueteManagement.UI.User.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireParticipantSessionAttribute : Attribute, IAsyncActionFilter
{
    // When true, missing-session redirects to the landing page instead of returning 401.
    // Use for MVC POST actions; leave default (false) for API controllers consumed via fetch.
    public bool Redirect { get; init; }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionDescriptor.EndpointMetadata.OfType<SkipParticipantSessionAttribute>().Any())
        {
            await next();
            return;
        }

        if (!context.ActionArguments.TryGetValue("projectId", out var raw) || raw is not int projectId)
        {
            context.Result = new BadRequestObjectResult("Missing projectId.");
            return;
        }

        var sessionService = context.HttpContext.RequestServices.GetRequiredService<ISessionService>();
        if (string.IsNullOrWhiteSpace(sessionService.GetToken(projectId)))
        {
            context.Result = Redirect
                ? new RedirectToActionResult("Index", "LandingPage", new { projectId })
                : new UnauthorizedObjectResult("Participant session required.");
            return;
        }

        await next();
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SkipParticipantSessionAttribute : Attribute { }

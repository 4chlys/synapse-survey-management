using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SynapseEnqueteManagement.UI.Admin.Filters;

// Enforces that the caller (a ProjectPlatformAdmin) only acts on projects belonging
// to their own ProjectPlatform. No SystemAdmin bypass: SystemAdmins are not supposed
// to access project content (ideas, answers, participants, settings) — only platform
// metadata and billing.
public sealed class RequireProjectPlatformOwnershipAttribute : TypeFilterAttribute
{
    public RequireProjectPlatformOwnershipAttribute() : base(typeof(RequireProjectPlatformOwnershipFilter)) { }
}

public sealed class RequireProjectPlatformOwnershipFilter(IProjectOwnershipLookup lookup) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ActionArguments.TryGetValue("projectId", out var raw) || raw is null)
        {
            await next();
            return;
        }

        if (raw is not int projectId)
        {
            await next();
            return;
        }

        var ownership = await lookup.ResolveAsync(context.HttpContext.User, projectId);

        if (ownership.CallerPlatformId is null)
        {
            context.Result = new ForbidResult();
            return;
        }

        if (!ownership.ProjectExists)
        {
            context.Result = new NotFoundResult();
            return;
        }

        if (!ownership.Matches)
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}

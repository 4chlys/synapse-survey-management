using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using SynapseEnqueteManagement.UI.Admin.Filters;

namespace SynapseEnqueteManagement.UI.Admin.Tests.Unit;

// Direct tests for the action filter that enforces project-platform ownership.
// Uses a hand-rolled stub IProjectOwnershipLookup so we don't have to construct
// UserManager<ApplicationUser> or stand up the host.
public class RequireProjectPlatformOwnershipFilterTests
{
    [Fact]
    public async Task ProjectFromDifferentPlatform_SetsForbidResult()
    {
        var lookup = new StubLookup(callerPlatformId: 1, projectPlatformId: 99);
        var filter = new RequireProjectPlatformOwnershipFilter(lookup);
        var context = MakeContext(new Dictionary<string, object> { ["projectId"] = 5 });

        var nextCalled = false;
        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null);
        });

        Assert.IsType<ForbidResult>(context.Result);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task ProjectFromSamePlatform_CallsNext()
    {
        var lookup = new StubLookup(callerPlatformId: 1, projectPlatformId: 1);
        var filter = new RequireProjectPlatformOwnershipFilter(lookup);
        var context = MakeContext(new Dictionary<string, object> { ["projectId"] = 5 });

        var nextCalled = false;
        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null);
        });

        Assert.Null(context.Result);
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task MissingProject_SetsNotFoundResult()
    {
        var lookup = new StubLookup(callerPlatformId: 1, projectPlatformId: null);
        var filter = new RequireProjectPlatformOwnershipFilter(lookup);
        var context = MakeContext(new Dictionary<string, object> { ["projectId"] = 5 });

        await filter.OnActionExecutionAsync(context, () =>
            Task.FromResult<ActionExecutedContext>(null));

        Assert.IsType<NotFoundResult>(context.Result);
    }

    [Fact]
    public async Task UserHasNoPlatform_SetsForbidResult()
    {
        var lookup = new StubLookup(callerPlatformId: null, projectPlatformId: 1);
        var filter = new RequireProjectPlatformOwnershipFilter(lookup);
        var context = MakeContext(new Dictionary<string, object> { ["projectId"] = 5 });

        await filter.OnActionExecutionAsync(context, () =>
            Task.FromResult<ActionExecutedContext>(null));

        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public async Task ActionWithoutProjectId_PassesThrough()
    {
        var lookup = new StubLookup(callerPlatformId: 1, projectPlatformId: 1);
        var filter = new RequireProjectPlatformOwnershipFilter(lookup);
        var context = MakeContext(new Dictionary<string, object>());

        var nextCalled = false;
        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null);
        });

        Assert.Null(context.Result);
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task NullProjectIdArgument_PassesThrough()
    {
        // Optional projectId (e.g. CheckShortForm) bound as int? null — should not block.
        var lookup = new StubLookup(callerPlatformId: 1, projectPlatformId: 99);
        var filter = new RequireProjectPlatformOwnershipFilter(lookup);
        var context = MakeContext(new Dictionary<string, object> { ["projectId"] = null });

        var nextCalled = false;
        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null);
        });

        Assert.Null(context.Result);
        Assert.True(nextCalled);
    }

    private static ActionExecutingContext MakeContext(IDictionary<string, object> actionArguments)
    {
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity("Test")) };
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ControllerActionDescriptor());
        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            actionArguments,
            controller: null);
    }

    private sealed class StubLookup(int? callerPlatformId, int? projectPlatformId) : IProjectOwnershipLookup
    {
        public Task<ProjectOwnership> ResolveAsync(ClaimsPrincipal user, int projectId) =>
            Task.FromResult(new ProjectOwnership(callerPlatformId, projectPlatformId));
    }
}

namespace SynapseEnqueteManagement.UI.User.Tests.Filters;

public class RequireParticipantSessionAttributeTests
{
    private readonly Mock<ISessionService> _sessionService = new();
    private readonly IServiceProvider _services;

    public RequireParticipantSessionAttributeTests()
    {
        var sp = new Mock<IServiceProvider>();
        sp.Setup(s => s.GetService(typeof(ISessionService))).Returns(_sessionService.Object);
        _services = sp.Object;
    }

    [Fact]
    public async Task SkipAttribute_OnAction_BypassesAllChecks()
    {
        var attr = new RequireParticipantSessionAttribute();
        var ctx = BuildContext(
            actionArgs: new Dictionary<string, object>(),
            metadata: new List<object> { new SkipParticipantSessionAttribute() });
        var nextCalled = false;

        await attr.OnActionExecutionAsync(ctx, () => { nextCalled = true; return Task.FromResult(BuildExecuted(ctx)); });

        Assert.True(nextCalled);
        Assert.Null(ctx.Result);
        _sessionService.Verify(s => s.GetToken(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task MissingProjectId_ReturnsBadRequest()
    {
        var attr = new RequireParticipantSessionAttribute();
        var ctx = BuildContext(actionArgs: new Dictionary<string, object>());
        var nextCalled = false;

        await attr.OnActionExecutionAsync(ctx, () => { nextCalled = true; return Task.FromResult(BuildExecuted(ctx)); });

        Assert.False(nextCalled);
        Assert.IsType<BadRequestObjectResult>(ctx.Result);
    }

    [Fact]
    public async Task MissingToken_DefaultMode_ReturnsUnauthorized()
    {
        _sessionService.Setup(s => s.GetToken(42)).Returns((string)null);
        var attr = new RequireParticipantSessionAttribute();
        var ctx = BuildContext(actionArgs: new Dictionary<string, object> { ["projectId"] = 42 });
        var nextCalled = false;

        await attr.OnActionExecutionAsync(ctx, () => { nextCalled = true; return Task.FromResult(BuildExecuted(ctx)); });

        Assert.False(nextCalled);
        Assert.IsType<UnauthorizedObjectResult>(ctx.Result);
    }

    [Fact]
    public async Task MissingToken_RedirectMode_RedirectsToLandingPage()
    {
        _sessionService.Setup(s => s.GetToken(42)).Returns("");
        var attr = new RequireParticipantSessionAttribute { Redirect = true };
        var ctx = BuildContext(actionArgs: new Dictionary<string, object> { ["projectId"] = 42 });

        await attr.OnActionExecutionAsync(ctx, () => Task.FromResult(BuildExecuted(ctx)));

        var redirect = Assert.IsType<RedirectToActionResult>(ctx.Result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("LandingPage", redirect.ControllerName);
        Assert.Equal(42, redirect.RouteValues["projectId"]);
    }

    [Fact]
    public async Task ValidToken_CallsNext()
    {
        _sessionService.Setup(s => s.GetToken(42)).Returns("a-real-token");
        var attr = new RequireParticipantSessionAttribute();
        var ctx = BuildContext(actionArgs: new Dictionary<string, object> { ["projectId"] = 42 });
        var nextCalled = false;

        await attr.OnActionExecutionAsync(ctx, () => { nextCalled = true; return Task.FromResult(BuildExecuted(ctx)); });

        Assert.True(nextCalled);
        Assert.Null(ctx.Result);
    }

    private ActionExecutingContext BuildContext(
        IDictionary<string, object> actionArgs,
        IList<object> metadata = null)
    {
        var httpContext = new DefaultHttpContext { RequestServices = _services };
        var actionDescriptor = new ActionDescriptor { EndpointMetadata = metadata ?? new List<object>() };
        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor, new ModelStateDictionary());
        return new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), actionArgs, controller: new object());
    }

    private static ActionExecutedContext BuildExecuted(ActionExecutingContext ctx)
        => new(ctx, new List<IFilterMetadata>(), new object());
}

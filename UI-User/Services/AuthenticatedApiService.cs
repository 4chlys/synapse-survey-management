using SynapseEnqueteManagement.UI.User.Services.Session;

namespace SynapseEnqueteManagement.UI.User.Services;

public abstract class AuthenticatedApiService(HttpClient httpClient, ISessionService sessionService)
{
    private const string TokenHeader = "X-Participant-Token";

    protected HttpClient HttpClient { get; } = httpClient;
    protected ISessionService SessionService { get; } = sessionService;

    protected void AddTokenHeader(HttpRequestMessage request, int projectId)
    {
        var token = SessionService.GetToken(projectId);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Add(TokenHeader, token);
    }
}

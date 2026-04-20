using System.Net.Http.Json;
using SynapseEnqueteManagement.UI.User.Services.Session;

namespace SynapseEnqueteManagement.UI.User.Services.Project;

public class ParticipantService(HttpClient httpClient, ISessionService sessionService)
    : AuthenticatedApiService(httpClient, sessionService)
{
    public async Task SaveContactInfoAsync(int projectId, string email)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch,
            $"api/projects/{projectId}/participants/me/contact");
        request.Content = JsonContent.Create(new { Email = email });

        AddTokenHeader(request, projectId);

        await HttpClient.SendAsync(request);
    }
}

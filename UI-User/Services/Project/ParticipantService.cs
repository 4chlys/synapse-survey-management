using System.Net.Http.Json;
using SynapseEnqueteManagement.UI.User.Services.Session;

namespace SynapseEnqueteManagement.UI.User.Services.Project;

public class ParticipantService(HttpClient httpClient, ISessionService sessionService)
{
    private const string TokenHeader = "X-Participant-Token";

    public async Task SaveContactInfoAsync(int projectId, string email, string phoneNumber)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch,
            $"api/projects/{projectId}/participants/me/contact");
        request.Content = JsonContent.Create(new { Email = email, PhoneNumber = phoneNumber });

        var token = sessionService.GetToken(projectId);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Add(TokenHeader, token);

        await httpClient.SendAsync(request);
    }
}

using System.Net.Http.Json;
using SynapseEnqueteManagement.UI.User.Services.Session;

namespace SynapseEnqueteManagement.UI.User.Services.Moderation;

public class ModerationService(HttpClient httpClient, ISessionService sessionService)
    : AuthenticatedApiService(httpClient, sessionService)
{

    public async Task<ModerationResponse> CheckRegexAsync(int projectId, string text, string contentType = "idea")
    {
        var response = await HttpClient.PostAsJsonAsync(
            $"api/projects/{projectId}/moderation/regex", new { text, contentType });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ModerationResponse>()
               ?? new ModerationResponse();
    }

    public async Task<ModerationResponse> CheckAiAsync(int projectId, string text, string contentType = "idea")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"api/projects/{projectId}/moderation/ai");
        request.Content = JsonContent.Create(new { text, contentType });
        AddTokenHeader(request, projectId);
        var response = await HttpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ModerationResponse>()
               ?? new ModerationResponse();
    }

    public async Task<SuggestResponse> SuggestAsync(int projectId, string text, string language = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"api/projects/{projectId}/moderation/suggest");
        request.Content = JsonContent.Create(new { text, language });
        AddTokenHeader(request, projectId);
        var response = await HttpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SuggestResponse>()
               ?? new SuggestResponse();
    }
}

public record ModerationResponse(
    bool IsAcceptable = true,
    string Reason = null,
    string FlaggedBy = null,
    string ModerationToken = null);

public record SuggestResponse(string Suggestion = null, string ModerationToken = null);

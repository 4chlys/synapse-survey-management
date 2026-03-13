using System.Net.Http.Json;

namespace SynapseEnqueteManagement.UI.User.Services;

public class ModerationService(HttpClient httpClient, ISessionService sessionService)
{
    public async Task<ModerationResponse> CheckRegexAsync(int projectId, string text)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"api/projects/{projectId}/moderation/regex", new { text });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ModerationResponse>()
               ?? new ModerationResponse();
    }

    public async Task<ModerationResponse> CheckAiAsync(int projectId, string text)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"api/projects/{projectId}/moderation/ai");
        request.Content = JsonContent.Create(new { text });
        AddTokenHeader(request, projectId);
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ModerationResponse>()
               ?? new ModerationResponse();
    }

    public async Task<SuggestResponse> SuggestAsync(int projectId, string text)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"api/projects/{projectId}/moderation/suggest");
        request.Content = JsonContent.Create(new { text });
        AddTokenHeader(request, projectId);
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SuggestResponse>()
               ?? new SuggestResponse();
    }

    private const string TokenHeader = "X-Participant-Token";

    private void AddTokenHeader(HttpRequestMessage request, int projectId)
    {
        var token = sessionService.GetToken(projectId);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Add(TokenHeader, token);
    }
}

public class ModerationResponse
{
    public bool IsAcceptable { get; set; } = true;
    public string Reason { get; set; }
    public string FlaggedBy { get; set; }
    public string ModerationToken { get; set; }
}

public class SuggestResponse
{
    public string Suggestion { get; set; }
    public string ModerationToken { get; set; }
}

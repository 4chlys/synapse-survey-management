using System.Net.Http.Json;

namespace SynapseEnqueteManagement.UI.User.Services;

public class ModerationService(HttpClient httpClient)
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
        var response = await httpClient.PostAsJsonAsync(
            $"api/projects/{projectId}/moderation/ai", new { text });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ModerationResponse>()
               ?? new ModerationResponse();
    }

    public async Task<SuggestResponse> SuggestAsync(int projectId, string text)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"api/projects/{projectId}/moderation/suggest", new { text });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SuggestResponse>()
               ?? new SuggestResponse();
    }
}

public class ModerationResponse
{
    public bool IsAcceptable { get; set; } = true;
    public string Reason { get; set; }
    public string FlaggedBy { get; set; }
}

public class SuggestResponse
{
    public string Suggestion { get; set; }
}

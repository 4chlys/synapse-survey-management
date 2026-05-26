using System.Net;
using System.Net.Http.Json;
using SynapseEnqueteManagement.UI.Admin.Models.Project;

namespace SynapseEnqueteManagement.UI.Admin.Services.Project;

public class EmojiAdminService(HttpClient httpClient)
{
    // Shape of the JSON the API returns for both the catalogue and the
    // per-project emoji endpoints. Private: callers only see the merged view model.
    private record EmojiResponse(int EmojiId, string Symbol, string Name);

    // Combines the global emoji catalogue with the project's enabled set so each
    // emoji carries an IsEnabled flag. The API has no single endpoint for this.
    public async Task<List<EmojiAdminViewModel>> GetEmojisForProjectAsync(int projectId)
    {
        var allResponse = await httpClient.GetAsync("api/emojis");
        if (!allResponse.IsSuccessStatusCode)
            return new List<EmojiAdminViewModel>();
        var all = await allResponse.Content.ReadFromJsonAsync<List<EmojiResponse>>()
                  ?? new List<EmojiResponse>();

        var projectResponse = await httpClient.GetAsync($"api/projects/{projectId}/emojis");
        var enabled = projectResponse.IsSuccessStatusCode
            ? await projectResponse.Content.ReadFromJsonAsync<List<EmojiResponse>>() ?? new List<EmojiResponse>()
            : new List<EmojiResponse>();
        var enabledIds = enabled.Select(e => e.EmojiId).ToHashSet();

        return all.Select(e => new EmojiAdminViewModel
        {
            EmojiId = e.EmojiId,
            Symbol = e.Symbol,
            Name = e.Name,
            IsEnabled = enabledIds.Contains(e.EmojiId)
        }).ToList();
    }

    public async Task EnableEmojiAsync(int projectId, int emojiId)
    {
        // No request body: both IDs live in the route.
        var response = await httpClient.PostAsync($"api/projects/{projectId}/emojis/{emojiId}", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task DisableEmojiAsync(int projectId, int emojiId)
    {
        var response = await httpClient.DeleteAsync($"api/projects/{projectId}/emojis/{emojiId}");
        // 404 means the emoji is already not enabled for this project — the desired
        // end state. Treat it as a no-op rather than an error.
        if (response.StatusCode == HttpStatusCode.NotFound) return;
        response.EnsureSuccessStatusCode();
    }
}

using System.Net.Http.Json;

namespace SynapseEnqueteManagement.UI.Admin.Services.Ai;

public sealed record GlobalPromptsDto(
    string Moderation        = "",
    string CommentModeration = "",
    string Rewrite           = "",
    string ImageModeration   = "",
    string IdeaSupport       = "",
    string CommentSupport    = "",
    string QuestionSupport   = "");

public sealed record ModelAvailabilityItem(string Id, bool IsAvailable);

public class AiAdminService(HttpClient httpClient)
{
    public async Task<Dictionary<string, bool>> GetModelAvailabilityAsync()
    {
        try
        {
            var items = await httpClient.GetFromJsonAsync<List<ModelAvailabilityItem>>("api/admin/ai/available-models");
            return (items ?? []).ToDictionary(x => x.Id, x => x.IsAvailable, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return [];
        }
    }

    public async Task<HashSet<string>> GetAvailableModelIdsAsync()
    {
        var availability = await GetModelAvailabilityAsync();
        return availability.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<GlobalPromptsDto> GetGlobalPromptsAsync()
    {
        try
        {
            return await httpClient.GetFromJsonAsync<GlobalPromptsDto>("api/admin/ai/global-prompts")
                   ?? new GlobalPromptsDto();
        }
        catch
        {
            return new GlobalPromptsDto();
        }
    }
}
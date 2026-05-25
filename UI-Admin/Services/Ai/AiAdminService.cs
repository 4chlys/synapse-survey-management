using System.Net.Http.Json;

namespace SynapseEnqueteManagement.UI.Admin.Services.Ai;

public sealed record GlobalPromptsDto(
    string Moderation        = "",
    string CommentModeration = "",
    string Rewrite           = "",
    string ImageModeration   = "",
    string IdeaSupport       = "",
    string CommentSupport    = "",
    string QuestionSupport   = "",
    string IdeaSummary       = "");

public class AiAdminService(HttpClient httpClient)
{
    public async Task<HashSet<string>> GetAvailableModelIdsAsync()
    {
        try
        {
            var ids = await httpClient.GetFromJsonAsync<List<string>>("api/admin/ai/available-models");
            return (ids ?? []).ToHashSet();
        }
        catch
        {
            return [];
        }
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
using System.Net.Http.Json;

namespace SynapseEnqueteManagement.UI.Admin.Services.Ai;

public sealed class GlobalPromptsDto
{
    public string Moderation        { get; init; } = "";
    public string CommentModeration { get; init; } = "";
    public string Rewrite           { get; init; } = "";
    public string ImageModeration   { get; init; } = "";
    public string IdeaSupport       { get; init; } = "";
    public string CommentSupport    { get; init; } = "";
    public string QuestionSupport   { get; init; } = "";
    public string IdeaSummary       { get; init; } = "";
}

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
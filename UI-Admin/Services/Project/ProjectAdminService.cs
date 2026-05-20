using System.Net;
using SynapseEnqueteManagement.UI.Admin.Models.Project;
namespace SynapseEnqueteManagement.UI.Admin.Services.Project;

public class ProjectAdminService(HttpClient httpClient)
{
    private record ApiErrorBody(string Error);

    // 400 from the API carries a localised message in {"error": "..."}.
    // Surface it as ApiValidationException so the controller can show it
    // in the form instead of letting EnsureSuccessStatusCode wipe the body.
    private static async Task EnsureSuccessOrApiValidationAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var body = await response.Content.ReadFromJsonAsync<ApiErrorBody>();
            if (!string.IsNullOrWhiteSpace(body?.Error))
                throw new ApiValidationException(body.Error);
        }
        response.EnsureSuccessStatusCode();
    }

    private record ProjectTranslationPayload
    {
        public string LanguageCode { get; init; }
        public string Name { get; init; }
        public string Topic { get; init; }
        public string Introduction { get; init; }
    }

    private record ProjectRequest
    {
        public string ShortForm { get; init; }
        public string ImageUrl { get; init; }
        public string InteractionMode { get; init; }
        public string AiAssistance { get; init; }
        public int? MinAge { get; init; }
        public int? MaxAge { get; init; }
        public bool RequestContactInfo { get; init; }
        public bool AllowCrossLanguageIdeas { get; init; }
        public int IdeaPageSize { get; init; }
        public bool ExtraIdeasReplaceExisting { get; init; }
        public int MaxExtraIdeaRequests { get; init; }
        public int PlatformId { get; init; }
        public string DefaultLanguageCode { get; init; }
        public ICollection<string> SupportedLanguageCodes { get; init; } = new List<string>();
        public ICollection<ProjectTranslationPayload> Translations { get; init; } = new List<ProjectTranslationPayload>();
        public string Theme { get; init; } = "Default";
        public string PrimaryColor { get; init; }
        public string CustomCss { get; init; }
    }

    public async Task<List<ProjectAdminViewModel>> GetAllProjectsAsync(int? platformId = null)
    {
        var url = platformId.HasValue ? $"api/projects?platformId={platformId}" : "api/projects";
        var response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return new List<ProjectAdminViewModel>();

        return await response.Content.ReadFromJsonAsync<List<ProjectAdminViewModel>>()
               ?? new List<ProjectAdminViewModel>();
    }

    public async Task<ProjectAdminViewModel> GetProjectByIdAsync(int projectId)
    {
        var response = await httpClient.GetAsync($"api/projects/{projectId}");
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ProjectAdminViewModel>();
    }

    public async Task<ProjectAdminViewModel> CreateProjectAsync(string name, string topic, string introduction, string imageUrl, string interactionMode, string aiAssistance, int? minAge, int? maxAge, bool requestContactInfo, int platformId, string shortForm = null, int ideaPageSize = 5, bool extraIdeasReplaceExisting = false, int maxExtraIdeaRequests = 3, bool allowCrossLanguageIdeas = false, string theme = "Default", string primaryColor = null, string customCss = null, string languageCode = "nl")
    {
        var payload = new ProjectRequest { ShortForm = shortForm, ImageUrl = imageUrl, InteractionMode = interactionMode, AiAssistance = aiAssistance, MinAge = minAge, MaxAge = maxAge, RequestContactInfo = requestContactInfo, AllowCrossLanguageIdeas = allowCrossLanguageIdeas, IdeaPageSize = ideaPageSize, ExtraIdeasReplaceExisting = extraIdeasReplaceExisting, MaxExtraIdeaRequests = maxExtraIdeaRequests, PlatformId = platformId, Theme = theme, PrimaryColor = primaryColor, CustomCss = customCss, DefaultLanguageCode = languageCode, SupportedLanguageCodes = new List<string> { languageCode }, Translations = new List<ProjectTranslationPayload> { new() { LanguageCode = languageCode, Name = name, Topic = topic, Introduction = introduction } } };
        var response = await httpClient.PostAsJsonAsync("api/projects", payload);
        await EnsureSuccessOrApiValidationAsync(response);
        return await response.Content.ReadFromJsonAsync<ProjectAdminViewModel>();
    }

    public async Task<ProjectAdminViewModel> CopyProjectAsync(int projectId, string newName, int platformId, string newShortForm = null, string languageCode = "nl")
    {
        var payload = new { NewShortForm = newShortForm, PlatformId = platformId, Translations = new List<ProjectTranslationPayload> { new() { LanguageCode = languageCode, Name = newName } } };
        var response = await httpClient.PostAsJsonAsync($"api/projects/{projectId}/copy", payload);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProjectAdminViewModel>();
    }

    public async Task DeleteProjectAsync(int projectId)
    {
        var response = await httpClient.DeleteAsync($"api/projects/{projectId}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<ProjectAdminViewModel> UpdateProjectStateAsync(int projectId, string state)
    {
        var response = await httpClient.PatchAsJsonAsync($"api/projects/{projectId}/state", new { State = state });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProjectAdminViewModel>();
    }

    public async Task<ProjectAdminViewModel> UpdateProjectAsync(int projectId, string name, string topic, string introduction, string imageUrl, string interactionMode, string aiAssistance, int? minAge, int? maxAge, bool requestContactInfo, string shortForm = null, int ideaPageSize = 5, bool extraIdeasReplaceExisting = false, int maxExtraIdeaRequests = 3, bool allowCrossLanguageIdeas = false, string theme = "Default", string primaryColor = null, string customCss = null, string languageCode = "nl")
    {
        var payload = new ProjectRequest { ShortForm = shortForm, ImageUrl = imageUrl, InteractionMode = interactionMode, AiAssistance = aiAssistance, MinAge = minAge, MaxAge = maxAge, RequestContactInfo = requestContactInfo, AllowCrossLanguageIdeas = allowCrossLanguageIdeas, IdeaPageSize = ideaPageSize, ExtraIdeasReplaceExisting = extraIdeasReplaceExisting, MaxExtraIdeaRequests = maxExtraIdeaRequests, Theme = theme, PrimaryColor = primaryColor, CustomCss = customCss, DefaultLanguageCode = languageCode, SupportedLanguageCodes = new List<string> { languageCode }, Translations = new List<ProjectTranslationPayload> { new() { LanguageCode = languageCode, Name = name, Topic = topic, Introduction = introduction } } };
        var response = await httpClient.PutAsJsonAsync($"api/projects/{projectId}", payload);
        await EnsureSuccessOrApiValidationAsync(response);
        return await response.Content.ReadFromJsonAsync<ProjectAdminViewModel>();
    }

    public async Task<PlatformStats> GetPlatformStatsAsync(int platformId)
    {
        var response = await httpClient.GetAsync($"api/admin/platform-stats?platformId={platformId}");
        if (!response.IsSuccessStatusCode) return new PlatformStats();
        return await response.Content.ReadFromJsonAsync<PlatformStats>() ?? new PlatformStats();
    }

    public record PlatformStats
    {
        public int ActiveCount { get; init; }
        public int DraftCount { get; init; }
        public int ArchivedCount { get; init; }
        public int ParticipantCount { get; init; }
    }

    public async Task<bool> IsShortFormAvailableAsync(string shortForm, int? excludeProjectId)
    {
        var response = await httpClient.GetAsync($"api/projects/by-shortform/{Uri.EscapeDataString(shortForm)}");
        if (response.StatusCode == HttpStatusCode.NotFound)
            return true;
        if (!response.IsSuccessStatusCode)
            return true;
        var dto = await response.Content.ReadFromJsonAsync<ShortFormLookup>();
        return dto?.ProjectId == excludeProjectId;
    }

    private sealed record ShortFormLookup
    {
        public int ProjectId { get; init; }
    }
}

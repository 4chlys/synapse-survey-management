using System.Net;
using System.Text.Json;
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

    public async Task<ProjectAdminViewModel> CreateProjectAsync(ICollection<ProjectTranslationViewModel> translations, ICollection<string> supportedLanguageCodes, string defaultLanguageCode, string imageUrl, string interactionMode, int? minAge, int? maxAge, bool requestContactInfo, int platformId, string shortForm = null, int ideaPageSize = 5, bool extraIdeasReplaceExisting = false, int maxExtraIdeaRequests = 3, bool allowCrossLanguageIdeas = false, string theme = "Default", string primaryColor = null, string customCss = null)
    {
        var payload = new ProjectRequest { ShortForm = shortForm, ImageUrl = imageUrl, InteractionMode = interactionMode, MinAge = minAge, MaxAge = maxAge, RequestContactInfo = requestContactInfo, AllowCrossLanguageIdeas = allowCrossLanguageIdeas, IdeaPageSize = ideaPageSize, ExtraIdeasReplaceExisting = extraIdeasReplaceExisting, MaxExtraIdeaRequests = maxExtraIdeaRequests, PlatformId = platformId, Theme = theme, PrimaryColor = primaryColor, CustomCss = customCss, DefaultLanguageCode = defaultLanguageCode, SupportedLanguageCodes = supportedLanguageCodes, Translations = translations.Select(t => new ProjectTranslationPayload { LanguageCode = t.LanguageCode, Name = t.Name, Topic = t.Topic, Introduction = t.Introduction }).ToList() };
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

    private record ActivationProblemsBody(IReadOnlyList<ActivationProblemDto> Problems);

    public async Task<ProjectAdminViewModel> UpdateProjectStateAsync(int projectId, string state)
    {
        var response = await httpClient.PatchAsJsonAsync($"api/projects/{projectId}/state", new { State = state });
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ProjectAdminViewModel>();

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var json = await response.Content.ReadAsStringAsync();
            var body = JsonSerializer.Deserialize<ActivationProblemsBody>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (body?.Problems is { Count: > 0 })
                throw new ProjectActivationValidationException(body.Problems);

            var errorBody = JsonSerializer.Deserialize<ApiErrorBody>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (!string.IsNullOrWhiteSpace(errorBody?.Error))
                throw new ApiValidationException(errorBody.Error);
        }

        response.EnsureSuccessStatusCode();
        return null;
    }

    public async Task<ProjectAdminViewModel> UpdateProjectAsync(int projectId, ICollection<ProjectTranslationViewModel> translations, ICollection<string> supportedLanguageCodes, string defaultLanguageCode, string imageUrl, string interactionMode, int? minAge, int? maxAge, bool requestContactInfo, string shortForm = null, int ideaPageSize = 5, bool extraIdeasReplaceExisting = false, int maxExtraIdeaRequests = 3, bool allowCrossLanguageIdeas = false, string theme = "Default", string primaryColor = null, string customCss = null)
    {
        var payload = new ProjectRequest { ShortForm = shortForm, ImageUrl = imageUrl, InteractionMode = interactionMode, MinAge = minAge, MaxAge = maxAge, RequestContactInfo = requestContactInfo, AllowCrossLanguageIdeas = allowCrossLanguageIdeas, IdeaPageSize = ideaPageSize, ExtraIdeasReplaceExisting = extraIdeasReplaceExisting, MaxExtraIdeaRequests = maxExtraIdeaRequests, Theme = theme, PrimaryColor = primaryColor, CustomCss = customCss, DefaultLanguageCode = defaultLanguageCode, SupportedLanguageCodes = supportedLanguageCodes, Translations = translations.Select(t => new ProjectTranslationPayload { LanguageCode = t.LanguageCode, Name = t.Name, Topic = t.Topic, Introduction = t.Introduction }).ToList() };
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

    public async Task UpdateWizardStepAsync(int projectId, int step)
    {
        var response = await httpClient.PatchAsJsonAsync($"api/projects/{projectId}/wizard-step", new { Step = step });
        response.EnsureSuccessStatusCode();
    }

    public async Task<ShortFormSuggestion> GetShortFormSuggestionAsync(string shortForm, int? excludeProjectId)
    {
        var query = $"api/projects/shortform-suggestion?shortForm={Uri.EscapeDataString(shortForm)}";
        if (excludeProjectId is int id) query += $"&excludeProjectId={id}";
        var response = await httpClient.GetAsync(query);
        if (!response.IsSuccessStatusCode)
            return new ShortFormSuggestion(true, shortForm);
        var dto = await response.Content.ReadFromJsonAsync<ShortFormSuggestion>();
        return dto ?? new ShortFormSuggestion(true, shortForm);
    }

    public sealed record ShortFormSuggestion(bool Available, string Suggestion);
}

using System.Net;
using SynapseEnqueteManagement.UI.Admin.Models.Project;
namespace SynapseEnqueteManagement.UI.Admin.Services.Project;

public class ProjectAdminService(HttpClient httpClient)
{
    private class ProjectRequest
    {
        public string Name { get; set; }
        public string ShortForm { get; set; }
        public string Topic { get; set; }
        public string Introduction { get; set; }
        public string ImageUrl { get; set; }
        public string InteractionMode { get; set; }
        public string AiAssistance { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public bool RequestContactInfo { get; set; }
        public bool AllowCrossLanguageIdeas { get; set; }
        public int IdeaPageSize { get; set; }
        public bool ExtraIdeasReplaceExisting { get; set; }
        public int MaxExtraIdeaRequests { get; set; }
        public int PlatformId { get; set; }
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

    public async Task<ProjectAdminViewModel> CreateProjectAsync(string name, string topic, string introduction, string imageUrl, string interactionMode, string aiAssistance, int? minAge, int? maxAge, bool requestContactInfo, int platformId, string shortForm = null, int ideaPageSize = 5, bool extraIdeasReplaceExisting = false, int maxExtraIdeaRequests = 3, bool allowCrossLanguageIdeas = false)
    {
        var payload = new ProjectRequest { Name = name, ShortForm = shortForm, Topic = topic, Introduction = introduction, ImageUrl = imageUrl, InteractionMode = interactionMode, AiAssistance = aiAssistance, MinAge = minAge, MaxAge = maxAge, RequestContactInfo = requestContactInfo, AllowCrossLanguageIdeas = allowCrossLanguageIdeas, IdeaPageSize = ideaPageSize, ExtraIdeasReplaceExisting = extraIdeasReplaceExisting, MaxExtraIdeaRequests = maxExtraIdeaRequests, PlatformId = platformId };
        var response = await httpClient.PostAsJsonAsync("api/projects", payload);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProjectAdminViewModel>();
    }
    
    public async Task<ProjectAdminViewModel> CopyProjectAsync(int projectId, string newName, int platformId, string newShortForm = null)
    {
        var payload = new { NewName = newName, NewShortForm = newShortForm, PlatformId = platformId };
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

    public async Task<ProjectAdminViewModel> UpdateProjectAsync(int projectId, string name, string topic, string introduction, string imageUrl, string interactionMode, string aiAssistance, int? minAge, int? maxAge, bool requestContactInfo, string shortForm = null, int ideaPageSize = 5, bool extraIdeasReplaceExisting = false, int maxExtraIdeaRequests = 3, bool allowCrossLanguageIdeas = false)
    {
        var payload = new ProjectRequest { Name = name, ShortForm = shortForm, Topic = topic, Introduction = introduction, ImageUrl = imageUrl, InteractionMode = interactionMode, AiAssistance = aiAssistance, MinAge = minAge, MaxAge = maxAge, RequestContactInfo = requestContactInfo, AllowCrossLanguageIdeas = allowCrossLanguageIdeas, IdeaPageSize = ideaPageSize, ExtraIdeasReplaceExisting = extraIdeasReplaceExisting, MaxExtraIdeaRequests = maxExtraIdeaRequests };
        var response = await httpClient.PutAsJsonAsync($"api/projects/{projectId}", payload);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProjectAdminViewModel>();
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

    private sealed class ShortFormLookup
    {
        public int ProjectId { get; set; }
    }
}

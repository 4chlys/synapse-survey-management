using System.Net.Http.Json;
using SynapseEnqueteManagement.UI.Admin.Models.Project;
namespace SynapseEnqueteManagement.UI.Admin.Services.Project;

public class ProjectAdminService(HttpClient httpClient)
{
    private class ProjectRequest
    {
        public string Name { get; set; }
        public string Topic { get; set; }
        public string Introduction { get; set; }
        public string ImageUrl { get; set; }
        public string InteractionMode { get; set; }
        public string AiAssistance { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public bool RequestContactInfo { get; set; }
    }

    public async Task<List<ProjectAdminViewModel>> GetAllProjectsAsync()
    {
        var response = await httpClient.GetAsync("api/projects");
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

    public async Task<ProjectAdminViewModel> CreateProjectAsync(string name, string topic, string introduction, string imageUrl, string interactionMode, string aiAssistance, int? minAge, int? maxAge, bool requestContactInfo)
    {
        var payload = new ProjectRequest { Name = name, Topic = topic, Introduction = introduction, ImageUrl = imageUrl, InteractionMode = interactionMode, AiAssistance = aiAssistance, MinAge = minAge, MaxAge = maxAge, RequestContactInfo = requestContactInfo };
        var response = await httpClient.PostAsJsonAsync("api/projects", payload);
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

    public async Task<ProjectAdminViewModel> UpdateProjectAsync(int projectId, string name, string topic, string introduction, string imageUrl, string interactionMode, string aiAssistance, int? minAge, int? maxAge, bool requestContactInfo)
    {
        var payload = new ProjectRequest { Name = name, Topic = topic, Introduction = introduction, ImageUrl = imageUrl, InteractionMode = interactionMode, AiAssistance = aiAssistance, MinAge = minAge, MaxAge = maxAge, RequestContactInfo = requestContactInfo };
        var response = await httpClient.PutAsJsonAsync($"api/projects/{projectId}", payload);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProjectAdminViewModel>();
    }
}

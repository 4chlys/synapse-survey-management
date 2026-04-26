using SynapseEnqueteManagement.UI.User.Models;

namespace SynapseEnqueteManagement.UI.User.Services.Project;

public class ProjectApiService : IProjectApiService
{
    private readonly HttpClient _httpClient;

    public ProjectApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ProjectViewModel> GetProjectAsync(int projectId)
    {
        var response = await _httpClient.GetAsync($"api/projects/{projectId}");
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ProjectViewModel>();
    }

    public async Task<ProjectShortFormLookupViewModel> GetByShortFormAsync(string shortForm)
    {
        var response = await _httpClient.GetAsync($"api/projects/by-shortform/{shortForm}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProjectShortFormLookupViewModel>();
    }
}

using System.Net.Http.Json;
using SynapseEnqueteManagement.UI.Admin.Models.Project;
namespace SynapseEnqueteManagement.UI.Admin.Services.Project;

public class ProjectAdminService(HttpClient httpClient)
{
    public async Task<List<ProjectAdminViewModel>> GetAllProjectsAsync()
    {
        var response = await httpClient.GetAsync("api/projects");
        if (!response.IsSuccessStatusCode)
            return new List<ProjectAdminViewModel>();

        return await
                   response.Content.ReadFromJsonAsync<List<ProjectAdminViewModel>>()
               ?? new List<ProjectAdminViewModel>();
    }

    public async Task<ProjectAdminViewModel?> GetProjectByIdAsync(int projectId)
    {
        var response = await httpClient.GetAsync($"api/projects/{projectId}");
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ProjectAdminViewModel>();
    }

    public async Task<ProjectAdminViewModel> CreateProjectAsync(string name,
        string imageUrl)
    {
        var payload = new { Name = name, ImageUrl = imageUrl };
        var response = await httpClient.PostAsJsonAsync("api/projects", payload);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProjectAdminViewModel>();
    }
}

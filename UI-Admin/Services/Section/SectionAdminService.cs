using SynapseEnqueteManagement.UI.Admin.Models.Section;

namespace SynapseEnqueteManagement.UI.Admin.Services.Section;

public class SectionAdminService(HttpClient httpClient)
{
    public async Task<List<SectionViewModel>> GetSectionsAsync(int projectId)
    {
        var response = await
            httpClient.GetAsync($"api/projects/{projectId}/sections");
        if (!response.IsSuccessStatusCode)
            return new List<SectionViewModel>();

        return await response.Content.ReadFromJsonAsync<List<SectionViewModel>>()
               ?? new List<SectionViewModel>();
    }

    public async Task CreateSectionAsync(int projectId, string name, string
        description)
    {
        var payload = new { Name = name, Description = description };
        var response = await httpClient.PostAsJsonAsync(
            $"api/projects/{projectId}/sections", payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateSectionAsync(int projectId, int sectionId, string
        name, string description)
    {
        var payload = new { Name = name, Description = description };
        var response = await httpClient.PutAsJsonAsync(
            $"api/projects/{projectId}/sections/{sectionId}", payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteSectionAsync(int projectId, int sectionId)
    {
        var response = await httpClient.DeleteAsync(
            $"api/projects/{projectId}/sections/{sectionId}");
        response.EnsureSuccessStatusCode();
    }
}
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

    public async Task CreateSectionAsync(int projectId, ICollection<SectionTranslationViewModel> translations)
    {
        var payload = new
        {
            Translations = translations.Select(t => new { t.LanguageCode, t.Name, t.Description })
        };
        var response = await httpClient.PostAsJsonAsync(
            $"api/projects/{projectId}/sections", payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteSectionAsync(int projectId, int sectionId)
    {
        var response = await httpClient.DeleteAsync(
            $"api/projects/{projectId}/sections/{sectionId}");
        response.EnsureSuccessStatusCode();
    }
}
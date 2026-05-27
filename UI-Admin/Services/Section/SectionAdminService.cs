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

    public async Task CreateSectionAsync(int projectId, string name, string description, string languageCode)
    {
        var payload = new
        {
            Translations = new[]
            {
                new { LanguageCode = languageCode, Name = name, Description = description }
            }
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

    // Drive the project's sections to match the given names (in any order): delete sections
    // whose name is no longer in the list, create those that are new. Existing matches are
    // left untouched (preserves SectionIds and per-section question/translation state).
    public async Task SyncSectionsAsync(int projectId, IEnumerable<string> sectionNames, string defaultLanguageCode)
    {
        var newNames = (sectionNames ?? Enumerable.Empty<string>())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToList();

        var existing = await GetSectionsAsync(projectId);

        foreach (var s in existing)
            if (!newNames.Contains(s.Name))
                await DeleteSectionAsync(projectId, s.SectionId);

        var existingNames = existing.Select(s => s.Name).ToList();
        foreach (var name in newNames)
            if (!existingNames.Contains(name))
                await CreateSectionAsync(projectId, name, string.Empty, defaultLanguageCode);
    }
}
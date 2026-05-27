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

    // Replace all sections for the project: delete the existing ones, then recreate from the
    // posted multilingual input — keeping only translations in a supported language with a
    // non-empty name. Sections whose translations are all empty/unsupported are skipped.
    public async Task SyncSectionsAsync(
        int projectId,
        IEnumerable<SectionInputViewModel> sections,
        IEnumerable<string> supportedLanguageCodes)
    {
        var supported = supportedLanguageCodes as ISet<string> ?? supportedLanguageCodes.ToHashSet();

        var existing = await GetSectionsAsync(projectId);
        foreach (var s in existing)
            await DeleteSectionAsync(projectId, s.SectionId);

        foreach (var sectionInput in sections ?? new List<SectionInputViewModel>())
        {
            var translations = (sectionInput.Translations ?? new List<SectionTranslationViewModel>())
                .Where(t => supported.Contains(t.LanguageCode) && !string.IsNullOrWhiteSpace(t.Name))
                .Select(t => new SectionTranslationViewModel { LanguageCode = t.LanguageCode, Name = t.Name.Trim() })
                .ToList();
            if (translations.Count == 0) continue;
            await CreateSectionAsync(projectId, translations);
        }
    }
}
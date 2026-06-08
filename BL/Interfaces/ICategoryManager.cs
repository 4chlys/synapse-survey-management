using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Stats;

namespace SynapseEnqueteManagement.BL.Interfaces;

public record CategoryTranslationInput(string LanguageCode, string Name, string Description);

public interface ICategoryManager
{
    Task<IEnumerable<Category>> GetCategoriesForProjectReadOnlyAsync(int projectId);
    Task<IEnumerable<Category>> GetCategoriesWithIdeasAndParticipantsForProjectReadOnlyAsync(int projectId);
    Task<Category> GetCategoryByIdReadOnlyAsync(int id);
    Task<Category> GetCategoryByIdWithProjectReadOnlyAsync(int categoryId);

    Task<Category> CreateCategoryAsync(Project project, IEnumerable<CategoryTranslationInput> translations, CancellationToken ct = default);
    Task<bool> UpdateCategoryAsync(int categoryId, IEnumerable<CategoryTranslationInput> translations, CancellationToken ct = default);
    Task<bool> DeleteCategoryAsync(int categoryId, CancellationToken ct = default);

    Task<List<CategoryMetric>> GetTopCategoriesAsync(int projectId, int count, string languageCode, string defaultLanguageCode);
}

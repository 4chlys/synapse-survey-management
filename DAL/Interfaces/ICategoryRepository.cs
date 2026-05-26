using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Domain.Project.Stats;

namespace SynapseEnqueteManagement.DAL.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    Task<IEnumerable<Category>> ReadByProjectIdReadOnlyAsync(int projectId);
    Task<IEnumerable<Category>> ReadAllWithIdeasByProjectReadOnlyAsync(int projectId);

    Task<Category> ReadByIdReadOnlyAsync(int categoryId);
    Task<Category> ReadByIdWithTranslationsReadOnlyAsync(int categoryId);
    Task<CategoryTranslation> ReadTranslationForUpdateAsync(int categoryId, string languageCode);

    Task<Category> ReadByIdWithProjectReadOnlyAsync(int categoryId);

    Task<Category> ReadByIdWithProjectForUpdateAsync(int categoryId);

    Task<List<CategoryMetric>> ReadTopCategoriesAsync(int projectId, int count, string languageCode, string defaultLanguageCode);
    Task<List<IdeaMetric>> ReadTopIdeasAsync(int projectId, int count);
    Task<List<Idea>> ReadLatestIdeasAsync(int projectId, int count);
    Task<Dictionary<ModerationStatus, int>> ReadModerationQueueAsync(int projectId, int? categoryId = null);

    Task<Idea> ReadIdeaByIdForUpdateAsync(int ideaId, int? projectId = null);
}

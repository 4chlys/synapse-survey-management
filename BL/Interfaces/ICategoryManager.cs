using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Domain.Project.Stats;

namespace SynapseEnqueteManagement.BL.Interfaces;

public record CategoryTranslationInput(string LanguageCode, string Name, string Description);

public interface ICategoryManager
{
    Task<IEnumerable<Category>> GetCategoriesForProjectReadOnlyAsync(int projectId);
    Task<IEnumerable<Category>> GetCategoriesWithIdeasForProjectReadOnlyAsync(int projectId);
    Task<Category> GetCategoryByIdReadOnlyAsync(int id);
    Task<Category> GetCategoryByIdWithProjectReadOnlyAsync(int categoryId);

    Task<Category> CreateCategoryAsync(Project project, IEnumerable<CategoryTranslationInput> translations, CancellationToken ct = default);
    Task<bool> UpdateCategoryAsync(int categoryId, IEnumerable<CategoryTranslationInput> translations, CancellationToken ct = default);
    Task<bool> DeleteCategoryAsync(int categoryId, CancellationToken ct = default);

    Task<DashboardMetrics> GetDashboardMetricsAsync(int projectId, string languageCode, string defaultLanguageCode, int topCount = 5);
    Task<List<IdeaFeedItem>> GetIdeaFeedAsync(int projectId, string languageCode, int count = 10);
    Task<ModerationQueueResult> GetModerationQueueAsync(int projectId);

    Task<(List<IdeaFeedItem> Items, int Total, int ApprovedCount, int PendingCount, int FlaggedCount)>
        GetIdeasPagedAsync(int projectId, string search, int? categoryId, ModerationStatus? status, int page, string languageCode, int pageSize = 20);

    Task SetIdeaModerationStatusAsync(int ideaId, ModerationStatus status, int? projectId = null);
}

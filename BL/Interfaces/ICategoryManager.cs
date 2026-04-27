using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface ICategoryManager
{
    Task<IEnumerable<Category>> GetCategoriesForProjectReadOnlyAsync(int projectId);
    Task<IEnumerable<Category>> GetCategoriesWithIdeasForProjectReadOnlyAsync(int projectId);
    Task<Category> GetCategoryByIdReadOnlyAsync(int id);
    Task<Category> GetCategoryByIdWithProjectReadOnlyAsync(int categoryId);

    Task<Category> CreateCategoryAsync(Project project, string name, string description, CancellationToken ct = default);
    Task<bool> UpdateCategoryAsync(int categoryId, string name, string description, CancellationToken ct = default);
    Task<bool> DeleteCategoryAsync(int categoryId, CancellationToken ct = default);

    Task<List<IdeaFeedItem>> GetIdeaFeedAsync(int projectId, int count = 10);
    Task<ModerationQueueResult> GetModerationQueueAsync(int projectId);

    Task<(List<IdeaFeedItem> Items, int Total, int ApprovedCount, int PendingCount, int FlaggedCount)>
        GetIdeasPagedAsync(int projectId, string? search, int? categoryId, ModerationStatus? status, int page, int pageSize = 20);

    Task SetIdeaModerationStatusAsync(int ideaId, ModerationStatus status, int? projectId = null);
}

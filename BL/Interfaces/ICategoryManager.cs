using SynapseEnqueteManagement.BL.Domain.Project.Entities;

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
}

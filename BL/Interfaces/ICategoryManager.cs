using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface ICategoryManager
{
    Task<IEnumerable<Category>> GetCategoriesForProjectReadOnlyAsync(int projectId);
    Task<IEnumerable<Category>> GetCategoriesWithIdeasForProjectReadOnlyAsync(int projectId);
    Task<Category> GetCategoryByIdReadOnlyAsync(int id);

    Task<Category> CreateCategoryAsync(Project project, string name, string description);
    Task<bool> UpdateCategoryAsync(int categoryId, string name, string description);
    Task<bool> DeleteCategoryAsync(int categoryId);

    Task<List<IdeaFeedItem>> GetIdeaFeedAsync(int projectId, int count = 10);
    Task<ModerationQueueResult> GetModerationQueueAsync(int projectId);
}

public record IdeaFeedItem(int IdeaId, string Description, string CategoryName, int ReactionCount, DateTime CreatedAt);
public record ModerationQueueResult(int Approved, int Declined, int Flagged, int Unflagged);

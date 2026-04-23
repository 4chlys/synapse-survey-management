using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface ICategoryManager
{
    Task<IEnumerable<(Category Category, int IdeaCount)>> GetCategoriesAndTheirIdeaCountsAsync(int projectId);
    Task<(Category Category, int IdeaCount)> GetCategoryAndItsIdeaCountByIdAsync(int id);

    Task<Category> CreateCategoryAsync(int projectId, string name, string description);
    Task<bool> UpdateCategoryAsync(int categoryId, string name, string description);
    Task<bool> DeleteCategoryAsync(int categoryId);
}

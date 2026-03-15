using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface ICategoryManager
{
    Task<IEnumerable<Category>> GetCategoriesAsync(int projectId);
    Task<Category> GetCategoryByIdAsync(int id);
    Task<int> GetIdeaCountAsync(int categoryId);
}
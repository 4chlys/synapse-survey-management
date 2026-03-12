using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL;

public interface ICategoryManager
{
    Task<IEnumerable<Category>> GetCategoriesAsync(int projectId);
    Task<Category> GetCategoryByIdAsync(int id);
}

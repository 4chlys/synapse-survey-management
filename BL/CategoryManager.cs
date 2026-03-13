using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.DAL;

namespace SynapseEnqueteManagement.BL;

public class CategoryManager(IRepository repository) : ICategoryManager
{
    public async Task<IEnumerable<Category>> GetCategoriesAsync(int projectId)
    {
        return await repository.ReadAsync<Category>(
            filters: [c => c.Project.ProjectId == projectId],
            includes: [c => c.Ideas]
        );
    }

    public async Task<Category> GetCategoryByIdAsync(int id)
    {
        return await repository.ReadByIdAsync<Category>(id, includes: [c => c.Ideas]);
    }
}

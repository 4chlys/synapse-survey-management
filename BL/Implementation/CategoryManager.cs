using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.DAL;

namespace SynapseEnqueteManagement.BL.Implementation;

public class CategoryManager(IRepository repository) : ICategoryManager
{
    public async Task<IEnumerable<Category>> GetCategoriesAsync(int projectId)
    {
        return await repository.ReadAsync<Category>(
            filters: [c => c.Project.ProjectId == projectId]
        );
    }

    public async Task<Category> GetCategoryByIdAsync(int id)
    {
        return await repository.ReadByIdAsync<Category>(id);
    }

    public async Task<int> GetIdeaCountAsync(int categoryId)
    {
        return await repository.CountAsync<Idea>(
            filters: [i => i.Category.CategoryId == categoryId]
        );
    }
}
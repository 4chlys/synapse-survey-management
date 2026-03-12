using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.DAL;

namespace SynapseEnqueteManagement.BL;

public class CategoryManager : ICategoryManager
{
    private readonly IRepository _repository;

    public CategoryManager(IRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Category>> GetCategoriesAsync(int projectId)
    {
        return await _repository.ReadAsync<Category>(
            filters: [c => c.Project.ProjectId == projectId],
            includes: [c => c.Ideas]
        );
    }
    
    public async Task<Category> GetCategoryByIdAsync(int id)
    {
        return await _repository.ReadByIdAsync<Category>(id, includes: [c => c.Ideas]);
    }
}

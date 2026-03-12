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

    public async Task<IEnumerable<Category>> GetCategoriesAsync()
    {
        return await _repository.ReadAsync<Category>(
            includes: [c => c.Ideas]
        );
    }
}

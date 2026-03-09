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

    public IEnumerable<Category> GetCategories()
    {
        return _repository.Read<Category>(
            includes: [c => c.Ideas]
        );
    }
}

using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL;

public interface ICategoryManager
{
    Task<IEnumerable<Category>> GetCategoriesAsync();
}

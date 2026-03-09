using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL;

public interface ICategoryManager
{
    IEnumerable<Category> GetCategories();
}

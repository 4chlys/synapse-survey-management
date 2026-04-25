using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    Task<IEnumerable<Category>> ReadByProjectIdReadOnlyAsync(int projectId);

    Task<Category> ReadByIdReadOnlyAsync(int categoryId);

    Task<Category> ReadByIdWithProjectReadOnlyAsync(int categoryId);

    Task<Category> ReadByIdWithProjectForUpdateAsync(int categoryId);
}

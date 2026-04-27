using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.DAL.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    Task<IEnumerable<Category>> ReadByProjectIdReadOnlyAsync(int projectId);
    Task<IEnumerable<Category>> ReadAllWithIdeasByProjectReadOnlyAsync(int projectId);

    Task<Category> ReadByIdReadOnlyAsync(int categoryId);

    Task<Category> ReadByIdWithProjectReadOnlyAsync(int categoryId);

    Task<Category> ReadByIdWithProjectForUpdateAsync(int categoryId);

    Task<List<Idea>> ReadLatestIdeasAsync(int projectId, int count);
    Task<Dictionary<ModerationStatus, int>> ReadModerationQueueAsync(int projectId);
}

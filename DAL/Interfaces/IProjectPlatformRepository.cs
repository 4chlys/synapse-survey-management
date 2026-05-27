using SynapseEnqueteManagement.BL.Domain.Platform.Entities;

namespace SynapseEnqueteManagement.DAL.Interfaces;

public interface IProjectPlatformRepository : IRepository<ProjectPlatform>
{
    Task<IEnumerable<ProjectPlatform>> ReadAllOrderedByNameReadOnlyAsync();
}

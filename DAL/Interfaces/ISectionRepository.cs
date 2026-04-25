using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL.Interfaces;

public interface ISectionRepository : IRepository<Section>
{
    Task<IEnumerable<Section>> ReadByProjectIdReadOnlyAsync(int projectId);
}

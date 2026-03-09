using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL;

public interface IProjectManager
{
    Project GetProjectById(int projectId);
}

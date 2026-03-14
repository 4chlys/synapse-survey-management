using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL;

public interface IProjectManager
{
    Task<Project> GetProjectByIdAsync(int projectId);
}
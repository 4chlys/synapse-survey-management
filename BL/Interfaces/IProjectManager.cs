using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IProjectManager
{
    Task<Project> GetProjectByIdAsync(int projectId);
    
    Task<Project> CreateProjectAsync(string name, string imageUrl );
    Task<IEnumerable<Project>> GetAllProjectsAsync();
}
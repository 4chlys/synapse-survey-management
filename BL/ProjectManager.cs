using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.DAL;

namespace SynapseEnqueteManagement.BL;

public class ProjectManager(IRepository repository) : IProjectManager
{
    public async Task<Project> GetProjectByIdAsync(int projectId)
    {
        return await repository.ReadByIdAsync<Project>(projectId,
            includes: [p => p.Introduction]);
    }
}
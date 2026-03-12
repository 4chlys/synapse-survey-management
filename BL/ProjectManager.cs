using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.DAL;

namespace SynapseEnqueteManagement.BL;

public class ProjectManager : IProjectManager
{
    private readonly IRepository _repository;

    public ProjectManager(IRepository repository)
    {
        _repository = repository;
    }

    public async Task<Project> GetProjectByIdAsync(int projectId)
    {
        return await _repository.ReadByIdAsync<Project>(projectId);
    }
}

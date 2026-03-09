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

    public Project GetProjectById(int projectId)
    {
        return _repository.ReadSingle<Project>(
            filters: [p => p.ProjectId == projectId]
        );
    }
}

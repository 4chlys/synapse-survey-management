using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.DAL;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class ProjectManager(IRepository repository) : IProjectManager
{
    public async Task<Project> GetProjectByIdAsync(int projectId)
    {
        return await repository.ReadByIdAsync<Project>(projectId,
            includes: [p => p.Introduction]);
    }

    public async Task<Project> CreateProjectAsync(string name, string imageUrl)
    {
        var project = new Project
        {
            OrganisationName = name,
            OrganisationLogoUrl = imageUrl,
            ProjectState = ProjectState.Draft,
            FormType = FormType.VerticalScroll,
            Questionnaire = new Questionnaire(),
            CreatedAt = DateTime.UtcNow
        };
        return await repository.CreateAsync(project);
    }

    public async Task<IEnumerable<Project>> GetAllProjectsAsync()
    {
        return await repository.ReadAsync<Project>();
    }
    
}
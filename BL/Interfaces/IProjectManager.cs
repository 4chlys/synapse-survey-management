using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IProjectManager
{
    Task<Project> GetProjectByIdAsync(int projectId);
    Task<Project> CreateProjectAsync(string name, string topic, string introduction, string imageUrl, FormType formType, string aiAssistance, int? minAge, int? maxAge, bool requestContactInfo);
    Task<Project> UpdateProjectAsync(int projectId, string name, string topic, string introduction, string imageUrl, FormType formType, string aiAssistance, int? minAge, int? maxAge, bool requestContactInfo);
    Task<IEnumerable<Project>> GetAllProjectsAsync();
}

using SynapseEnqueteManagement.UI.User.Models;

namespace SynapseEnqueteManagement.UI.User.Services.Project;

public interface IProjectApiService
{
    Task<ProjectViewModel> GetProjectAsync(int projectId);
}

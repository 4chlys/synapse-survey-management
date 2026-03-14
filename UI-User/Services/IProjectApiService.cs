using SynapseEnqueteManagement.UI.User.Models;

namespace SynapseEnqueteManagement.UI.User.Services;

public interface IProjectApiService
{
    Task<ProjectViewModel> GetProjectAsync(int projectId);
}
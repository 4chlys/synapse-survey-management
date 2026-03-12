using SynapseEnqueteManagement.UI.User.Models;

namespace SynapseEnqueteManagement.UI.User.Services;

public interface IProjectApiService
{
    Task<ProjectInfo> GetProjectAsync(int projectId);
}

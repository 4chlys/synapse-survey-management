using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using SynapseEnqueteManagement.UI.Admin.Data;
using SynapseEnqueteManagement.UI.Admin.Services.Project;

namespace SynapseEnqueteManagement.UI.Admin.Filters;

public interface IProjectOwnershipLookup
{
    Task<ProjectOwnership> ResolveAsync(ClaimsPrincipal user, int projectId);
}

public sealed record ProjectOwnership(int? CallerPlatformId, int? ProjectPlatformId)
{
    public bool ProjectExists => ProjectPlatformId.HasValue;
    public bool Matches => CallerPlatformId.HasValue && CallerPlatformId == ProjectPlatformId;
}

public sealed class ProjectOwnershipLookup(
    ProjectAdminService projectService,
    UserManager<ApplicationUser> userManager) : IProjectOwnershipLookup
{
    public async Task<ProjectOwnership> ResolveAsync(ClaimsPrincipal user, int projectId)
    {
        var appUser = await userManager.GetUserAsync(user);
        var project = await projectService.GetProjectByIdAsync(projectId);
        return new ProjectOwnership(appUser?.ProjectPlatformId, project?.PlatformId);
    }
}

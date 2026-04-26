using SynapseEnqueteManagement.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class ProjectRepository(DbContext context) : Repository<Project>(context), IProjectRepository
{
    public Task<Project> ReadByIdReadOnlyAsync(int projectId)
        => Context.Set<Project>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);

    public Task<Project> ReadByIdWithIntroductionReadOnlyAsync(int projectId)
        => Context.Set<Project>()
            .AsNoTracking()
            .Include(p => p.Introduction)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);

    public Task<Project> ReadByIdWithIntroductionForUpdateAsync(int projectId)
        => Context.Set<Project>()
            .Include(p => p.Introduction)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);

    public Task<Project> ReadByIdWithEmojisForUpdateAsync(int projectId)
        => Context.Set<Project>()
            .Include(p => p.Emojis)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);

    public Task<Project> ReadByIdWithEmojisReadOnlyAsync(int projectId)
        => Context.Set<Project>()
            .AsNoTracking()
            .Include(p => p.Emojis)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);

    public Task<bool> ExistsByShortFormAsync(string shortForm, int? excludeProjectId = null)
        => Context.Set<Project>()
            .AnyAsync(p => p.ShortForm == shortForm
                           && (excludeProjectId == null || p.ProjectId != excludeProjectId));

    public Task<Project> ReadByShortFormReadOnlyAsync(string shortForm)
        => Context.Set<Project>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ShortForm == shortForm);
}

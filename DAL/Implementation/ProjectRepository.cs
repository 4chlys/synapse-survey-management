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

    public async Task<IEnumerable<Project>> ReadAllWithPlatformReadOnlyAsync(int? platformId = null)
    {
        var query = Context.Set<Project>()
            .AsNoTracking()
            .Include(p => p.ProjectPlatform)
            .AsQueryable();

        if (platformId.HasValue)
            query = query.Where(p => p.ProjectPlatformId == platformId.Value);

        return await query.ToListAsync();
    }
}

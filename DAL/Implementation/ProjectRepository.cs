using SynapseEnqueteManagement.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class ProjectRepository(DbContext context) : Repository<Project>(context), IProjectRepository
{
    public Task<Project> ReadByIdReadOnlyAsync(int projectId)
        => Context.Set<Project>()
            .AsNoTracking()
            .Include(p => p.ProjectPlatform)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);

    public Task<Project> ReadByIdWithIntroductionReadOnlyAsync(int projectId)
        => Context.Set<Project>()
            .AsNoTracking()
            .Include(p => p.ProjectPlatform)
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

    public async Task<IEnumerable<Project>> ReadAllWithPlatformReadOnlyAsync(int? platformId = null)
    {
        IQueryable<Project> query = Context.Set<Project>()
            .AsNoTracking()
            .Include(p => p.ProjectPlatform);

        if (platformId.HasValue)
            query = query.Where(p => p.ProjectPlatformId == platformId.Value);

        return await query.ToListAsync();
    }

    public async Task<bool> DeleteWithRelationsAsync(int projectId)
    {
        var project = await Context.Set<Project>()
            .Include(p => p.Categories)
                .ThenInclude(c => c.Ideas)
                    .ThenInclude(i => i.Comments)
            .Include(p => p.Participants)
                .ThenInclude(p => p.Comments)
            .Include(p => p.Questionnaire)
                .ThenInclude(q => q.Questions)
                    .ThenInclude(q => q.Responses)
            .Include(p => p.Questionnaire)
                .ThenInclude(q => q.Questions)
                    .ThenInclude(q => q.EndingAnswers)
            .Include(p => p.Introduction)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);

        if (project is null)
            return false;

        Context.Set<Project>().Remove(project);
        return true;
    }
}

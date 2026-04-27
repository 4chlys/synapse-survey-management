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

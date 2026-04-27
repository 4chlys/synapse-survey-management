using SynapseEnqueteManagement.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class CategoryRepository(DbContext context) : Repository<Category>(context), ICategoryRepository
{
    public async Task<IEnumerable<Category>> ReadByProjectIdReadOnlyAsync(int projectId)
        => await Context.Set<Category>()
            .AsNoTracking()
            .Where(c => c.Project.ProjectId == projectId)
            .ToListAsync();

    public async Task<IEnumerable<Category>> ReadAllWithIdeasByProjectReadOnlyAsync(int projectId)
        => await Context.Set<Category>()
            .AsNoTracking()
            .Where(c => c.Project.ProjectId == projectId)
            .Include(c => c.Ideas)
                .ThenInclude(i => i.Comments)
            .ToListAsync();

    public Task<Category> ReadByIdReadOnlyAsync(int categoryId)
        => Context.Set<Category>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

    public Task<Category> ReadByIdWithProjectReadOnlyAsync(int categoryId)
        => Context.Set<Category>()
            .AsNoTracking()
            .Include(c => c.Project)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

    public Task<Category> ReadByIdWithProjectForUpdateAsync(int categoryId)
        => Context.Set<Category>()
            .Include(c => c.Project)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

    public async Task<List<Idea>> ReadLatestIdeasAsync(int projectId, int count)
        => await Context.Set<Idea>()
            .AsNoTracking()
            .Include(i => i.Category)
            .Include(i => i.Comments)
            .Where(i => i.Status == ModerationStatus.Approved
                        && i.Category.Project.ProjectId == projectId)
            .OrderByDescending(i => i.CreatedAt)
            .Take(count)
            .ToListAsync();

    public async Task<Dictionary<ModerationStatus, int>> ReadModerationQueueAsync(int projectId)
        => await Context.Set<Idea>()
            .AsNoTracking()
            .Where(i => i.Category.Project.ProjectId == projectId)
            .GroupBy(i => i.Status)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
}

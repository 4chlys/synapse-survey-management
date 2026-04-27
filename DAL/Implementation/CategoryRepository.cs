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

    public async Task<(List<Idea> Items, int Total)> ReadIdeasPagedAsync(
        int projectId, string? search, int? categoryId, ModerationStatus? status,
        int page, int pageSize)
    {
        var query = Context.Set<Idea>()
            .AsNoTracking()
            .Include(i => i.Category)
            .Include(i => i.Comments)
            .Where(i => i.Category.Project.ProjectId == projectId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(i => i.Description.Contains(search));
        if (categoryId.HasValue)
            query = query.Where(i => i.Category.CategoryId == categoryId.Value);
        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<Idea?> ReadIdeaByIdForUpdateAsync(int ideaId, int? projectId = null)
        => await Context.Set<Idea>()
            .FirstOrDefaultAsync(i => i.IdeaId == ideaId
                && (projectId == null || i.Category.Project.ProjectId == projectId));
}

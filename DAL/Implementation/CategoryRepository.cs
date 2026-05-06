using SynapseEnqueteManagement.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Domain.Project.Stats;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class CategoryRepository(DbContext context) : Repository<Category>(context), ICategoryRepository
{
    public async Task<IEnumerable<Category>> ReadByProjectIdReadOnlyAsync(int projectId)
        => await Context.Set<Category>()
            .AsNoTracking()
            .Where(c => c.Project.ProjectId == projectId)
            .Include(c => c.Translations).ThenInclude(t => t.Language)
            .ToListAsync();

    public async Task<IEnumerable<Category>> ReadAllWithIdeasByProjectReadOnlyAsync(int projectId)
        => await Context.Set<Category>()
            .AsNoTracking()
            .Where(c => c.Project.ProjectId == projectId)
            .Include(c => c.Translations).ThenInclude(t => t.Language)
            .Include(c => c.Ideas)
                .ThenInclude(i => i.Comments)
            .ToListAsync();

    public Task<Category> ReadByIdReadOnlyAsync(int categoryId)
        => Context.Set<Category>()
            .AsNoTracking()
            .Include(c => c.Translations).ThenInclude(t => t.Language)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

    public Task<Category> ReadByIdWithProjectReadOnlyAsync(int categoryId)
        => Context.Set<Category>()
            .AsNoTracking()
            .Include(c => c.Project)
                .ThenInclude(p => p.DefaultLanguage)
            .Include(c => c.Translations).ThenInclude(t => t.Language)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

    public Task<Category> ReadByIdWithProjectForUpdateAsync(int categoryId)
        => Context.Set<Category>()
            .Include(c => c.Project)
            .Include(c => c.Translations).ThenInclude(t => t.Language)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

    public Task<Category> ReadByIdWithTranslationsReadOnlyAsync(int categoryId)
        => Context.Set<Category>()
            .AsNoTracking()
            .Include(c => c.Translations).ThenInclude(t => t.Language)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

    public Task<CategoryTranslation> ReadTranslationForUpdateAsync(int categoryId, string languageCode)
        => Context.Set<CategoryTranslation>()
            .FirstOrDefaultAsync(t =>
                EF.Property<int>(t, "CategoryId") == categoryId
                && EF.Property<string>(t, "LanguageCode") == languageCode);

    public async Task<List<CategoryMetric>> ReadTopCategoriesAsync(
        int projectId, int count, string languageCode, string defaultLanguageCode)
    {
        var rows = await Context.Set<Category>()
            .AsNoTracking()
            .Where(c => c.Project.ProjectId == projectId)
            .Select(c => new
            {
                c.CategoryId,
                Name = c.Translations
                           .Where(t => EF.Property<string>(t, "LanguageCode") == languageCode)
                           .Select(t => t.Name)
                           .FirstOrDefault()
                       ?? c.Translations
                           .Where(t => EF.Property<string>(t, "LanguageCode") == defaultLanguageCode)
                           .Select(t => t.Name)
                           .FirstOrDefault()
                       ?? c.Translations.Select(t => t.Name).FirstOrDefault(),
                IdeaCount = c.Ideas.Count(i => i.Status == ModerationStatus.Approved),
                TotalReactions = c.Ideas
                    .Where(i => i.Status == ModerationStatus.Approved)
                    .SelectMany(i => i.Comments)
                    .Count(cm => EF.Property<string>(cm, "CommentType") == "IconComment")
            })
            .OrderByDescending(c => c.IdeaCount + c.TotalReactions)
            .Take(count)
            .ToListAsync();

        return rows
            .Select(c => new CategoryMetric(c.CategoryId, c.Name ?? string.Empty, c.IdeaCount, c.TotalReactions))
            .ToList();
    }

    public async Task<List<IdeaMetric>> ReadTopIdeasAsync(int projectId, int count)
    {
        var raw = await Context.Set<Idea>()
            .AsNoTracking()
            .Where(i => i.Status == ModerationStatus.Approved
                        && i.Category.Project.ProjectId == projectId)
            .Select(i => new
            {
                i.IdeaId,
                i.Description,
                ReactionCount = i.Comments.Count(cm => EF.Property<string>(cm, "CommentType") == "IconComment"),
                CommentCount = i.Comments.Count(cm => EF.Property<string>(cm, "CommentType") == "TextComment")
            })
            .OrderByDescending(i => i.ReactionCount + i.CommentCount)
            .Take(count)
            .ToListAsync();

        return raw.Select(i => new IdeaMetric(i.IdeaId, i.Description, i.ReactionCount, i.CommentCount)).ToList();
    }

    public async Task<List<Idea>> ReadLatestIdeasAsync(int projectId, int count)
        => await Context.Set<Idea>()
            .AsNoTracking()
            .Include(i => i.Category).ThenInclude(c => c.Translations).ThenInclude(t => t.Language)
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
        int projectId, string search, int? categoryId, ModerationStatus? status,
        int page, int pageSize)
    {
        var query = Context.Set<Idea>()
            .AsNoTracking()
            .Include(i => i.Category).ThenInclude(c => c.Translations).ThenInclude(t => t.Language)
            .Include(i => i.Comments)
            .Include(i => i.Participant)
            .Include(i => i.Language)
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

    public async Task<Idea> ReadIdeaByIdForUpdateAsync(int ideaId, int? projectId = null)
        => await Context.Set<Idea>()
            .FirstOrDefaultAsync(i => i.IdeaId == ideaId
                && (projectId == null || i.Category.Project.ProjectId == projectId));
}

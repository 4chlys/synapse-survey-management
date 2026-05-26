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
                    .ThenInclude(c => ((IconComment)c).Emoji)
            .Include(c => c.Ideas)
                .ThenInclude(i => i.Comments)
                    .ThenInclude(c => ((IconComment)c).TextComment)
            .Include(c => c.Ideas)
                .ThenInclude(i => i.Comments)
                    .ThenInclude(c => ((TextComment)c).EmojiReactions)
                        .ThenInclude(ir => ir.Emoji)
            .Include(c => c.Ideas)
                .ThenInclude(i => i.Comments)
                    .ThenInclude(c => c.Participant)
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
            .Include(c => c.Project).ThenInclude(p => p.SupportedLanguages)
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

    public async Task<Dictionary<ModerationStatus, int>> ReadModerationQueueAsync(int projectId, int? categoryId = null)
    {
        var query = Context.Set<Idea>()
            .AsNoTracking()
            .Where(i => i.Category.Project.ProjectId == projectId);

        if (categoryId.HasValue)
            query = query.Where(i => i.Category.CategoryId == categoryId.Value);

        return await query
            .GroupBy(i => i.Status)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }
}

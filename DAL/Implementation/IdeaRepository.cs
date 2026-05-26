using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Domain.Project.Stats;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class IdeaRepository(DbContext context) : Repository<Idea>(context), IIdeaRepository
{
    private static IQueryable<Idea> WithComments(IQueryable<Idea> query)
        => query
            .Include(i => i.Comments).ThenInclude(c => c.Participant)
            .Include($"{nameof(Idea.Comments)}.{nameof(IconComment.Emoji)}")
            .Include($"{nameof(Idea.Comments)}.{nameof(IconComment.TextComment)}");

    // Reactions (IconComments that are on a TextComment) share the same IdeaId but must not appear
    // in Idea.Comments — strip them client-side after materialisation.
    private static void ExcludeReactions(IEnumerable<Idea> ideas)
    {
        foreach (var idea in ideas)
            idea.Comments = idea.Comments
                .Where(c => c is not IconComment ic || ic.TextComment == null)
                .ToList();
    }

    public Task<Idea> ReadByIdWithProjectForUpdateAsync(int ideaId)
        => Context.Set<Idea>()
            .Include(i => i.Category).ThenInclude(c => c.Project).ThenInclude(p => p.SupportedLanguages)
            .FirstOrDefaultAsync(i => i.IdeaId == ideaId);

    public Task<Idea> ReadByIdWithProjectAndEmojisForUpdateAsync(int ideaId)
        => Context.Set<Idea>()
            .Include(i => i.Category).ThenInclude(c => c.Project).ThenInclude(p => p.Emojis)
            .FirstOrDefaultAsync(i => i.IdeaId == ideaId);

    public async Task<Idea> ReadByIdWithCommentsAndParticipantReadOnlyAsync(int ideaId)
    {
        var idea = await WithComments(Context.Set<Idea>().AsNoTracking().Include(i => i.Participant).Include(i => i.Language))
            .FirstOrDefaultAsync(i => i.IdeaId == ideaId);
        if (idea != null) ExcludeReactions([idea]);
        return idea;
    }

    public Task<Idea> ReadByIdWithParticipantReadOnlyAsync(int ideaId)
        => Context.Set<Idea>()
            .AsNoTracking()
            .Include(i => i.Participant)
            .FirstOrDefaultAsync(i => i.IdeaId == ideaId);

    public async Task<(bool AllowCrossLanguageIdeas, string DefaultLanguageCode)> ReadProjectFlagsForCategoryReadOnlyAsync(int categoryId)
    {
        var result = await Context.Set<Category>()
            .AsNoTracking()
            .Where(c => c.CategoryId == categoryId)
            .Select(c => new
            {
                c.Project.AllowCrossLanguageIdeas,
                DefaultLanguageCode = EF.Property<string>(c.Project, "DefaultLanguageCode")
            })
            .FirstOrDefaultAsync();
        return result == null ? (false, null) : (result.AllowCrossLanguageIdeas, result.DefaultLanguageCode);
    }

    public Task<int> CountVisibleByCategoryAsync(int categoryId, string filterLanguageCode = null)
    {
        var query = Context.Set<Idea>()
            .Where(i => i.Category.CategoryId == categoryId
                         && i.Status != ModerationStatus.Flagged
                         && i.Status != ModerationStatus.Declined);
        if (filterLanguageCode != null)
            query = query.Where(i => EF.Property<string>(i, "LanguageCode") == filterLanguageCode);
        return query.CountAsync();
    }

    public async Task<Dictionary<int, int>> CountVisibleByCategoriesAsync(IEnumerable<int> categoryIds, string filterLanguageCode = null)
    {
        var ids = categoryIds.ToHashSet();
        var query = Context.Set<Idea>()
            .Where(i => ids.Contains(i.Category.CategoryId)
                         && i.Status != ModerationStatus.Flagged
                         && i.Status != ModerationStatus.Declined);
        if (filterLanguageCode != null)
            query = query.Where(i => EF.Property<string>(i, "LanguageCode") == filterLanguageCode);
        return await query
            .GroupBy(i => i.Category.CategoryId)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }

    public async Task<IEnumerable<Idea>> ReadVisibleByCategoryWithCommentsReadOnlyAsync(
        int categoryId,
        Func<IQueryable<Idea>, IOrderedQueryable<Idea>> orderBy,
        bool randomize,
        int skip,
        int take,
        string filterLanguageCode = null)
    {
        var query = WithComments(Context.Set<Idea>().AsNoTracking().Include(i => i.Participant).Include(i => i.Language))
            .Where(i => i.Category.CategoryId == categoryId
                        && i.Status != ModerationStatus.Flagged
                        && i.Status != ModerationStatus.Declined);

        if (filterLanguageCode != null)
            query = query.Where(i => EF.Property<string>(i, "LanguageCode") == filterLanguageCode);

        IQueryable<Idea> ordered = randomize
            ? query.OrderBy(_ => EF.Functions.Random())
            : orderBy(query);

        var ideas = await ordered.Skip(skip).Take(take).ToListAsync();
        ExcludeReactions(ideas);
        return ideas;
    }

    public Task<bool> AnyExistByParticipantAsync(int participantId)
        => Context.Set<Idea>()
            .AnyAsync(i => i.Participant.ParticipantId == participantId);

    public Task<string> ReadMostRecentDescriptionByCategoryAndParticipantAsync(int categoryId, int participantId)
        => Context.Set<Idea>()
            .AsNoTracking()
            .Where(i => i.Category.CategoryId == categoryId
                        && i.Participant.ParticipantId == participantId
                        && i.Status != ModerationStatus.Declined)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => i.Description)
            .FirstOrDefaultAsync();

    public async Task<IEnumerable<Idea>> ReadByCategoryAndParticipantWithCommentsReadOnlyAsync(int categoryId, int participantId)
    {
        var ideas = await WithComments(Context.Set<Idea>().AsNoTracking().Include(i => i.Participant).Include(i => i.Language))
            .Where(i => i.Category.CategoryId == categoryId
                        && i.Participant.ParticipantId == participantId
                        && i.Status != ModerationStatus.Declined)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
        ExcludeReactions(ideas);
        return ideas;
    }

    public async Task<List<IdeaMetric>> ReadTopByProjectReadOnlyAsync(int projectId, int count)
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

    public async Task<List<Idea>> ReadLatestByProjectWithCommentsReadOnlyAsync(int projectId, int count)
        => await Context.Set<Idea>()
            .AsNoTracking()
            .Include(i => i.Category).ThenInclude(c => c.Translations).ThenInclude(t => t.Language)
            .Include(i => i.Comments)
            .Where(i => i.Status == ModerationStatus.Approved
                        && i.Category.Project.ProjectId == projectId)
            .OrderByDescending(i => i.CreatedAt)
            .Take(count)
            .ToListAsync();

    public async Task<Dictionary<ModerationStatus, int>> CountByStatusForProjectAsync(int projectId)
        => await Context.Set<Idea>()
            .AsNoTracking()
            .Where(i => i.Category.Project.ProjectId == projectId)
            .GroupBy(i => i.Status)
            .ToDictionaryAsync(g => g.Key, g => g.Count());

    public async Task<(List<Idea> Items, int Total)> ReadPagedByProjectWithCommentsReadOnlyAsync(
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

    public async Task<Idea> ReadByIdInProjectForUpdateAsync(int ideaId, int? projectId = null)
        => await Context.Set<Idea>()
            .FirstOrDefaultAsync(i => i.IdeaId == ideaId
                && (projectId == null || i.Category.Project.ProjectId == projectId));

}
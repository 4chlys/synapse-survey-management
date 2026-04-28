using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
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
            .Include(i => i.Category).ThenInclude(c => c.Project)
            .FirstOrDefaultAsync(i => i.IdeaId == ideaId);

    public Task<Idea> ReadByIdWithProjectAndEmojisForUpdateAsync(int ideaId)
        => Context.Set<Idea>()
            .Include(i => i.Category).ThenInclude(c => c.Project).ThenInclude(p => p.Emojis)
            .FirstOrDefaultAsync(i => i.IdeaId == ideaId);

    public async Task<Idea> ReadByIdWithCommentsAndParticipantReadOnlyAsync(int ideaId)
    {
        var idea = await WithComments(Context.Set<Idea>().AsNoTracking().Include(i => i.Participant))
            .FirstOrDefaultAsync(i => i.IdeaId == ideaId);
        if (idea != null) ExcludeReactions([idea]);
        return idea;
    }

    public Task<Idea> ReadByIdWithParticipantReadOnlyAsync(int ideaId)
        => Context.Set<Idea>()
            .AsNoTracking()
            .Include(i => i.Participant)
            .FirstOrDefaultAsync(i => i.IdeaId == ideaId);

    public Task<int> CountVisibleByCategoryAsync(int categoryId)
        => Context.Set<Idea>()
            .CountAsync(i => i.Category.CategoryId == categoryId
                             && i.Status != ModerationStatus.Flagged
                             && i.Status != ModerationStatus.Declined);

    public Task<int> CountByCategoryAsync(int categoryId)
        => Context.Set<Idea>()
            .CountAsync(i => i.Category.CategoryId == categoryId);

    public async Task<Dictionary<int, int>> CountByCategoriesAsync(IEnumerable<int> categoryIds)
    {
        var ids = categoryIds.ToHashSet();
        return await Context.Set<Idea>()
            .Where(i => ids.Contains(i.Category.CategoryId))
            .GroupBy(i => i.Category.CategoryId)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }

    public async Task<IEnumerable<Idea>> ReadVisibleByCategoryWithCommentsReadOnlyAsync(
        int categoryId,
        Func<IQueryable<Idea>, IOrderedQueryable<Idea>> orderBy,
        bool randomize,
        int skip,
        int take)
    {
        var query = WithComments(Context.Set<Idea>().AsNoTracking().Include(i => i.Participant))
            .Where(i => i.Category.CategoryId == categoryId
                        && i.Status != ModerationStatus.Flagged
                        && i.Status != ModerationStatus.Declined);

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
        var ideas = await WithComments(Context.Set<Idea>().AsNoTracking().Include(i => i.Participant))
            .Where(i => i.Category.CategoryId == categoryId
                        && i.Participant.ParticipantId == participantId
                        && i.Status != ModerationStatus.Declined)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
        ExcludeReactions(ideas);
        return ideas;
    }

}
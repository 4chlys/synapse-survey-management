using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class IdeaRepository(DbContext context) : Repository<Idea>(context), IIdeaRepository
{
    public Task<Idea> ReadByIdWithProjectForUpdateAsync(int ideaId)
        => Context.Set<Idea>()
            .Include(i => i.Category).ThenInclude(c => c.Project)
            .FirstOrDefaultAsync(i => i.IdeaId == ideaId);

    public Task<Idea> ReadByIdWithProjectAndEmojisForUpdateAsync(int ideaId)
        => Context.Set<Idea>()
            .Include(i => i.Category).ThenInclude(c => c.Project).ThenInclude(p => p.Emojis)
            .FirstOrDefaultAsync(i => i.IdeaId == ideaId);

    public Task<Idea> ReadByIdWithCommentsAndParticipantReadOnlyAsync(int ideaId)
        => Context.Set<Idea>()
            .AsNoTracking()
            .Include(i => i.Participant)
            .Include(i => i.Comments).ThenInclude(c => c.Participant)
            .Include($"{nameof(Idea.Comments)}.{nameof(IconComment.Emoji)}")
            .FirstOrDefaultAsync(i => i.IdeaId == ideaId);

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
        var query = Context.Set<Idea>()
            .AsNoTracking()
            .Include(i => i.Participant)
            .Include(i => i.Comments).ThenInclude(c => c.Participant)
            .Include($"{nameof(Idea.Comments)}.{nameof(IconComment.Emoji)}")
            .Where(i => i.Category.CategoryId == categoryId
                        && i.Status != ModerationStatus.Flagged
                        && i.Status != ModerationStatus.Declined);

        IQueryable<Idea> ordered = randomize
            ? query.OrderBy(_ => EF.Functions.Random())
            : orderBy(query);

        return await ordered.Skip(skip).Take(take).ToListAsync();
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
        => await Context.Set<Idea>()
            .AsNoTracking()
            .Include(i => i.Participant)
            .Include(i => i.Comments).ThenInclude(c => c.Participant)
            .Include($"{nameof(Idea.Comments)}.{nameof(IconComment.Emoji)}")
            .Where(i => i.Category.CategoryId == categoryId
                        && i.Participant.ParticipantId == participantId
                        && i.Status != ModerationStatus.Declined)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

}
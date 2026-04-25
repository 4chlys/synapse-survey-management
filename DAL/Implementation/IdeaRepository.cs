using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
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

    public async Task<IEnumerable<Idea>> ReadVisibleByCategoryOrderedByEmbeddingReadOnlyAsync(
        int categoryId, float[] queryEmbedding, bool ascending, int skip, int take)
    {
        var baseQuery = VisibleByCategoryQuery(categoryId);

        if (Context.Database.IsNpgsql())
        {
            var queryVector = new Vector(queryEmbedding);
            var ordered = ascending
                ? baseQuery.OrderBy(i => i.Embedding.CosineDistance(queryVector))
                : baseQuery.OrderByDescending(i => i.Embedding.CosineDistance(queryVector));
            return await ordered.Skip(skip).Take(take).ToListAsync();
        }

        var all = await baseQuery.ToListAsync();
        return SortByCosineDistance(all, queryEmbedding, ascending).Skip(skip).Take(take);
    }

    public async Task<IEnumerable<Idea>> ReadVisibleByCategoryTopNByEmbeddingReadOnlyAsync(
        int categoryId, float[] queryEmbedding, int maxItems)
    {
        var baseQuery = VisibleByCategoryQuery(categoryId);

        if (Context.Database.IsNpgsql())
        {
            var queryVector = new Vector(queryEmbedding);
            return await baseQuery
                .OrderBy(i => i.Embedding.CosineDistance(queryVector))
                .Take(maxItems)
                .ToListAsync();
        }

        var all = await baseQuery.ToListAsync();
        return SortByCosineDistance(all, queryEmbedding, ascending: true).Take(maxItems);
    }

    private IQueryable<Idea> VisibleByCategoryQuery(int categoryId)
        => Context.Set<Idea>()
            .AsNoTracking()
            .Include(i => i.Participant)
            .Include(i => i.Comments).ThenInclude(c => c.Participant)
            .Include($"{nameof(Idea.Comments)}.{nameof(IconComment.Emoji)}")
            .Where(i => i.Category.CategoryId == categoryId
                        && i.Status != ModerationStatus.Flagged
                        && i.Status != ModerationStatus.Declined);

    private static IEnumerable<Idea> SortByCosineDistance(IEnumerable<Idea> ideas, float[] query, bool ascending)
    {
        var ranked = ideas
            .Where(i => i.Embedding != null)
            .Select(i => (idea: i, dist: CosineDistance(i.Embedding.ToArray(), query)));

        return ascending
            ? ranked.OrderBy(x => x.dist).Select(x => x.idea)
            : ranked.OrderByDescending(x => x.dist).Select(x => x.idea);
    }

    private static float CosineDistance(float[] a, float[] b)
    {
        float dot = 0f, magA = 0f, magB = 0f;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        if (magA == 0f || magB == 0f) return 1f;
        return 1f - dot / (MathF.Sqrt(magA) * MathF.Sqrt(magB));
    }
}
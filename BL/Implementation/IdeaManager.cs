using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Embeddings;
using SynapseEnqueteManagement.BL.Sorting;
using SynapseEnqueteManagement.BL.Sorting.Embeddings;
using SynapseEnqueteManagement.DAL;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class IdeaManager(
    IRepository repository,
    ILogger<IdeaManager> logger,
    ISortStrategyFactory<Idea> sortStrategyFactory,
    IIdeaSortStrategyFactory embeddingSortFactory,
    IEmbeddingService embeddingService) : IIdeaManager
{
    // Safety cap for embedding sorts: they materialise the full filtered set in
    // memory. The brief calls for "performant for ≤1,000 ideas" — so that's
    // where we draw the line.
    private const int EmbeddingSortMaxItems = 1000;

    public async Task<PagedResult<Idea>> GetOkIdeasByCategoryAsync(int categoryId, int page, int pageSize, string sortKey, int? excludeParticipantId = null)
    {
        var strategy = sortStrategyFactory.Get(sortKey);
        var filterList = new List<System.Linq.Expressions.Expression<Func<Idea, bool>>>
        {
            i => i.Category.CategoryId == categoryId,
            i => i.Status != ModerationStatus.Flagged && i.Status != ModerationStatus.Declined
        };
        if (excludeParticipantId.HasValue)
            filterList.Add(i => i.Participant.ParticipantId != excludeParticipantId.Value);
        var filters = filterList.ToArray();

        var total = await repository.CountAsync<Idea>(filters);
        var items = await repository.ReadAsync<Idea>(
            filters: filters,
            includes: [i => i.Comments, i => i.Participant],
            includeStrings: ["Comments.Emoji", "Comments.Participant"],
            orderBy: strategy.DbOrderBy,
            randomize: strategy.Randomize,
            skip: (page - 1) * pageSize,
            take: pageSize
        );

        return new PagedResult<Idea> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<PagedResult<Idea>> GetOkIdeasByCategoryEmbeddingSortedAsync(
        int categoryId, int page, int pageSize, string sortKey,
        string queryText, int? excludeParticipantId = null,
        CancellationToken cancellationToken = default)
    {
        var strategy = embeddingSortFactory.Get(sortKey);

        var filterList = new List<System.Linq.Expressions.Expression<Func<Idea, bool>>>
        {
            i => i.Category.CategoryId == categoryId,
            i => i.Status != ModerationStatus.Flagged && i.Status != ModerationStatus.Declined
        };
        if (excludeParticipantId.HasValue)
            filterList.Add(i => i.Participant.ParticipantId != excludeParticipantId.Value);
        var filters = filterList.ToArray();

        // Materialise the full filtered set (capped) — embedding sort cannot be
        // pushed into SQL. Pagination happens in-memory after sorting.
        var all = (await repository.ReadAsync<Idea>(
            filters: filters,
            includes: [i => i.Comments, i => i.Participant],
            includeStrings: ["Comments.Emoji", "Comments.Participant"],
            take: EmbeddingSortMaxItems
        )).ToList();

        if (all.Count == EmbeddingSortMaxItems)
            logger.LogWarning("Embedding sort hit the {Cap}-item cap for category {CategoryId}; results are truncated",
                EmbeddingSortMaxItems, categoryId);

        float[] queryEmbedding = null;
        if (!string.IsNullOrWhiteSpace(queryText))
            queryEmbedding = await embeddingService.GenerateAsync(queryText, cancellationToken);

        var ordered = strategy.Sort(all, queryEmbedding);
        var paged = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<Idea> { Items = paged, Page = page, PageSize = pageSize, TotalCount = ordered.Count };
    }

    public async Task<IEnumerable<Idea>> GetOwnIdeasByCategoryAsync(int categoryId, int participantId)
    {
        return await repository.ReadAsync<Idea>(
            filters:
            [
                i => i.Category.CategoryId == categoryId,
                i => i.Participant.ParticipantId == participantId,
                i => i.Status != ModerationStatus.Declined
            ],
            includes: [i => i.Comments, i => i.Participant],
            includeStrings: ["Comments.Emoji", "Comments.Participant"],
            orderBy: q => q.OrderByDescending(i => i.CreatedAt)
        );
    }

    public async Task<Idea> AddIdeaAsync(int categoryId, int projectId, string description, ModerationStatus status,
        Participant participant)
    {
        var category = await repository.ReadByIdAsync<Category>(categoryId, includes: [c => c.Project], tracked: true);
        if (category == null || category.Project?.ProjectId != projectId)
        {
            logger.LogWarning("Category {CategoryId} not found in project {ProjectId}", categoryId, projectId);
            throw new KeyNotFoundException($"Category {categoryId} not found");
        }

        // Generate embedding at write-time so reads stay cheap. Failure here
        // must not block submission — Embedding is nullable and the sort
        // strategies handle that case. A background backfill job can fill in
        // missing vectors later.
        float[] embedding = null;
        try
        {
            embedding = await embeddingService.GenerateAsync(description);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to generate embedding for new idea; storing without vector");
        }

        var idea = new Idea
        {
            Status = status,
            Description = description,
            Category = category,
            Participant = participant,
            Embedding = embedding,
            CreatedAt = DateTime.UtcNow
        };

        return await repository.CreateAsync(idea);
    }

    public async Task<int> GetIdeaCountByParticipantAsync(int participantId)
    {
        return await repository.CountAsync<Idea>([i => i.Participant.ParticipantId == participantId]);
    }

    public async Task<Idea> GetIdeaByIdAsync(int ideaId)
    {
        return await repository.ReadByIdAsync<Idea>(
            ideaId,
            includes: [i => i.Comments, i => i.Participant],
            includeStrings:
            [
                "Comments.Emoji",
                "Comments.Participant"
            ]
        );
    }
}

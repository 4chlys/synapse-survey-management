using Microsoft.Extensions.Logging;
using Pgvector;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Embeddings;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.BL.Sorting;
using SynapseEnqueteManagement.BL.Sorting.Embeddings;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class IdeaManager(
    IIdeaRepository ideaRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork uow,
    ILogger<IdeaManager> logger,
    ISortStrategyFactory<Idea> sortStrategyFactory,
    IEmbeddingSortStrategyFactory embeddingSortFactory,
    IEmbeddingService embeddingService) : IIdeaManager
{
    // Safety cap for embedding sorts: they materialise the full filtered set in
    // memory when not on PostgreSQL. The brief calls for "performant for ≤1,000 ideas".
    private const int EmbeddingSortMaxItems = 1000;

    public async Task<PagedResult<Idea>> GetVisibleIdeasByCategoryWithCommentsReadOnlyAsync(int categoryId, int pageSize, int page, string sortKey)
    {
        var strategy = sortStrategyFactory.Get(sortKey);
        var total = await ideaRepository.CountVisibleByCategoryAsync(categoryId);
        var items = await ideaRepository.ReadVisibleByCategoryWithCommentsReadOnlyAsync(
            categoryId,
            strategy.DbOrderBy,
            strategy.Randomize,
            (page - 1) * pageSize,
            pageSize);

        return new PagedResult<Idea> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<PagedResult<Idea>> GetVisibleIdeasByCategoryEmbeddingSortedWithCommentsReadOnlyAsync(
        int categoryId, int pageSize, int page, string sortKey, string queryText,
        CancellationToken ct = default)
    {
        float[] rawEmbedding = null;
        if (!string.IsNullOrWhiteSpace(queryText))
            rawEmbedding = await embeddingService.GenerateAsync(queryText, ct);

        var total = await ideaRepository.CountVisibleByCategoryAsync(categoryId);

        // Without a query embedding, "diverse" falls back to centroid-based MMR over
        // an unordered fetch. "similar" / "different" fall back to the regular sort.
        if (rawEmbedding == null)
        {
            if (sortKey == "diverse")
            {
                var all = (await ideaRepository.ReadVisibleByCategoryWithCommentsReadOnlyAsync(
                    categoryId, q => q.OrderBy(i => i.IdeaId), false, 0, EmbeddingSortMaxItems)).ToList();
                var sorted = embeddingSortFactory.Get("diverse").Sort(all, null);
                var paged = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                return new PagedResult<Idea> { Items = paged, Page = page, PageSize = pageSize, TotalCount = total };
            }

            return await GetVisibleIdeasByCategoryWithCommentsReadOnlyAsync(categoryId, pageSize, page, sortKey: null);
        }

        if (sortKey == "diverse")
        {
            var candidates = (await ideaRepository.ReadVisibleByCategoryTopNByEmbeddingReadOnlyAsync(
                categoryId, rawEmbedding, EmbeddingSortMaxItems)).ToList();

            if (candidates.Count == EmbeddingSortMaxItems)
                logger.LogWarning("Diverse sort hit the {Cap}-item cap for category {CategoryId}",
                    EmbeddingSortMaxItems, categoryId);

            var sorted = embeddingSortFactory.Get("diverse").Sort(candidates, rawEmbedding);
            var paged = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return new PagedResult<Idea> { Items = paged, Page = page, PageSize = pageSize, TotalCount = total };
        }

        // similar: ascending cosine distance; different: descending
        bool ascending = sortKey != "different";
        var items = await ideaRepository.ReadVisibleByCategoryOrderedByEmbeddingReadOnlyAsync(
            categoryId, rawEmbedding, ascending, (page - 1) * pageSize, pageSize);

        return new PagedResult<Idea> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public Task<IEnumerable<Idea>> GetParticipantIdeasByCategoryWithCommentsReadOnlyAsync(int categoryId, int participantId)
        => ideaRepository.ReadByCategoryAndParticipantWithCommentsReadOnlyAsync(categoryId, participantId);

    public Task<Idea> GetIdeaByIdWithCommentsReadOnlyAsync(int ideaId)
        => ideaRepository.ReadByIdWithCommentsAndParticipantReadOnlyAsync(ideaId);

    public async Task<AddIdeaResult> AddIdeaAsync(int categoryId, int projectId, string description, ModerationStatus status,
        Participant participant, CancellationToken ct = default)
    {
        var category = await categoryRepository.ReadByIdWithProjectForUpdateAsync(categoryId);
        if (category == null || category.Project?.ProjectId != projectId)
        {
            logger.LogWarning("Category {CategoryId} not found in project {ProjectId}", categoryId, projectId);
            throw new KeyNotFoundException($"Category {categoryId} not found");
        }

        var hadPriorIdeas = await ideaRepository.AnyExistByParticipantAsync(participant.ParticipantId);

        // Generate embedding at write-time so reads stay cheap. Failure must not
        // block submission — Embedding is nullable; sort strategies handle the null case.
        Vector embedding = null;
        try
        {
            var raw = await embeddingService.GenerateAsync(description, ct);
            embedding = new Vector(raw);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to generate embedding for idea; storing without vector");
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

        var created = await ideaRepository.CreateAsync(idea);
        await uow.SaveChangesAsync(ct);

        var shouldAskForContactInfo = !hadPriorIdeas && category.Project.RequestContactInfo;
        return new AddIdeaResult(created.IdeaId, shouldAskForContactInfo);
    }

    public Task<Dictionary<int, int>> CountByCategoriesAsync(IEnumerable<int> categoryIds)
        => ideaRepository.CountByCategoriesAsync(categoryIds);

    public Task<int> CountByCategoryAsync(int categoryId)
        => ideaRepository.CountByCategoryAsync(categoryId);
}

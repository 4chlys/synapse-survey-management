using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Domain.Project.Stats;
using SynapseEnqueteManagement.BL.Embeddings;
using SynapseEnqueteManagement.BL.Helpers;
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
    // memory when not on PostgreSQL. The brief calls for "performant for ≤1,000 ideas".
    private const int EmbeddingSortMaxItems = 1000;

    public async Task<IdeaIndexResult> GetIdeasIndexAsync(int categoryId, int projectId, int? participantId, int pageSize, int page, string sortKey, string participantLanguageCode = null, bool showAllLanguages = false, CancellationToken ct = default)
    {
        var (allowCross, defaultLangCode) = await ideaRepository.ReadProjectFlagsForCategoryReadOnlyAsync(categoryId);
        string filterLanguageCode = (!allowCross || !showAllLanguages)
            ? (participantLanguageCode ?? defaultLangCode)
            : null;

        // Anchor = participant's most recent idea description in this category.
        string anchorText = null;
        if (participantId.HasValue)
            anchorText = await ideaRepository.ReadMostRecentDescriptionByCategoryAndParticipantAsync(categoryId, participantId.Value);
        bool hasOwnIdeas = anchorText != null;

        if (!embeddingSortFactory.IsEmbeddingKey(sortKey))
        {
            var paged = await RegularSortAsync(categoryId, pageSize, page, sortKey, filterLanguageCode);
            return ToIndexResult(paged, sortKey, hasOwnIdeas);
        }

        float[] queryEmbedding = null;
        if (!string.IsNullOrWhiteSpace(anchorText))
            queryEmbedding = await embeddingService.GenerateAsync(anchorText, projectId, ct);

        var resolvedKey = sortKey;
        if (queryEmbedding == null && sortKey != "diverse")
        {
            resolvedKey = "diverse";
            queryEmbedding = null;
        }

        var total = await ideaRepository.CountVisibleByCategoryAsync(categoryId, filterLanguageCode);
        if (total > EmbeddingSortMaxItems)
            logger.LogWarning("Embedding sort '{Key}' hit the {Cap}-item cap for category {CategoryId}",
                resolvedKey, EmbeddingSortMaxItems, categoryId);

        var all = (await ideaRepository.ReadVisibleByCategoryWithCommentsReadOnlyAsync(
            categoryId, q => q.OrderBy(i => i.IdeaId), false, 0, EmbeddingSortMaxItems, filterLanguageCode)).ToList();

        var sorted = embeddingSortFactory.Get(resolvedKey).Sort(all, queryEmbedding);
        var pagedItems = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new IdeaIndexResult
        {
            Items = pagedItems,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            ResolvedSort = resolvedKey,
            HasOwnIdeas = hasOwnIdeas
        };
    }

    private async Task<PagedResult<Idea>> RegularSortAsync(int categoryId, int pageSize, int page, string sortKey, string filterLanguageCode)
    {
        var strategy = sortStrategyFactory.Get(sortKey);
        var total = await ideaRepository.CountVisibleByCategoryAsync(categoryId, filterLanguageCode);
        var items = await ideaRepository.ReadVisibleByCategoryWithCommentsReadOnlyAsync(
            categoryId, strategy.DbOrderBy, strategy.Randomize, (page - 1) * pageSize, pageSize, filterLanguageCode);

        return new PagedResult<Idea> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    private static IdeaIndexResult ToIndexResult(PagedResult<Idea> paged, string resolvedSort, bool hasOwnIdeas) =>
        new()
        {
            Items = paged.Items,
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
            ResolvedSort = resolvedSort,
            HasOwnIdeas = hasOwnIdeas
        };

    public Task<IEnumerable<Idea>> GetParticipantIdeasByCategoryWithCommentsReadOnlyAsync(int categoryId, int participantId)
        => ideaRepository.ReadByCategoryAndParticipantWithCommentsReadOnlyAsync(categoryId, participantId);

    public Task<Idea> GetIdeaByIdWithCommentsReadOnlyAsync(int ideaId)
        => ideaRepository.ReadByIdWithCommentsAndParticipantReadOnlyAsync(ideaId);

    public async Task<int?> GetIdeaAuthorParticipantIdReadOnlyAsync(int ideaId)
    {
        var idea = await ideaRepository.ReadByIdWithParticipantReadOnlyAsync(ideaId);
        return idea?.Participant?.ParticipantId;
    }

    public async Task<AddIdeaResult> AddIdeaAsync(int categoryId, int projectId, string description, ModerationStatus status,
        Participant participant, string imageUrl = null, string submissionLanguageCode = null, CancellationToken ct = default)
    {
        var category = await categoryRepository.ReadByIdWithProjectForUpdateAsync(categoryId);
        if (category == null || category.Project?.ProjectId != projectId)
        {
            logger.LogWarning("Category {CategoryId} not found in project {ProjectId}", categoryId, projectId);
            throw new KeyNotFoundException($"Category {categoryId} not found");
        }

        var hadPriorIdeas = await ideaRepository.AnyExistByParticipantAsync(participant.ParticipantId);

        float[] embedding = null;
        try
        {
            embedding = await embeddingService.GenerateAsync(description, projectId, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to generate embedding for idea; storing without vector");
        }

        var idea = new Idea
        {
            Status = status,
            Description = description,
            ImageUrl = imageUrl,
            Category = category,
            Participant = participant,
            Embedding = embedding,
            Language = ContentLanguageResolver.Resolve(participant, category.Project, submissionLanguageCode),
            CreatedAt = DateTime.UtcNow
        };
        EntityValidator.Validate(idea);
        var created = await ideaRepository.CreateAsync(idea);
        await uow.SaveChangesAsync(ct);

        var shouldAskForContactInfo = !hadPriorIdeas && category.Project.RequestContactInfo;
        return new AddIdeaResult(created.IdeaId, shouldAskForContactInfo);
    }

    public async Task<string> RemoveIdeaImageAsync(int ideaId, int projectId, CancellationToken ct = default)
    {
        var idea = await ideaRepository.ReadByIdWithProjectForUpdateAsync(ideaId);
        if (idea == null || idea.Category?.Project?.ProjectId != projectId)
            throw new KeyNotFoundException($"Idea {ideaId} not found in project {projectId}.");

        var oldUrl = idea.ImageUrl;
        if (string.IsNullOrEmpty(oldUrl)) return null;

        idea.ImageUrl = null;
        await uow.SaveChangesAsync(ct);
        return oldUrl;
    }

    public Task<int> CountVisibleByCategoryAsync(int categoryId, string filterLanguageCode = null)
        => ideaRepository.CountVisibleByCategoryAsync(categoryId, filterLanguageCode);

    public Task<Dictionary<int, int>> CountVisibleByCategoriesAsync(IEnumerable<int> categoryIds, string filterLanguageCode = null)
        => ideaRepository.CountVisibleByCategoriesAsync(categoryIds, filterLanguageCode);

    public Task<List<IdeaMetric>> GetTopIdeasAsync(int projectId, int count)
        => ideaRepository.ReadTopByProjectReadOnlyAsync(projectId, count);

    public Task<List<Idea>> GetLatestApprovedWithCommentsAsync(int projectId, int count)
        => ideaRepository.ReadLatestByProjectWithCommentsReadOnlyAsync(projectId, count);

    public Task<Dictionary<ModerationStatus, int>> CountByStatusAsync(int projectId)
        => ideaRepository.CountByStatusForProjectAsync(projectId);

    public Task<(List<Idea> Items, int Total)> GetIdeasPageAsync(
        int projectId, string search, int? categoryId, ModerationStatus? status, int page, int pageSize)
        => ideaRepository.ReadPagedByProjectWithCommentsReadOnlyAsync(projectId, search, categoryId, status, page, pageSize);

    public async Task SetModerationStatusAsync(int ideaId, ModerationStatus status, int? projectId = null)
    {
        var idea = await ideaRepository.ReadByIdInProjectForUpdateAsync(ideaId, projectId)
            ?? throw new KeyNotFoundException($"Idea {ideaId} not found.");
        idea.Status = status;
        await uow.SaveChangesAsync();
    }

}

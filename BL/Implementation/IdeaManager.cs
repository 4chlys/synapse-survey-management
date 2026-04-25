using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Sorting;
using SynapseEnqueteManagement.DAL.Interfaces;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class IdeaManager(
    IIdeaRepository ideaRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork uow,
    ILogger<IdeaManager> logger,
    ISortStrategyFactory<Idea> sortStrategyFactory) : IIdeaManager
{
    public async Task<PagedResult<Idea>> GetVisibleIdeasByCategoryWithCommentsReadOnlyAsync(int projectId, int categoryId, int page, string sortKey, int pageSize)
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

    public Task<IEnumerable<Idea>> GetParticipantIdeasByCategoryWithCommentsReadOnlyAsync(int categoryId, int participantId)
        => ideaRepository.ReadByCategoryAndParticipantWithCommentsReadOnlyAsync(categoryId, participantId);

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

        var idea = new Idea
        {
            Status = status,
            Description = description,
            Category = category,
            Participant = participant,
            CreatedAt = DateTime.UtcNow
        };

        var created = await ideaRepository.CreateAsync(idea);
        await uow.SaveChangesAsync(ct);

        var shouldAskForContactInfo = !hadPriorIdeas && category.Project.RequestContactInfo;
        return new AddIdeaResult(created.IdeaId, shouldAskForContactInfo);
    }

    public Task<Idea> GetIdeaByIdWithCommentsReadOnlyAsync(int ideaId)
        => ideaRepository.ReadByIdWithCommentsAndParticipantReadOnlyAsync(ideaId);

    public async Task<IReadOnlyDictionary<int, int>> CountByCategoriesAsync(IEnumerable<int> categoryIds)
        => await ideaRepository.CountByCategoriesAsync(categoryIds);

    public Task<int> CountByCategoryAsync(int categoryId)
        => ideaRepository.CountByCategoryAsync(categoryId);
}

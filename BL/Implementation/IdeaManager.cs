using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Sorting;
using SynapseEnqueteManagement.DAL;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class IdeaManager(
    IIdeaRepository ideaRepository,
    IRepository<Category> categoryRepository,
    IUnitOfWork uow,
    ILogger<IdeaManager> logger,
    ISortStrategyFactory<Idea> sortStrategyFactory) : IIdeaManager
{
    public async Task<PagedResult<Idea>> GetVisibleIdeasByCategoryWithCommentsAsync(int categoryId, int page, int pageSize, string sortKey)
    {
        var strategy = sortStrategyFactory.Get(sortKey);
        var total = await ideaRepository.CountVisibleByCategoryAsync(categoryId);
        var items = await ideaRepository.GetVisibleByCategoryWithCommentsReadOnlyAsync(
            categoryId,
            strategy.DbOrderBy,
            strategy.Randomize,
            (page - 1) * pageSize,
            pageSize);

        return new PagedResult<Idea> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public Task<IEnumerable<Idea>> GetParticipantIdeasByCategoryWithCommentsAsync(int categoryId, int participantId)
        => ideaRepository.GetByCategoryAndParticipantWithCommentsReadOnlyAsync(categoryId, participantId);

    public async Task<Idea> AddIdeaAsync(int categoryId, int projectId, string description, ModerationStatus status,
        Participant participant, CancellationToken ct = default)
    {
        var category = await categoryRepository.ReadByIdAsync(categoryId, includes: [c => c.Project], tracked: true);
        if (category == null || category.Project?.ProjectId != projectId)
        {
            logger.LogWarning("Category {CategoryId} not found in project {ProjectId}", categoryId, projectId);
            throw new KeyNotFoundException($"Category {categoryId} not found");
        }

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
        return created;
    }

    public Task<int> GetIdeaCountByParticipantAsync(int participantId)
        => ideaRepository.CountAsync([i => i.Participant.ParticipantId == participantId]);

    public Task<Idea> GetIdeaByIdWithCommentsAsync(int ideaId)
        => ideaRepository.GetByIdWithCommentsAndParticipantReadOnlyAsync(ideaId);
}

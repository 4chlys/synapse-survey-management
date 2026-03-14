using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Sorting;
using SynapseEnqueteManagement.DAL;

namespace SynapseEnqueteManagement.BL;

public class IdeaManager(IRepository repository, ILogger<IdeaManager> logger, ISortStrategyFactory<Idea> sortStrategyFactory) : IIdeaManager
{
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
            includes: [i => i.Comments],
            includeStrings: ["Comments.Emoji", "Comments.Participant"],
            orderBy: strategy.DbOrderBy,
            randomize: strategy.Randomize,
            skip: (page - 1) * pageSize,
            take: pageSize
        );

        return new PagedResult<Idea> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
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
            includes: [i => i.Comments],
            includeStrings: ["Comments.Emoji", "Comments.Participant"],
            orderBy: q => q.OrderByDescending(i => i.CreatedAt)
        );
    }

    public async Task<Idea> AddIdeaAsync(int categoryId, int projectId, string description, ModerationStatus status,
        Participant participant)
    {
        var category = await repository.ReadByIdAsync<Category>(categoryId, includes: [c => c.Project]);
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

        return await repository.CreateAsync(idea);
    }

    public async Task<Idea> GetIdeaByIdAsync(int ideaId)
    {
        return await repository.ReadByIdAsync<Idea>(
            ideaId,
            includes: [i => i.Comments],
            includeStrings:
            [
                "Comments.Emoji",
                "Comments.Participant",
                "Comments.EmojiReactions",
                "Comments.EmojiReactions.Emoji",
                "Comments.EmojiReactions.Participant"
            ]
        );
    }
}
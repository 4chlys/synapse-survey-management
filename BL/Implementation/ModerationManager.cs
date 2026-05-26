using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Helpers;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

// Coordinator (owns no repository): the admin idea-moderation use case. Delegates all Idea
// reads/writes to IIdeaManager and shapes the paged listing (with queue counts) for the admin UI.
public class ModerationManager(IIdeaManager ideaManager) : IModerationManager
{
    public async Task<(List<IdeaFeedItem> Items, int Total, int ApprovedCount, int PendingCount, int FlaggedCount)>
        GetIdeasPagedAsync(int projectId, string search, int? categoryId, ModerationStatus? status, int page, string languageCode, int pageSize = 20)
    {
        var (ideas, total) = await ideaManager.GetIdeasPageAsync(projectId, search, categoryId, status, page, pageSize);

        var queue    = await ideaManager.CountByStatusAsync(projectId);
        var approved = queue.GetValueOrDefault(ModerationStatus.Approved, 0);
        var pending  = queue.GetValueOrDefault(ModerationStatus.Unflagged, 0);
        var flagged  = queue.GetValueOrDefault(ModerationStatus.Flagged, 0);

        var items = ideas.Select(i => new IdeaFeedItem(
            IdeaId:        i.IdeaId,
            Description:   i.Description,
            CategoryName:  CategoryNameResolver.Resolve(i.Category, languageCode),
            CategoryId:    i.Category.CategoryId,
            ReactionCount: i.Comments.OfType<IconComment>().Count(),
            CreatedAt:     i.CreatedAt,
            Status:        i.Status,
            ParticipantEmail: i.Participant?.Email,
            ImageUrl:      i.ImageUrl,
            LanguageCode:  i.Language?.Code
        )).ToList();

        return (items, total, approved, pending, flagged);
    }

    public Task SetIdeaModerationStatusAsync(int ideaId, ModerationStatus status, int? projectId = null)
        => ideaManager.SetModerationStatusAsync(ideaId, status, projectId);
}

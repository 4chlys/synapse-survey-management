using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Domain.Project.Stats;
using SynapseEnqueteManagement.BL.Helpers;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

// Coordinator (owns no repository): composes the Category and Idea managers into the
// project dashboard read model. The cross-aggregate top-categories + top-ideas join and
// the dashboard-specific text truncation live here, not on either aggregate's manager.
public class DashboardManager(
    ICategoryManager categoryManager,
    IIdeaManager ideaManager) : IDashboardManager
{
    public async Task<DashboardMetrics> GetDashboardMetricsAsync(int projectId, string languageCode, string defaultLanguageCode, int topCount = 5)
    {
        var categories = await categoryManager.GetTopCategoriesAsync(projectId, topCount, languageCode, defaultLanguageCode);
        var ideas = (await ideaManager.GetTopIdeasAsync(projectId, topCount)).Select(i => i with
        {
            Description = i.Description.Length > 100 ? i.Description[..100] + "…" : i.Description
        }).ToList();

        return new DashboardMetrics(categories, ideas);
    }

    public async Task<List<IdeaFeedItem>> GetIdeaFeedAsync(int projectId, string languageCode, int count = 10)
    {
        var ideas = await ideaManager.GetLatestApprovedWithCommentsAsync(projectId, count);
        return ideas.Select(i => new IdeaFeedItem(
            IdeaId: i.IdeaId,
            Description: i.Description.Length > 80 ? i.Description[..80] + "…" : i.Description,
            CategoryName: CategoryNameResolver.Resolve(i.Category, languageCode),
            CategoryId: i.Category.CategoryId,
            ReactionCount: i.Comments.OfType<IconComment>().Count(ic => ic.TextComment == null),
            CreatedAt: i.CreatedAt,
            Status: i.Status,
            ParticipantEmail: i.Participant?.Email
        )).ToList();
    }

    public async Task<ModerationQueueResult> GetModerationQueueAsync(int projectId)
    {
        var counts = await ideaManager.CountByStatusAsync(projectId);
        return new ModerationQueueResult(
            Approved: counts.GetValueOrDefault(ModerationStatus.Approved, 0),
            Declined: counts.GetValueOrDefault(ModerationStatus.Declined, 0),
            Flagged: counts.GetValueOrDefault(ModerationStatus.Flagged, 0),
            Unflagged: counts.GetValueOrDefault(ModerationStatus.Unflagged, 0)
        );
    }
}

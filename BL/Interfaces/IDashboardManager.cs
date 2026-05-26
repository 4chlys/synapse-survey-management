using SynapseEnqueteManagement.BL.Domain.Project.Stats;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IDashboardManager
{
    Task<DashboardMetrics> GetDashboardMetricsAsync(int projectId, string languageCode, string defaultLanguageCode, int topCount = 5);
    Task<List<IdeaFeedItem>> GetIdeaFeedAsync(int projectId, string languageCode, int count = 10);
    Task<ModerationQueueResult> GetModerationQueueAsync(int projectId);
}

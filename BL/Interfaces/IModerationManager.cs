using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IModerationManager
{
    Task<(List<IdeaFeedItem> Items, int Total, int ApprovedCount, int PendingCount, int FlaggedCount)>
        GetIdeasPagedAsync(int projectId, string search, int? categoryId, ModerationStatus? status, int page, string languageCode, int pageSize = 20);

    Task SetIdeaModerationStatusAsync(int ideaId, ModerationStatus status, int? projectId = null);
}

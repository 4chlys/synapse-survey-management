namespace SynapseEnqueteManagement.UI.Admin.Models.Platform;

public sealed record PlatformDashboardViewModel(
    int ProjectPlatformId,
    string Name,
    string LogoUrl,
    string WebsiteUrl,
    DateTime CreatedAt,
    int AdminCount,
    int ActiveProjectCount,
    int DraftProjectCount,
    int ArchivedProjectCount,
    int ParticipantCount,
    string AiModelName,
    string CostDashboardUrl
)
{
    public ICollection<ProjectRowViewModel> Projects { get; init; } = [];
}
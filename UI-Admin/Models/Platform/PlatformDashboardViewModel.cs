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
    int ParticipantCount
)
{
    public ICollection<ProjectRowViewModel> Projects { get; init; } = [];
    public ICollection<ProjectAiUsageRowViewModel> AiUsageByProject { get; init; } = [];
}

public sealed class ProjectAiUsageRowViewModel
{
    public int ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public long InputTokens { get; init; }
    public long OutputTokens { get; init; }
    public decimal CostEur { get; init; }
}
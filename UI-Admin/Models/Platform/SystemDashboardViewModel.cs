namespace SynapseEnqueteManagement.UI.Admin.Models.Platform;

public sealed record SystemDashboardViewModel(
    List<PlatformSummaryViewModel> Platforms
)
{
    public double AvgCompletionSeconds { get; init; }
    public double MedianCompletionSeconds { get; init; }
    public ICollection<ProjectRowViewModel> Projects { get; init; } = [];
    public ICollection<ServiceUsageRowViewModel> AiUsageBySystem { get; init; } = [];
    public ICollection<PlatformServiceUsageRowViewModel> AiUsageByPlatform { get; init; } = [];
}

public sealed record PlatformSummaryViewModel(
    int ProjectPlatformId,
    string Name,
    string LogoUrl,
    int AdminCount,
    int ActiveProjectCount,
    int DraftProjectCount,
    int ArchivedProjectCount,
    int ParticipantCount,
    string AiModelName,
    string CostDashboardUrl
);

public class ProjectRowViewModel
{
    public int ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public string PlatformName { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public int ParticipantCount { get; init; }
    public double AvgCompletionSeconds { get; init; }
    public double MedianCompletionSeconds { get; init; }
}

public class ServiceUsageRowViewModel
{
    public string Service { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public long InputTokens { get; init; }
    public long OutputTokens { get; init; }
    public int RequestCount { get; init; }
    public decimal CostEur { get; init; }
}

public class PlatformServiceUsageRowViewModel
{
    public int PlatformId { get; init; }
    public string PlatformName { get; init; } = string.Empty;
    public string Service { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public long InputTokens { get; init; }
    public long OutputTokens { get; init; }
    public int RequestCount { get; init; }
    public decimal CostEur { get; init; }
}
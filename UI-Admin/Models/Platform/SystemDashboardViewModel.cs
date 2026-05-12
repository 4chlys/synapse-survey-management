namespace SynapseEnqueteManagement.UI.Admin.Models.Platform;

public sealed record SystemDashboardViewModel(
    List<PlatformSummaryViewModel> Platforms
);

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
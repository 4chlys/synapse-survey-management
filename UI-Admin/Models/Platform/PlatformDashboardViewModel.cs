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
    string AiModelName,
    string CostDashboardUrl
);
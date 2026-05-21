namespace SynapseEnqueteManagement.UI.Admin.Models.Platform;

public record PlatformConfigurationViewModel
{
    public int ProjectPlatformId { get; init; }
    public string ContactName { get; init; }
    public string ContactEmail { get; init; }
    public string BillingReference { get; init; }
    public bool AiAssistanceEnabled { get; init; }
    public bool IdeationEnabled { get; init; }
    public int? MaxProjects { get; init; }
    public int? MaxAdmins { get; init; }
    public string AiModelName { get; init; }
    public string CostDashboardUrl { get; init; }
}

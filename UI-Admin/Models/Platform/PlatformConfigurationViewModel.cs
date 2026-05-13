namespace SynapseEnqueteManagement.UI.Admin.Models.Platform;

public class PlatformConfigurationViewModel
{
    public int ProjectPlatformId { get; set; }
    public string ContactName { get; set; }
    public string ContactEmail { get; set; }
    public string BillingReference { get; set; }
    public bool AiAssistanceEnabled { get; set; }
    public bool IdeationEnabled { get; set; }
    public int? MaxProjects { get; set; }
    public int? MaxAdmins { get; set; }
    public string AiModelName { get; set; }
    public string CostDashboardUrl { get; set; }
}
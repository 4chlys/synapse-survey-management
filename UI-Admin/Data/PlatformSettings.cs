namespace SynapseEnqueteManagement.UI.Admin.Data;

public class PlatformSettings
{
    public int Id { get; set; }
    public int ProjectPlatformId { get; set; }

    // Contact & billing
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? BillingReference { get; set; }

    // Feature flags
    public bool AiAssistanceEnabled { get; set; } = true;
    public bool IdeationEnabled { get; set; } = true;

    // Quotas
    public int? MaxProjects { get; set; }
    public int? MaxAdmins { get; set; }
}
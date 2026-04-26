using SynapseEnqueteManagement.UI.Admin.Models.Platform;

namespace SynapseEnqueteManagement.UI.Admin.Models.Platform;

public class PlatformDetailViewModel
{
    public int ProjectPlatformId { get; set; }
    public string Name { get; set; }
    public string LogoUrl { get; set; }
    public string WebsiteUrl { get; set; }
    public List<AdminSummaryViewModel> Admins { get; set; }
}

public class AdminSummaryViewModel
{
    public string UserName { get; set; }
    public string Email { get; set; }
}

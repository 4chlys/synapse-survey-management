using SynapseEnqueteManagement.UI.Admin.Models.Platform;

namespace SynapseEnqueteManagement.UI.Admin.Models.Platform;

public record PlatformDetailViewModel
{
    public int ProjectPlatformId { get; init; }
    public string Name { get; init; }
    public string LogoUrl { get; init; }
    public string WebsiteUrl { get; init; }
    public ICollection<AdminSummaryViewModel> Admins { get; init; }
}

public record AdminSummaryViewModel
{
    public string UserId { get; init; }
    public string UserName { get; init; }
    public string Email { get; init; }
}

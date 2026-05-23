namespace SynapseEnqueteManagement.UI.Admin.Models.Platform;

public record PlatformViewModel
{
    public int ProjectPlatformId { get; init; }
    public string Name { get; init; }
    public string LogoUrl { get; init; }
    public string WebsiteUrl { get; init; }
}

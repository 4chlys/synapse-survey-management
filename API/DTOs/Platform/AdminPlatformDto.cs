namespace SynapseEnqueteManagement.API.DTOs.Platform;

public sealed record AdminPlatformDto
{
    public int      ProjectPlatformId { get; init; }
    public string   Name              { get; init; }
    public string   LogoUrl           { get; init; }
    public string   WebsiteUrl        { get; init; }
    public DateTime CreatedAt         { get; init; }
}

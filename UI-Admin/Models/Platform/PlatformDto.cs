namespace SynapseEnqueteManagement.UI.Admin.Models.Platform;

// UI-Admin's local read model for a ProjectPlatform row. Lives on the UI-Admin side of
// the HTTP boundary — no link to the BL/Domain ProjectPlatform entity. Shape mirrors
// API/DTOs/Platform/AdminPlatformDto.cs (which is the wire contract on the API side).
public sealed record PlatformDto
{
    public int      ProjectPlatformId { get; init; }
    public string   Name              { get; init; }
    public string   LogoUrl           { get; init; }
    public string   WebsiteUrl        { get; init; }
    public DateTime CreatedAt         { get; init; }
}

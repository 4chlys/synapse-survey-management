using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Platform;

public sealed record UpdateProjectPlatformDto
{
    [Required, MaxLength(100)]
    public string Name { get; init; }

    [MaxLength(500)]
    public string LogoUrl { get; init; }

    [MaxLength(500)]
    public string WebsiteUrl { get; init; }
}

using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.UI.Admin.Models.Platform;

public record PlatformFormViewModel
{
    public int? ProjectPlatformId { get; init; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; init; }

    [MaxLength(500)]
    public string LogoUrl { get; init; }

    [MaxLength(500)]
    public string WebsiteUrl { get; init; }
}

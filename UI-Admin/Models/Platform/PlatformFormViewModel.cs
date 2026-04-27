using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.UI.Admin.Models.Platform;

public class PlatformFormViewModel
{
    public int? ProjectPlatformId { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; }

    [MaxLength(500)]
    public string LogoUrl { get; set; }

    [MaxLength(500)]
    public string WebsiteUrl { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.UI.Admin.Models.Platform;

public record AddAdminViewModel
{
    public int ProjectPlatformId { get; init; }
    public string PlatformName { get; init; }

    [Required]
    [EmailAddress]
    public string Email { get; init; }

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; init; }
}

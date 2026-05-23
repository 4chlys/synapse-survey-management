using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.UI.Admin.Models.Platform;

public record AddAdminViewModel
{
    public int ProjectPlatformId { get; init; }
    public string PlatformName { get; init; }

    [Required]
    [EmailAddress]
    public string Email { get; init; }

    public string InviteLink { get; init; }
}

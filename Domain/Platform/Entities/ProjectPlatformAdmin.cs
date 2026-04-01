using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.Platform.Enums;

namespace SynapseEnqueteManagement.BL.Domain.Platform.Entities;

public class ProjectPlatformAdmin
{
    public int ProjectPlatformAdminId { get; set; }

    [Required] [MaxLength(255)] public string Email { get; set; }
    [Required] [MaxLength(255)] public string Password { get; set; }
    public PlatformRole PlatformRole { get; set; }
}
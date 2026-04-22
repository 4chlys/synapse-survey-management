using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.BL.Domain.Platform.Entities;

public class SystemAdmin //TODO IdentityUser 
{
    public int SystemAdminId { get; set; }

    [Required] [MaxLength(255)] public string Email { get; set; }
    [Required] [MaxLength(255)] public string Password { get; set; }
}
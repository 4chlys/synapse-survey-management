using Microsoft.AspNetCore.Identity;
using SynapseEnqueteManagement.BL.Domain.Platform.Entities;

namespace SynapseEnqueteManagement.UI.Admin.Data;

public class ApplicationUser : IdentityUser
{
    public int? ProjectPlatformId { get; set; }
    public ProjectPlatform ProjectPlatform { get; set; }
}

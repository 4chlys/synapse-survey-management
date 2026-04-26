using SynapseEnqueteManagement.BL.Domain.Platform.Entities;
using SynapseEnqueteManagement.UI.Admin.Data;

namespace SynapseEnqueteManagement.UI.Admin.Models.Platform;

public class PlatformDetailViewModel
{
    public ProjectPlatform Platform { get; set; }
    public List<ApplicationUser> Admins { get; set; }
}

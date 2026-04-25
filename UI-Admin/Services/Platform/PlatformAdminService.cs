using Microsoft.AspNetCore.Identity;
using SynapseEnqueteManagement.UI.Admin.Data;
namespace SynapseEnqueteManagement.UI.Admin.Services.Platform;
public class PlatformAdminService(AdminDbContext db, UserManager<ApplicationUser> userManager) { }

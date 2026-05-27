using Microsoft.AspNetCore.Identity;

namespace SynapseEnqueteManagement.UI.Admin.Data;

// Identity user for UI-Admin. ProjectPlatformId is a plain scalar FK — the typed
// navigation to the Domain ProjectPlatform entity is intentionally absent so this
// project doesn't reference the Domain assembly. The relationship is declared by
// string name in AdminDbContext.OnModelCreating, matching the codebase's shadow-FK
// convention. UI-Admin reads platform details over HTTP via PlatformApiService.
public class ApplicationUser : IdentityUser
{
    public int? ProjectPlatformId { get; set; }
}

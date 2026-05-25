using SynapseEnqueteManagement.UI.Admin.Models.Platform;

namespace SynapseEnqueteManagement.UI.Admin.Models.Admin;

public sealed record AdminListViewModel(
    List<AdminWithPlatformViewModel> Admins,
    List<PlatformViewModel> Platforms,
    int? FilterPlatformId
);

public sealed record AdminWithPlatformViewModel(
    string UserId,
    string UserName,
    string Email,
    int PlatformId,
    string PlatformName
);
namespace SynapseEnqueteManagement.UI.Admin.Models.Platform;

public sealed record PlatformAdminsViewModel(
    int ProjectPlatformId,
    string PlatformName,
    List<AdminSummaryViewModel> Admins
);

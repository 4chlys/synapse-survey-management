namespace SynapseEnqueteManagement.UI.Admin.Models.Platform;

public sealed record PlatformSettingsViewModel(
    int ProjectPlatformId,
    string PlatformName,
    PlatformFormViewModel BasicInfo,
    PlatformConfigurationViewModel Configuration
);
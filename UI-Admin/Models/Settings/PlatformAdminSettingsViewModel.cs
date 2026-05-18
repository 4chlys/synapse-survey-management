using SynapseEnqueteManagement.BL.Domain.Ai;

namespace SynapseEnqueteManagement.UI.Admin.Models.Settings;

public sealed record PlatformAdminSettingsViewModel(
    int ProjectPlatformId,
    string PlatformName,
    PlatformAdminBasicInfoViewModel BasicInfo,
    PlatformAdminAiViewModel AiSettings,
    PlatformAdminPromptsViewModel Prompts
);

public sealed record PlatformAdminBasicInfoViewModel(
    string ContactName,
    string ContactEmail,
    string BillingReference
);

public sealed record PlatformAdminAiViewModel(
    IReadOnlyList<AiModelDefinition> AllowedModels,
    string SelectedModelId
);

public sealed record PlatformAdminPromptsViewModel(
    IReadOnlyList<PromptOverrideEntry> Entries
);

public sealed record PromptOverrideEntry(
    string PromptType,
    string Label,
    string EffectivePrompt,
    bool IsOverridden
);
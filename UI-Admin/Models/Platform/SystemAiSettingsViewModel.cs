using SynapseEnqueteManagement.UI.Admin.Models.Ai;

namespace SynapseEnqueteManagement.UI.Admin.Models.Platform;

public sealed record SystemAiSettingsViewModel(
    IReadOnlyList<AiModelDefinition> AllModels,
    List<string> AllowedModelIds,
    // Models that are configured/known (have an API key) — NOT necessarily reachable.
    // Actual per-model reachability lives in ModelAvailability (id -> isAvailable).
    HashSet<string> AvailableModelIds,
    Dictionary<string, bool> ModelAvailability
);
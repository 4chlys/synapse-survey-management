using SynapseEnqueteManagement.BL.Domain.Ai;

namespace SynapseEnqueteManagement.UI.Admin.Models.Platform;

public sealed record SystemAiSettingsViewModel(
    IReadOnlyList<AiModelDefinition> AllModels,
    List<string> AllowedModelIds
);
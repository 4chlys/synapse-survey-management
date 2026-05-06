using System.Text.Json.Serialization;

namespace SynapseEnqueteManagement.BL.AiSupport;

public abstract class AiSupportConfigBase
{
    [JsonPropertyName("system_prompt")]
    public string SystemPrompt { get; init; } = string.Empty;

    [JsonPropertyName("context_prompt")]
    public string ContextPrompt { get; init; } = string.Empty;

    [JsonPropertyName("fallback_message")]
    public string FallbackMessage { get; init; } = string.Empty;

    /// <summary>Set by AiConfigLoader from platform.age_range — not a JSON field.</summary>
    public string DefaultAgeRange { get; init; } = "12–30";
}

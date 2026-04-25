using System.Text.Json.Serialization;

namespace SynapseEnqueteManagement.BL.AiCommentSupport;

/// <summary>
/// Configuration for the quick comment-support chat agent.
/// Supports {platform_name}, {language}, {country} substitutions (applied by AiConfigLoader).
/// </summary>
public sealed class AiCommentSupportConfig
{
    /// <summary>
    /// System prompt that establishes the agent's persona and output contract.
    /// </summary>
    [JsonPropertyName("system_prompt")]
    public string SystemPrompt { get; init; } = string.Empty;

    /// <summary>
    /// Context prompt injected on the first turn, priming the agent with the idea text.
    /// Supports {idea_text}, {category_name} substitutions (applied per request).
    /// </summary>
    [JsonPropertyName("context_prompt")]
    public string ContextPrompt { get; init; } = string.Empty;

    /// <summary>User-facing message returned when the AI call fails.</summary>
    [JsonPropertyName("fallback_message")]
    public string FallbackMessage { get; init; } =
        "Sorry, de assistent is even niet beschikbaar. Je kunt je reactie hieronder direct intypen.";
}

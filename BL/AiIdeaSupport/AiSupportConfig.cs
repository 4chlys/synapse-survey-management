using System.Text.Json.Serialization;

namespace SynapseEnqueteManagement.BL.AiIdeaSupport;

/// <summary>
/// Configuration block for the idea-support chat agent.
/// Lives inside <see cref="AiIdeaSupportConfig"/> which is a sibling of
/// <c>moderation</c> in ai-config.json.
/// </summary>
public sealed class AiIdeaSupportConfig
{
    /// <summary>
    /// System prompt that establishes the agent's persona and goals.
    /// Supports {platform_name}, {language}, {country} substitutions.
    /// </summary>
    [JsonPropertyName("system_prompt")]
    public string SystemPrompt { get; init; } = String.Empty;

    /// <summary>
    /// Prompt injected before the user's first message to prime the agent
    /// with category and project context.
    /// Supports {category_name}, {category_description}, {platform_name}.
    /// </summary>
    [JsonPropertyName("context_prompt")]
    public string ContextPrompt { get; init; } = String.Empty;

    /// <summary>
    /// Prompt used for the standalone /refine endpoint.
    /// Supports {draft} substitution.
    /// </summary>
    [JsonPropertyName("refine_prompt")]
    public string RefinePrompt { get; init; } = String.Empty;

    /// <summary>Maximum number of conversation turns kept in context.</summary>
    [JsonPropertyName("max_history_turns")]
    public int MaxHistoryTurns { get; init; } = 10;

    // Built-in defaults
    // These are used when no ai-config.json is present so the feature still
    // works out of the box in development.
}
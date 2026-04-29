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

    /// <summary>
    /// Maximum number of characters of the project introduction sent to the LLM.
    /// Longer texts are trimmed to keep the context window reasonable.
    /// </summary>
    [JsonPropertyName("max_introduction_length")]
    public int MaxIntroductionLength { get; init; } = 1200;

    /// <summary>
    /// Platform-level default age range used when the project has no MinAge/MaxAge configured.
    /// Set by <see cref="AiConfigLoader"/> from <c>platform.age_range</c>; not a JSON field.
    /// </summary>
    public string DefaultAgeRange { get; init; } = "12–30";

    /// <summary>User-facing message returned when the AI call fails or produces no output.</summary>
    [JsonPropertyName("fallback_message")]
    public string FallbackMessage { get; init; } =
        "Sorry, ik kan je nu even niet helpen. Probeer je idee zelf te verwoorden " +
        "en stuur het in — je kunt het altijd later aanpassen.";
}
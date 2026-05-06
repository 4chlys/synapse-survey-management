using System.Text.Json.Serialization;

namespace SynapseEnqueteManagement.BL.AiSocialMedia;

public sealed class SocialMediaConfig
{
    [JsonPropertyName("linkedin")] public PlatformPostConfig LinkedIn { get; init; } = new();
    [JsonPropertyName("facebook")] public PlatformPostConfig Facebook { get; init; } = new();
    [JsonPropertyName("instagram")] public PlatformPostConfig Instagram { get; init; } = new();
    [JsonPropertyName("fallback_message")] public string FallbackMessage { get; init; } =
        "Sorry, de AI is even niet beschikbaar. Probeer het opnieuw.";
}

public sealed class PlatformPostConfig
{
    [JsonPropertyName("system_prompt")] public string SystemPrompt { get; init; } = "";
    [JsonPropertyName("user_prompt_template")] public string UserPromptTemplate { get; init; } = "";
    [JsonPropertyName("max_chars")] public int MaxChars { get; init; } = 2200;
}

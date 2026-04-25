using System.Text.Json.Serialization;
using SynapseEnqueteManagement.BL.AiIdeaSupport;
using SynapseEnqueteManagement.BL.AiModeration;
using SynapseEnqueteManagement.BL.AiSpeechToText;

namespace SynapseEnqueteManagement.BL.Ai;

/// <summary>
/// Root configuration model that maps to ai-config.json.
/// Each feature owns its config class; this is purely the composition root.
/// Populated by <see cref="AiConfigLoader"/> at startup and registered as a singleton.
/// </summary>
public sealed class AiConfig
{
    [JsonPropertyName("platform")]      public PlatformConfig     Platform     { get; init; } = new();
    [JsonPropertyName("moderation")]    public ModerationConfig   Moderation   { get; init; } = new();
    [JsonPropertyName("idea_support")]  public AiIdeaSupportConfig IdeaSupport { get; init; } = new();
    [JsonPropertyName("speech_to_text")] public AiSpeechToTextConfig SpeechToText { get; init; } = new();
    [JsonPropertyName("fuzzy")]         public FuzzyConfig        Fuzzy        { get; init; } = new();
}

/// <summary>Platform identity tokens used for prompt substitution across all features.</summary>
public sealed class PlatformConfig
{
    [JsonPropertyName("name")]     public string Name     { get; init; } = "Synapse";
    [JsonPropertyName("language")] public string Language { get; init; } = "nl";
    [JsonPropertyName("country")]  public string Country  { get; init; } = "België";
}

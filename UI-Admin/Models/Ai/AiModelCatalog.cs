namespace SynapseEnqueteManagement.UI.Admin.Models.Ai;

public enum AiProvider { VertexAi, OpenAi, Anthropic, Grok }

public sealed record AiModelDefinition(string Id, string DisplayName, AiProvider Provider);

// Local copy of the API's allowed-model catalog. This is static reference data used
// to render model pickers and to seed the default allowed-model set. Models cross the
// HTTP boundary as plain id strings, so only the ids must stay aligned with the API;
// DisplayName/Provider are presentation-only. Keep in sync with Domain/Ai/AiModelCatalog.cs.
public static class AiModelCatalog
{
    public static readonly IReadOnlyList<AiModelDefinition> All =
    [
        new("gemini-2.5-flash-lite", "Gemini 2.5 Flash Lite", AiProvider.VertexAi),
        new("gemini-2.0-flash",      "Gemini 2.0 Flash",      AiProvider.VertexAi),
        new("gemini-2.5-flash",      "Gemini 2.5 Flash",      AiProvider.VertexAi),
        new("gemini-2.5-pro",        "Gemini 2.5 Pro",        AiProvider.VertexAi),
        new("gpt-4o-mini",           "GPT-4o mini",           AiProvider.OpenAi),
        new("gpt-4o",                "GPT-4o",                AiProvider.OpenAi),
        new("claude-haiku-4-5",      "Claude Haiku 4.5",      AiProvider.Anthropic),
        new("claude-sonnet-4-6",     "Claude Sonnet 4.6",     AiProvider.Anthropic),
        new("claude-opus-4-7",       "Claude Opus 4.7",       AiProvider.Anthropic),
        new("grok-3-mini",           "Grok 3 mini",           AiProvider.Grok),
        new("grok-3",                "Grok 3",                AiProvider.Grok),
    ];
}

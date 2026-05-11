namespace SynapseEnqueteManagement.BL.Ai;

using SynapseEnqueteManagement.BL.Domain.Ai;

// Re-export from Domain layer for backwards compatibility
public static class AiModelCatalog
{
    public static readonly IReadOnlyList<AiModelDefinition> All => global::SynapseEnqueteManagement.BL.Domain.Ai.AiModelCatalog.All;
}

public static class AiProviderCompat
{
    public const string VertexAi = nameof(AiProvider.VertexAi);
    public const string OpenAi = nameof(AiProvider.OpenAi);
    public const string Anthropic = nameof(AiProvider.Anthropic);
    public const string Grok = nameof(AiProvider.Grok);
}
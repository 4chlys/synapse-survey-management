using System.Text.Json.Serialization;

namespace SynapseEnqueteManagement.BL.AiIdeaSummary;

public sealed class AiIdeaSummaryConfig
{
    [JsonPropertyName("system_prompt")]
    public string SystemPrompt { get; init; } = string.Empty;

    [JsonPropertyName("batch_size")]
    public int BatchSize { get; init; } = 50;

    [JsonPropertyName("max_summary_chars")]
    public int MaxSummaryChars { get; init; } = 50_000;
}
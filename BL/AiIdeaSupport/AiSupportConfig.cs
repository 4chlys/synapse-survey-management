using System.Text.Json.Serialization;
using SynapseEnqueteManagement.BL.AiSupport;

namespace SynapseEnqueteManagement.BL.AiIdeaSupport;

public sealed class AiIdeaSupportConfig : AiSupportConfigBase
{
    [JsonPropertyName("refine_prompt")]
    public string RefinePrompt { get; init; } = string.Empty;

    [JsonPropertyName("max_history_turns")]
    public int MaxHistoryTurns { get; init; } = 10;

    [JsonPropertyName("max_introduction_length")]
    public int MaxIntroductionLength { get; init; } = 1200;
}

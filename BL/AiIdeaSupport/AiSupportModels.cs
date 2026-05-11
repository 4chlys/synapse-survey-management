using System.Text.Json.Serialization;
using SynapseEnqueteManagement.BL.AiSupport;

namespace SynapseEnqueteManagement.BL.AiIdeaSupport;

public enum IdeaSupportPhase
{
    Intro,
    Ideation,
    Draft,
    Confirm,
    Error,
}

public sealed class IdeaSupportAiReply
{
    [JsonPropertyName("phase")]   public string Phase   { get; init; } = string.Empty;
    [JsonPropertyName("message")] public string Message { get; init; } = string.Empty;
    [JsonPropertyName("draft")]   public string Draft   { get; init; }
}

public sealed class IdeaSupportChatRequest
{
    public IReadOnlyList<AiSupportMessage> History             { get; init; } = [];
    public string                          UserMessage         { get; init; } = string.Empty;
    public string                          ProjectName         { get; init; } = string.Empty;
    public string                          ProjectIntroduction { get; init; } = string.Empty;
    public string                          CategoryName        { get; init; } = string.Empty;
    public string                          CategoryDescription { get; init; } = string.Empty;
    public string                          AgeRange            { get; init; }
    public bool                            IsFirstTurn         { get; init; }
}

public sealed class IdeaSupportChatReply
{
    public string           AssistantMessage { get; init; } = string.Empty;
    public string           SuggestedDraft   { get; init; }
    public IdeaSupportPhase Phase            { get; init; } = IdeaSupportPhase.Ideation;
}

public sealed class IdeaSupportRefineResult
{
    public string RefinedText { get; init; }
}

using System.Text.Json.Serialization;
using SynapseEnqueteManagement.BL.AiSupport;

namespace SynapseEnqueteManagement.BL.AiQuestionSupport;

public sealed class QuestionSupportChatRequest
{
    public string                          ProjectName        { get; init; } = string.Empty;
    public string                          ProjectDescription { get; init; } = string.Empty;
    public IReadOnlyList<string>           ExistingQuestions  { get; init; } = [];
    public IReadOnlyList<AiSupportMessage> History            { get; init; } = [];
    public string                          UserMessage        { get; init; } = string.Empty;
    public bool                            IsFirstTurn        { get; init; }
}

public sealed class QuestionSupportChatReply
{
    public string AssistantMessage { get; init; } = string.Empty;
}

internal sealed class QuestionSupportAiReply
{
    [JsonPropertyName("message")] public string Message { get; init; } = string.Empty;
}

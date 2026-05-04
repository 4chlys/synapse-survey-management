using System.Text.Json.Serialization;
using SynapseEnqueteManagement.BL.AiSupport;

namespace SynapseEnqueteManagement.BL.AiCommentSupport;

public sealed class CommentSupportRequest
{
    public string                          IdeaText    { get; init; } = string.Empty;
    public string                          CategoryName{ get; init; } = string.Empty;
    public IReadOnlyList<AiSupportMessage> History     { get; init; } = [];
    public string                          UserMessage { get; init; } = string.Empty;
    public string                          AgeRange    { get; init; }
    public bool                            IsFirstTurn { get; init; }
}

public sealed class CommentSupportReply
{
    public string AssistantMessage { get; init; } = string.Empty;
    public string Suggestion       { get; init; }
    public bool   IsFinal          { get; init; }
}

internal sealed class CommentSupportAiReply
{
    [JsonPropertyName("message")]    public string Message    { get; init; } = string.Empty;
    [JsonPropertyName("suggestion")] public string Suggestion { get; init; }
    [JsonPropertyName("is_final")]   public bool   IsFinal    { get; init; }
}

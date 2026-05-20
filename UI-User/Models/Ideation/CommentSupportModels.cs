namespace SynapseEnqueteManagement.UI.User.Models.Ideation;

public sealed record CommentSupportMessage
{
    public string Role    { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
}

public sealed record CommentSupportChatRequest
{
    public IReadOnlyList<CommentSupportMessage> History      { get; init; } = [];
    public string                               UserMessage  { get; init; } = string.Empty;
    public string                               IdeaText     { get; init; } = string.Empty;
    public string                               CategoryName { get; init; } = string.Empty;
}

public sealed record CommentSupportChatResponse
{
    public string AssistantMessage { get; init; } = string.Empty;
    public string Suggestion       { get; init; }
    public bool   IsFinal          { get; init; }
}

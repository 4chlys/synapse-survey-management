namespace SynapseEnqueteManagement.UI.User.Models.Ideation;

public sealed class CommentSupportMessage
{
    public string Role    { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public sealed class CommentSupportChatRequest
{
    public IReadOnlyList<CommentSupportMessage> History      { get; set; } = [];
    public string                               UserMessage  { get; set; } = string.Empty;
    public string                               IdeaText     { get; set; } = string.Empty;
    public string                               CategoryName { get; set; } = string.Empty;
}

public sealed class CommentSupportChatResponse
{
    public string AssistantMessage { get; set; } = string.Empty;
    public string Suggestion       { get; set; }
    public bool   IsFinal          { get; set; }
}

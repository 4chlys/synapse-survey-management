namespace SynapseEnqueteManagement.API.DTOs.CommentSupport;

public sealed record CommentSupportChatResponseDto
{
    public string AssistantMessage { get; init; } = string.Empty;
    public string Suggestion       { get; init; }
    public bool   IsFinal          { get; init; }
}

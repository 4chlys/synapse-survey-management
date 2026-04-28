namespace SynapseEnqueteManagement.API.DTOs.CommentSupport;

public sealed class CommentSupportChatResponseDto
{
    public string AssistantMessage { get; set; } = string.Empty;
    public string Suggestion       { get; set; }
    public bool   IsFinal          { get; set; }
}

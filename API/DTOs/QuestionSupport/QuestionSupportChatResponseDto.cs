namespace SynapseEnqueteManagement.API.DTOs.QuestionSupport;

public sealed record QuestionSupportChatResponseDto
{
    public string AssistantMessage { get; init; } = string.Empty;
}

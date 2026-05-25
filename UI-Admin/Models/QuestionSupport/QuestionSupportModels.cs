namespace SynapseEnqueteManagement.UI.Admin.Models.QuestionSupport;

public sealed record QuestionSupportMessage
{
    public string Role    { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
}

public sealed record QuestionSupportChatRequest
{
    public IReadOnlyList<QuestionSupportMessage> History     { get; init; } = [];
    public string                                UserMessage { get; init; } = string.Empty;
    public bool                                  IsFirstTurn { get; init; }
}

public sealed record QuestionSupportChatResponse
{
    public string AssistantMessage { get; init; } = string.Empty;
}

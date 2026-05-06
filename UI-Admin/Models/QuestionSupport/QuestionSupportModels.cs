namespace SynapseEnqueteManagement.UI.Admin.Models.QuestionSupport;

public sealed class QuestionSupportMessage
{
    public string Role    { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public sealed class QuestionSupportChatRequest
{
    public IReadOnlyList<QuestionSupportMessage> History     { get; set; } = [];
    public string                                UserMessage { get; set; } = string.Empty;
    public bool                                  IsFirstTurn { get; set; }
}

public sealed class QuestionSupportChatResponse
{
    public string AssistantMessage { get; set; } = string.Empty;
}

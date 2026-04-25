namespace SynapseEnqueteManagement.UI.User.Models.Ideation;

public sealed class IdeaSupportMessage
{
    public string Role    { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public sealed class IdeaSupportChatRequest
{
    public IReadOnlyList<IdeaSupportMessage> History     { get; set; } = [];
    public string                            UserMessage { get; set; } = string.Empty;
    public int                               CategoryId  { get; set; }
}

public sealed class IdeaSupportChatResponse
{
    public string AssistantMessage { get; set; } = string.Empty;
    public string SuggestedDraft   { get; set; }
    public string Phase            { get; set; } = "Ideation";
}

public sealed class IdeaSupportRefineRequest
{
    public string DraftText { get; set; } = string.Empty;
}

public sealed class IdeaSupportRefineResponse
{
    public string RefinedText { get; set; }
}

namespace SynapseEnqueteManagement.UI.User.Models.Ideation;

public sealed record IdeaSupportMessage
{
    public string Role    { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
}

public sealed record IdeaSupportChatRequest
{
    public IReadOnlyList<IdeaSupportMessage> History     { get; init; } = [];
    public string                            UserMessage { get; init; } = string.Empty;
    public int                               CategoryId  { get; init; }
}

public sealed record IdeaSupportChatResponse
{
    public string AssistantMessage { get; init; } = string.Empty;
    public string SuggestedDraft   { get; init; }
    public string Phase            { get; init; } = "Ideation";
}

public sealed record IdeaSupportRefineRequest
{
    public string DraftText { get; init; } = string.Empty;
}

public sealed record IdeaSupportRefineResponse
{
    public string RefinedText { get; init; }
}

namespace SynapseEnqueteManagement.BL.AiIdeaSupport;

/// <summary>
/// A single turn in the idea-support conversation as seen by the BL.
/// The client is responsible for maintaining and sending back the full history
/// so the BL remains stateless.
/// </summary>
public sealed class IdeaSupportChatMessage
{
    /// <summary>"user" or "assistant"</summary>
    public string Role { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;
}

/// <summary>
/// Everything the BL needs to produce the next assistant message.
/// </summary>
public sealed class IdeaSupportChatRequest
{
    /// <summary>Full conversation so far (oldest first), NOT including the latest user turn.</summary>
    public IReadOnlyList<IdeaSupportChatMessage> History { get; init; } = [];

    /// <summary>The user's latest message.</summary>
    public string UserMessage { get; init; } = string.Empty;

    /// <summary>Name of the category the user is posting in.</summary>
    public string CategoryName { get; init; } = string.Empty;

    /// <summary>Optional description of the category for richer context.</summary>
    public string CategoryDescription { get; init; } = string.Empty;

    /// <summary>
    /// True when this is the very first user turn.
    /// The service will inject the context prompt before the history.
    /// </summary>
    public bool IsFirstTurn { get; init; }
}

/// <summary>Reply from the BL to the controller.</summary>
public sealed class IdeaSupportChatReply
{
    public string AssistantMessage { get; init; } = string.Empty;

    /// <summary>
    /// When the agent has gathered enough information it sets this to the
    /// full draft idea text the user can directly submit. Null otherwise.
    /// </summary>
    public string SuggestedDraft { get; init; }
}

/// <summary>Result of the standalone /refine call.</summary>
public sealed class IdeaSupportRefineResult
{
    public string RefinedText { get; init; }
}
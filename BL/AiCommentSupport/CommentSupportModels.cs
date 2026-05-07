using System.Text.Json.Serialization;

namespace SynapseEnqueteManagement.BL.AiCommentSupport;

/// <summary>A single turn in the comment-support conversation.</summary>
public sealed class CommentSupportMessage
{
    /// <summary>"user" or "assistant"</summary>
    public string Role { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;
}

/// <summary>Everything the BL needs to produce the next assistant message.</summary>
public sealed class CommentSupportRequest
{
    /// <summary>Full text of the idea being commented on.</summary>
    public string IdeaText { get; init; } = string.Empty;

    /// <summary>Name of the category the idea belongs to.</summary>
    public string CategoryName { get; init; } = string.Empty;

    /// <summary>Full conversation so far (oldest first), NOT including the latest user turn.</summary>
    public IReadOnlyList<CommentSupportMessage> History { get; init; } = [];

    /// <summary>The user's latest message. Empty on the opening turn.</summary>
    public string UserMessage { get; init; } = string.Empty;

    /// <summary>Age range of the project's target audience (e.g. "14–24"). Null falls back to the platform default.</summary>
    public string AgeRange { get; init; }

    /// <summary>BCP-47 two-letter language code of the participant's UI language (e.g. "nl", "en").</summary>
    public string Language { get; init; } = "nl";

    /// <summary>True when this is the very first turn (AI generates the summary/greeting).</summary>
    public bool IsFirstTurn { get; init; }
}

/// <summary>Reply from the BL to the controller.</summary>
public sealed class CommentSupportReply
{
    public string AssistantMessage { get; init; } = string.Empty;

    /// <summary>Optional polished version of the user's comment. Null when the draft is already good.</summary>
    public string Suggestion       { get; init; }

    /// <summary>True on the second (final) AI turn — no further user input is expected.</summary>
    public bool   IsFinal          { get; init; }
}

/// <summary>Strongly-typed representation of the JSON the AI always returns.</summary>
internal sealed class CommentSupportAiReply
{
    [JsonPropertyName("message")]    public string Message    { get; init; } = string.Empty;
    [JsonPropertyName("suggestion")] public string Suggestion { get; init; }
    [JsonPropertyName("is_final")]   public bool   IsFinal    { get; init; }
}

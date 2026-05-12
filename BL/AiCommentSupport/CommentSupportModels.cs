using System.Text.Json.Serialization;
using SynapseEnqueteManagement.BL.AiSupport;

namespace SynapseEnqueteManagement.BL.AiCommentSupport;

public sealed class CommentSupportRequest
{
    /// <summary>Full text of the idea being commented on.</summary>
    public string IdeaText { get; init; } = string.Empty;

    /// <summary>Name of the category the idea belongs to.</summary>
    public string CategoryName { get; init; } = string.Empty;

    /// <summary>Full conversation so far (oldest first), NOT including the latest user turn.</summary>
    public IReadOnlyList<AiSupportMessage> History { get; init; } = [];

    /// <summary>The user's latest message. Empty on the opening turn.</summary>
    public string UserMessage { get; init; } = string.Empty;

    /// <summary>Age range of the project's target audience (e.g. "14–24"). Null falls back to the platform default.</summary>
    public string AgeRange { get; init; }

    /// <summary>BCP-47 two-letter language code of the participant's UI language (e.g. "nl", "en").</summary>
    public string Language { get; init; } = "nl";

    /// <summary>True when this is the very first turn (AI generates the summary/greeting).</summary>
    public bool IsFirstTurn { get; init; }
}

public sealed class CommentSupportReply
{
    public string AssistantMessage { get; init; } = string.Empty;
    public string Suggestion       { get; init; }
    public bool   IsFinal          { get; init; }
}

internal sealed class CommentSupportAiReply
{
    [JsonPropertyName("message")]    public string Message    { get; init; } = string.Empty;
    [JsonPropertyName("suggestion")] public string Suggestion { get; init; }
    [JsonPropertyName("is_final")]   public bool   IsFinal    { get; init; }
}

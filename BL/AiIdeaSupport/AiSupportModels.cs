using System.Text.Json.Serialization;
using SynapseEnqueteManagement.BL.AiSupport;

namespace SynapseEnqueteManagement.BL.AiIdeaSupport;

public enum IdeaSupportPhase
{
    Intro,
    Ideation,
    Draft,
    Confirm,
    Error,
}

public sealed record IdeaSupportAiReply
{
    [JsonPropertyName("phase")]   public string Phase   { get; init; } = string.Empty;
    [JsonPropertyName("message")] public string Message { get; init; } = string.Empty;
    [JsonPropertyName("draft")]   public string Draft   { get; init; }
}

public sealed record IdeaSupportChatRequest
{
    /// <summary>Full conversation so far (oldest first), NOT including the latest user turn.</summary>
    public IReadOnlyList<AiSupportMessage> History { get; init; } = [];

    /// <summary>The user's latest message.</summary>
    public string UserMessage { get; init; } = string.Empty;

    /// <summary>Name of the organising project / platform instance.</summary>
    public string ProjectName { get; init; } = string.Empty;

    /// <summary>Introduction text of the project — gives the AI context on purpose and expectations.</summary>
    public string ProjectIntroduction { get; init; } = string.Empty;

    /// <summary>Name of the category the user is posting in.</summary>
    public string CategoryName { get; init; } = string.Empty;

    /// <summary>Optional description of the category for richer context.</summary>
    public string CategoryDescription { get; init; } = string.Empty;

    /// <summary>Age range of the project's target audience (e.g. "14–24"). Null falls back to the platform default.</summary>
    public string AgeRange { get; init; }

    /// <summary>BCP-47 two-letter language code of the participant's UI language (e.g. "nl", "en").</summary>
    public string Language { get; init; } = "nl";

    /// <summary>
    /// True when this is the very first user turn.
    /// The service will inject the context prompt before the history.
    /// </summary>
    public bool IsFirstTurn { get; init; }
}

public sealed record IdeaSupportChatReply
{
    public string           AssistantMessage { get; init; } = string.Empty;
    public string           SuggestedDraft   { get; init; }
    public IdeaSupportPhase Phase            { get; init; } = IdeaSupportPhase.Ideation;
}

public sealed record IdeaSupportRefineResult
{
    public string RefinedText { get; init; }
}

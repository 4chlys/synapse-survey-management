namespace SynapseEnqueteManagement.API.DTOs.IdeaSupport;

public record IdeaSupportChatResponseDto
{
    public string AssistantMessage { get; init; } = string.Empty;
    public string SuggestedDraft   { get; init; }

    /// <summary>
    /// One of: Intro | Ideation | Draft | Confirm | Error
    /// The frontend uses this to drive its state machine — no string-parsing needed.
    /// </summary>
    public string Phase { get; init; } = "Ideation";
}
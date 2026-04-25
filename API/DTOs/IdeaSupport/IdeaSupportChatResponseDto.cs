namespace SynapseEnqueteManagement.API.DTOs.IdeaSupport;

public class IdeaSupportChatResponseDto
{
    public string AssistantMessage { get; set; } = string.Empty;
    public string SuggestedDraft   { get; set; }

    /// <summary>
    /// One of: Intro | Ideation | Draft | Confirm | Error
    /// The frontend uses this to drive its state machine — no string-parsing needed.
    /// </summary>
    public string Phase { get; set; } = "Ideation";
}
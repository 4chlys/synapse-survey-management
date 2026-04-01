namespace SynapseEnqueteManagement.API.DTOs.IdeaSupport;

public class IdeaSupportChatResponseDto
{
    public string AssistantMessage { get; set; } = string.Empty;
 
    /// <summary>
    /// Non-null when the agent has generated a ready-to-submit draft.
    /// The client should offer a "use this idea" button when this is present.
    /// </summary>
    public string SuggestedDraft { get; set; }
}
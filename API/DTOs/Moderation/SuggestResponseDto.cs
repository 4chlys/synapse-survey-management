namespace SynapseEnqueteManagement.API.DTOs.Moderation;

public sealed class SuggestResponseDto
{
    public string Suggestion { get; set; }

    /// <summary>
    /// Token for the suggested (rewritten) text — pre-approved as Unflagged.
    /// Only present when Suggestion is not null.
    /// </summary>
    public string ModerationToken { get; set; }
}

namespace SynapseEnqueteManagement.API.DTOs.Moderation;

public sealed record SuggestResponseDto
{
    public string Suggestion { get; init; }

    /// <summary>
    /// Token for the suggested (rewritten) text — pre-approved as Unflagged.
    /// Only present when Suggestion is not null.
    /// </summary>
    public string ModerationToken { get; init; }
}

namespace SynapseEnqueteManagement.API.DTOs.Moderation;

public sealed record ModerationResponseDto
{
    public bool IsAcceptable { get; init; }
    public string Reason { get; init; }
    public string FlaggedBy { get; init; }

    /// <summary>
    /// Signed token encoding the moderation result for the checked text.
    /// Pass this token when submitting the idea/comment so the API can
    /// verify the result without re-running the AI check.
    /// </summary>
    public string ModerationToken { get; init; }
}

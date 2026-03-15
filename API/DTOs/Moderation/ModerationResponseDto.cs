namespace SynapseEnqueteManagement.API.DTOs.Moderation;

public sealed class ModerationResponseDto
{
    public bool IsAcceptable { get; set; }
    public string Reason { get; set; }
    public string FlaggedBy { get; set; }

    /// <summary>
    /// Signed token encoding the moderation result for the checked text.
    /// Pass this token when submitting the idea/comment so the API can
    /// verify the result without re-running the AI check.
    /// </summary>
    public string ModerationToken { get; set; }
}

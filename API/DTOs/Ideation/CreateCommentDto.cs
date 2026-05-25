using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Ideation;

public record CreateCommentDto
{
    [Required]
    [MaxLength(500)]
    public string Text { get; init; }

    /// <summary>
    /// Signed token from the moderation endpoint. If valid and the text
    /// matches, the server uses the encoded status. If missing/invalid/expired,
    /// the server runs moderation again.
    /// </summary>
    public string ModerationToken { get; init; }
    public string LanguageCode { get; init; }
}

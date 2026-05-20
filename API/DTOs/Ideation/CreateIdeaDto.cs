using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Ideation;

public record CreateIdeaDto
{
    [Required]
    [MaxLength(1000)]
    public string Description { get; init; }

    /// <summary>
    /// Signed token from the moderation endpoint. If valid and the text
    /// matches, the server uses the encoded status. If missing/invalid/expired,
    /// the server runs moderation again.
    /// </summary>
    public string ModerationToken { get; init; }

    [MaxLength(500)] public string ImageUrl { get; init; }
    public string ImageModerationToken { get; init; }
    public string LanguageCode { get; init; }
}

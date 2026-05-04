using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Ideation;

public class CreateIdeaDto
{
    [Required]
    [MaxLength(1000)]
    public string Description { get; set; }

    /// <summary>
    /// Signed token from the moderation endpoint. If valid and the text
    /// matches, the server uses the encoded status. If missing/invalid/expired,
    /// the server runs moderation again.
    /// </summary>
    public string ModerationToken { get; set; }

    [MaxLength(500)] public string ImageUrl { get; set; }
    public string ImageModerationToken { get; set; }
}

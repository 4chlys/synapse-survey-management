using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs;

public class CreateIdeaDto
{
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string Description { get; set; }

    /// <summary>
    /// Signed token from the moderation endpoint. If valid and the text
    /// matches, the server uses the encoded status. If missing/invalid/expired,
    /// the server runs moderation again.
    /// </summary>
    public string ModerationToken { get; set; }
}
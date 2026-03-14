using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs;

public class CreateCommentDto
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Text { get; set; }

    /// <summary>
    /// Signed token from the moderation endpoint. If valid and the text
    /// matches, the server uses the encoded status. If missing/invalid/expired,
    /// the server runs moderation again.
    /// </summary>
    public string ModerationToken { get; set; }
}
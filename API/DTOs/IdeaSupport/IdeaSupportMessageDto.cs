using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.IdeaSupport;

public class IdeaSupportMessageDto
{
    /// <summary>"user" or "assistant"</summary>
    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.IdeaSupport;

public class IdeaSupportMessageDto
{
    /// <summary>"user" or "assistant"</summary>
    [Required]
    public string Role { get; set; } = string.Empty;
 
    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;
}
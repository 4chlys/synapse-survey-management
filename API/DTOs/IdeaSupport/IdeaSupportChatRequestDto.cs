using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.IdeaSupport;

public class IdeaSupportChatRequestDto
{
    /// <summary>
    /// Full conversation history excluding the current user message
    /// (oldest turn first). May be empty on the first call.
    /// </summary>
    public IReadOnlyList<IdeaSupportMessageDto> History { get; set; } = [];
 
    /// <summary>The user's latest message.</summary>
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string UserMessage { get; set; } = string.Empty;
 
    /// <summary>Id of the category the participant is posting in.</summary>
    [Required]
    public int CategoryId { get; set; }
}
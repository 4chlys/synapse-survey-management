using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.IdeaSupport;

public class IdeaSupportChatRequestDto
{
    /// <summary>
    /// Full conversation history excluding the current user message
    /// (oldest turn first). May be empty on the first call.
    /// Capped at 30 entries (15 turns × 2 roles) to prevent context flooding.
    /// </summary>
    [MaxLength(30)]
    public IReadOnlyList<IdeaSupportMessageDto> History { get; set; } = [];

    /// <summary>
    /// The user's latest message. Empty string on the opening turn — the
    /// server uses IsFirstTurn + category context to generate the greeting.
    /// </summary>
    [MaxLength(1000)]
    public string UserMessage { get; set; } = string.Empty;

    /// <summary>Id of the category the participant is posting in.</summary>
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}

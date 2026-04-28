using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.CommentSupport;

public sealed class CommentSupportChatRequestDto
{
    /// <summary>
    /// Full conversation history (oldest first), excluding the current user message.
    /// Capped at 6 entries (3 turns × 2 roles) — the feature intentionally limits depth.
    /// </summary>
    [MaxLength(6)]
    public IReadOnlyList<CommentSupportMessageDto> History { get; set; } = [];

    /// <summary>The user's latest message. Empty on the opening turn.</summary>
    [StringLength(500)]
    public string UserMessage { get; set; } = string.Empty;

    /// <summary>Full text of the idea being commented on (used to prime the AI).</summary>
    [Required, StringLength(2000)]
    public string IdeaText { get; set; } = string.Empty;

    /// <summary>Name of the category the idea belongs to.</summary>
    [StringLength(200)]
    public string CategoryName { get; set; } = string.Empty;
}

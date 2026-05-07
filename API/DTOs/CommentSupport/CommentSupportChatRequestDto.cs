using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.API.DTOs.AiSupport;

namespace SynapseEnqueteManagement.API.DTOs.CommentSupport;

public sealed class CommentSupportChatRequestDto
{
    [MaxLength(6)]
    public IReadOnlyList<AiSupportMessageDto> History { get; set; } = [];

    [StringLength(500)]
    public string UserMessage { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string IdeaText { get; set; } = string.Empty;

    [StringLength(200)]
    public string CategoryName { get; set; } = string.Empty;
}

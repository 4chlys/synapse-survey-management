using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.API.DTOs.AiSupport;

namespace SynapseEnqueteManagement.API.DTOs.CommentSupport;

public sealed record CommentSupportChatRequestDto
{
    [MaxLength(6)]
    public IReadOnlyList<AiSupportMessageDto> History { get; init; } = [];

    [StringLength(500)]
    public string UserMessage { get; init; } = string.Empty;

    [Required, StringLength(2000)]
    public string IdeaText { get; init; } = string.Empty;

    [StringLength(200)]
    public string CategoryName { get; init; } = string.Empty;
}

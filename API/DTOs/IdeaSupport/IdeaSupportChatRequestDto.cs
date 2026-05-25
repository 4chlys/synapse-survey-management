using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.API.DTOs.AiSupport;

namespace SynapseEnqueteManagement.API.DTOs.IdeaSupport;

public record IdeaSupportChatRequestDto
{
    [MaxLength(30)]
    public IReadOnlyList<AiSupportMessageDto> History { get; init; } = [];

    [MaxLength(1000)]
    public string UserMessage { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CategoryId { get; init; }
}

using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.API.DTOs.AiSupport;

namespace SynapseEnqueteManagement.API.DTOs.IdeaSupport;

public class IdeaSupportChatRequestDto
{
    [MaxLength(30)]
    public IReadOnlyList<AiSupportMessageDto> History { get; set; } = [];

    [MaxLength(1000)]
    public string UserMessage { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}

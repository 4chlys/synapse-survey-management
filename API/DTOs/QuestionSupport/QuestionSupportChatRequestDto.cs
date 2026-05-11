using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.API.DTOs.AiSupport;

namespace SynapseEnqueteManagement.API.DTOs.QuestionSupport;

public sealed class QuestionSupportChatRequestDto
{
    [MaxLength(20)]
    public IReadOnlyList<AiSupportMessageDto> History { get; set; } = [];

    [MaxLength(1000)]
    public string UserMessage { get; set; } = string.Empty;

    public bool IsFirstTurn { get; set; }
}

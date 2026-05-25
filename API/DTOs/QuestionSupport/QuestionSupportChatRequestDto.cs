using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.API.DTOs.AiSupport;

namespace SynapseEnqueteManagement.API.DTOs.QuestionSupport;

public sealed record QuestionSupportChatRequestDto
{
    [MaxLength(20)]
    public IReadOnlyList<AiSupportMessageDto> History { get; init; } = [];

    [MaxLength(1000)]
    public string UserMessage { get; init; } = string.Empty;

    public bool IsFirstTurn { get; init; }
}

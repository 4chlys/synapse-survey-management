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

    // Language code of the modal tab the admin is currently editing. The BL prompt
    // builds chat replies in CurrentUICulture but requires *survey question text*
    // to be written in this language. When null, the project default is used.
    [MaxLength(10)]
    public string QuestionLanguage { get; init; }
}

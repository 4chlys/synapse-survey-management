using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Survey;

public record EndingAnswerTranslationDto
{
    [Required] [MaxLength(10)] public string LanguageCode { get; init; }
    [Required] [MaxLength(500)] public string Value { get; init; }
}

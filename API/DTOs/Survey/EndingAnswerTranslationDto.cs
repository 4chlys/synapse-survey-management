using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Survey;

public class EndingAnswerTranslationDto
{
    [Required] [MaxLength(10)] public string LanguageCode { get; set; }
    [Required] [MaxLength(500)] public string Value { get; set; }
}

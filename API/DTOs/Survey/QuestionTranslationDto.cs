using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Survey;

public record QuestionTranslationDto
{
    [Required] [MaxLength(10)] public string LanguageCode { get; init; }
    [Required] [MaxLength(500)] public string Text { get; init; }
    public ICollection<string> AnswerChoices { get; init; } = new List<string>();
    [MaxLength(2000)] public string MediaText { get; init; }
}

using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Survey;

public class QuestionTranslationDto
{
    [Required] [MaxLength(10)] public string LanguageCode { get; set; }
    [Required] [MaxLength(500)] public string Text { get; set; }
    public ICollection<string> AnswerChoices { get; set; } = new List<string>();
    [MaxLength(2000)] public string MediaText { get; set; }
}

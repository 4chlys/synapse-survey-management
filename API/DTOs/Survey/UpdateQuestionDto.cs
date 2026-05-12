using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.API.DTOs.Survey;

public class UpdateQuestionDto
{
    [Required] [Range(0, int.MaxValue)]
    public int AnswerChoiceCount { get; set; }

    [Required] [MaxLength(50)]
    public string QuestionFormat { get; set; }

    public bool IsMandatory { get; set; }

    public int? SectionId { get; set; }

    [Required]
    public ICollection<QuestionTranslationDto> Translations { get; set; } = new List<QuestionTranslationDto>();

    public ICollection<ICollection<EndingAnswerTranslationDto>> EndingAnswers { get; set; } = new List<ICollection<EndingAnswerTranslationDto>>();

    [MaxLength(500)]
    public string MediaUrl { get; set; }

    public QuestionMediaType? MediaType { get; set; }

    public bool MediaIsEmbed { get; set; }
}

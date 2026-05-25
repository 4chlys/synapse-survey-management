using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.API.DTOs.Survey;

public record CreateQuestionDto
{
    [Required] [Range(0, int.MaxValue)]
    public int AnswerChoiceCount { get; init; }

    [Required] [MaxLength(50)]
    public string QuestionFormat { get; init; }

    public bool IsMandatory { get; init; }

    public int? SectionId { get; init; }

    public int? ParentQuestionId { get; init; }

    public int? ChoiceIndex { get; init; }

    [Required]
    public ICollection<QuestionTranslationDto> Translations { get; init; } = new List<QuestionTranslationDto>();

    public ICollection<ICollection<EndingAnswerTranslationDto>> EndingAnswers { get; init; } = new List<ICollection<EndingAnswerTranslationDto>>();

    [MaxLength(500)]
    public string MediaUrl { get; init; }

    public QuestionMediaType? MediaType { get; init; }

    public bool MediaIsEmbed { get; init; }
}

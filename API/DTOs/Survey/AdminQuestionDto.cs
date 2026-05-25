using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.API.DTOs.Survey;

public record AdminQuestionDto
{
    public int QuestionId { get; init; }
    public string QuestionFormat { get; init; }
    public bool IsMandatory { get; init; }
    public int Position { get; init; }
    public int AnswerChoiceCount { get; init; }
    public int? ChoiceIndex { get; init; }
    public int? ParentQuestionId { get; init; }
    public int? SectionId { get; init; }
    public string SectionName { get; init; }
    public string MediaUrl { get; init; }
    public QuestionMediaType? MediaType { get; init; }
    public bool MediaIsEmbed { get; init; }
    public ICollection<QuestionTranslationDto> Translations { get; init; } = new List<QuestionTranslationDto>();
    public ICollection<EndingAnswerDto> EndingAnswers { get; init; } = new List<EndingAnswerDto>();
}

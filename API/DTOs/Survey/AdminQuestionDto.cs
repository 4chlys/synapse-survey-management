using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.API.DTOs.Survey;

public class AdminQuestionDto
{
    public int QuestionId { get; set; }
    public string QuestionFormat { get; set; }
    public bool IsMandatory { get; set; }
    public int Position { get; set; }
    public int AnswerChoiceCount { get; set; }
    public int? ChoiceIndex { get; set; }
    public int? ParentQuestionId { get; set; }
    public int? SectionId { get; set; }
    public string SectionName { get; set; }
    public string MediaUrl { get; set; }
    public QuestionMediaType? MediaType { get; set; }
    public bool MediaIsEmbed { get; set; }
    public ICollection<QuestionTranslationDto> Translations { get; set; } = new List<QuestionTranslationDto>();
    public ICollection<EndingAnswerDto> EndingAnswers { get; set; } = new List<EndingAnswerDto>();
}

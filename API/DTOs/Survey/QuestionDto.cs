using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.API.DTOs.Survey;

public record QuestionDto
{
    public int QuestionId { get; init; }
    public string Text { get; init; }
    public string QuestionFormat { get; init; }
    public ICollection<string> AnswerChoices { get; init; } = new List<string>();
    public bool IsMandatory { get; init; }
    public int Position { get; init; }
    public int AnswerChoiceCount { get; init; }
    public int? ChoiceIndex { get; init; }
    public int? ParentQuestionId { get; init; }
    public ICollection<string> EndingAnswers { get; init; } = new List<string>();
    public string SectionName { get; init; }
    public int? SectionId { get; init; }
    public string MediaUrl { get; init; }
    public QuestionMediaType? MediaType { get; init; }
    public bool MediaIsEmbed { get; init; }
    public string MediaText { get; init; }
}

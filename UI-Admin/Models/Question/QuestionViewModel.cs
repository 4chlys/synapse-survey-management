using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.UI.Admin.Models.Question;

public class QuestionViewModel
{
    public int QuestionId { get; set; }
    public string Text { get; set; }
    public string QuestionFormat { get; set; }
    public bool IsMandatory { get; set; }
    public ICollection<string> AnswerChoices { get; set; } = new List<string>();
    public int AnswerChoiceCount { get; set; }
    public int Position { get; set; }
    public int? ParentQuestionId { get; set; }
    public int? ChoiceIndex { get; set; }
    public ICollection<string> EndingAnswers { get; set; } = new List<string>();
    public string SectionName { get; set; }
    public int? SectionId { get; set; }
    public string MediaUrl { get; set; }
    public QuestionMediaType? MediaType { get; set; }
    public bool MediaIsEmbed { get; set; }
    public string MediaText { get; set; }
}

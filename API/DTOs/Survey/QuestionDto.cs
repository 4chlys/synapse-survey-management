namespace SynapseEnqueteManagement.API.DTOs.Survey;

public class QuestionDto
{
    public int QuestionId { get; set; }
    public string Text { get; set; }
    public string QuestionFormat { get; set; }
    public ICollection<string> AnswerChoices { get; set; } = new List<string>();
    public bool IsMandatory { get; set; }
    public int Position { get; set; }
    public int? ParentQuestionId { get; set; }
    public string ParentTriggerValue { get; set; }
    public ICollection<string> EndingAnswers { get; set; } = new List<string>();
    public string SectionName { get; set; }
    public int? SectionId { get; set; }
}

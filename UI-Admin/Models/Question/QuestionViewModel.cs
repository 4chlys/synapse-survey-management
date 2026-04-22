namespace SynapseEnqueteManagement.UI.Admin.Models.Question;

public class QuestionViewModel
{
    public int QuestionId { get; set; }
    public string Text { get; set; }
    public string QuestionFormat { get; set; }
    public bool IsMandatory { get; set; }
    public List<string> AnswerChoices { get; set; } = new();
    public string SectionName { get; set; }
}
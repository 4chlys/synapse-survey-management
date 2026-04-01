namespace SynapseEnqueteManagement.UI.Admin.Models.Question;

public class QuestionViewModel
{
    public int QuestionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string QuestionFormat { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
    public List<string> AnswerChoices { get; set; } = new();
}
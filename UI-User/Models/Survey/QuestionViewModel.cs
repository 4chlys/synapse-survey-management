namespace SynapseEnqueteManagement.UI.User.Models.Survey;

public class QuestionViewModel
{
    public int QuestionId { get; set; }
    public string Text { get; set; }
    public QuestionFormat Format { get; set; }
    public List<string> AnswerChoices { get; set; }
    public int QuestionNumber { get; set; }
    public int TotalQuestions { get; set; }
    public bool IsMandatory { get; set; }
    public int Position { get; set; }
}

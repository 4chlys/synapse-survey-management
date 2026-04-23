namespace SynapseEnqueteManagement.UI.Admin.Models.Question;

public class QuestionNodeViewModel
{
    public QuestionViewModel Question { get; set; }
    public List<QuestionViewModel> AllQuestions { get; set; } = new();
    public int Depth { get; set; }
}

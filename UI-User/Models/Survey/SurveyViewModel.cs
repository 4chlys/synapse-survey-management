namespace SynapseEnqueteManagement.UI.User.Models.Survey;

public class SurveyViewModel
{
    public string Title { get; set; }
    public List<QuestionViewModel> Questions { get; set; }
    public Dictionary<int, string> ExistingResponses { get; set; } = new();
    public bool HasSession { get; set; }
}

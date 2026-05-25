namespace SynapseEnqueteManagement.UI.User.Models.Survey;

public record SurveyViewModel
{
    public string Title { get; init; }
    public ICollection<QuestionViewModel> Questions { get; init; }
    public Dictionary<int, string> ExistingResponses { get; init; } = new();
    public bool HasSession { get; init; }
}

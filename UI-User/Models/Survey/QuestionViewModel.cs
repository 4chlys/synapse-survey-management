namespace SynapseEnqueteManagement.UI.User.Models.Survey;

public record QuestionViewModel
{
    public int QuestionId { get; init; }
    public string Text { get; init; }
    public QuestionFormat Format { get; init; }
    public ICollection<string> AnswerChoices { get; init; }
    public int QuestionNumber { get; init; }
    public int TotalQuestions { get; init; }
    public bool IsMandatory { get; init; }
    public int Position { get; init; }
    public string MediaUrl { get; init; }
    public QuestionMediaType? MediaType { get; init; }
    public bool MediaIsEmbed { get; init; }
    public string MediaText { get; init; }
}

namespace SynapseEnqueteManagement.UI.Admin.Models.Question;

// Edit payload — same multilingual shape as create, plus the question id.
public record EditQuestionViewModel : CreateQuestionViewModel
{
    public int QuestionId { get; init; }
}

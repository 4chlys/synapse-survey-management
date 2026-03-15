namespace SynapseEnqueteManagement.API.DTOs.Survey;

public class QuestionDto
{
    public int QuestionId { get; set; }
    public string Text { get; set; }
    public string QuestionFormat { get; set; }
    public List<string> AnswerChoices { get; set; } = new();
    public bool IsMandatory { get; set; }
}

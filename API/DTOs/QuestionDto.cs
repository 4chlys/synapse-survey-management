namespace SynapseEnqueteManagement.API.DTOs;

public class QuestionDto
{
    public int QuestionId { get; set; }
    public string Text { get; set; }
    public string QuestionFormat { get; set; }
    public List<string> AnswerChoices { get; set; } = new();
}

namespace SynapseEnqueteManagement.API.DTOs.Survey;

public class QuestionnaireDto
{
    public int QuestionnaireId { get; set; }
    public List<QuestionDto> Questions { get; set; } = new();
}

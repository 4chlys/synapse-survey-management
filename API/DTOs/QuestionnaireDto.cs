namespace SynapseEnqueteManagement.API.DTOs;

public class QuestionnaireDto
{
    public int QuestionnaireId { get; set; }
    public List<QuestionDto> Questions { get; set; } = new();
}

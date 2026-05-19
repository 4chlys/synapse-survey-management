namespace SynapseEnqueteManagement.API.DTOs.Survey;

public class QuestionnaireDto
{
    public int QuestionnaireId { get; set; }
    public ICollection<QuestionDto> Questions { get; set; } = new List<QuestionDto>();
}

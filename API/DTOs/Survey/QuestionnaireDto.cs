namespace SynapseEnqueteManagement.API.DTOs.Survey;

public record QuestionnaireDto
{
    public int QuestionnaireId { get; init; }
    public ICollection<QuestionDto> Questions { get; init; } = new List<QuestionDto>();
}

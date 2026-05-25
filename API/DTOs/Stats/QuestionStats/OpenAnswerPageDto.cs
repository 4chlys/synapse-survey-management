namespace SynapseEnqueteManagement.API.DTOs.Stats.QuestionStats;

public record OpenAnswerPageDto
{
    public int QuestionId { get; init; }
    public int TotalAnswers { get; init; }
    public ICollection<OpenAnswerDto> Answers { get; init; } = new List<OpenAnswerDto>();
}

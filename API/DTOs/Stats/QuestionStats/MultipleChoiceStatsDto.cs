namespace SynapseEnqueteManagement.API.DTOs.Stats.QuestionStats;

public record MultipleChoiceStatsDto
{
    public int QuestionId { get; init; }
    public int TotalResponses { get; init; }
    public ICollection<OptionCountDto> Counts { get; init; } = new List<OptionCountDto>();
}

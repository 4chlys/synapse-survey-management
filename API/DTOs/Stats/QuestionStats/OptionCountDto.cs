namespace SynapseEnqueteManagement.API.DTOs.Stats.QuestionStats;

public record OptionCountDto
{
    public string Option { get; init; } = string.Empty;
    public int Count { get; init; }
}
